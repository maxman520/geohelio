using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Pool;
using Cysharp.Threading.Tasks;
using Random = UnityEngine.Random;

// 소행성 스포너: 초기화 시 초기 배치를 만들고, 주기적으로 스폰을 수행한다.
// 동작 규칙 개요:
// 1) 자동 시작하지 않음.
// 2) Initialize 시 기존 소행성 정리 후 초기 개수 배치
// 3) Initialize 끝에서 스폰 시작
// 4) 플레이어 공전 원 안쪽은 스폰 금지(규칙 4, 초기/일반 공통 적용)
// 5) 기존 소행성과 최소 간격 0.5 유지(규칙 5)
// 6) 게임 진행 중에는 주기적으로 스폰, 중지 시 스폰 정지
public class ObjectSpawner : MonoBehaviour
{
    [Header("소행성 설정")]
    [SerializeField] private GameObject asteroidPrefab;      // 소행성 프리팹
    [SerializeField] private float spawnInterval = 1.0f;     // 스폰 간격(초)
    private int _maxAlive = 50;               // 최대 동시 소행성 수
    private float _spawnRadius = 6f;          // 스폰 반경 기본값(카메라 미발견 시 폴백 값)
    private int _initialCount = 8;            // 초기 생성 개수(Initialize 시 사용)

    [Header("규칙")]
    [SerializeField] private float minSeparation = 0.5f;      // 최소 간격 0.5 유지(규칙 5)
    [SerializeField] private float orbitEpsilon = 0.001f;     // 공전 원 내부 판정 여유값
    [Tooltip("플레이어 공전 원 내부 금지 시 추가로 확보할 여유 반경(지구/태양 크기 고려)")]
    [SerializeField] private float orbitGap = 0.5f;           // 공전 반지름에 더해 내부 금지 반경을 넓히는 갭
    [Tooltip("카메라 가시 영역 밖에서 추가로 허용할 여유 반경(월드 단위)")]
    [SerializeField] private float despawnMargin = 0.75f;     // 화면 경계 밖 여유 공간. 디스폰 마진



    [Header("장애물 소행성")]
    [Tooltip("장애물 소행성 프리팹(일반 소행성과 다른 프리팹 사용)")]
    [SerializeField] private GameObject obstacleAsteroidPrefab;
    [Tooltip("장애물 소행성 스폰 간격(초)")]
    [SerializeField] private float obstacleSpawnInterval = 2.5f;
    [Tooltip("장애물 소행성 최소 간격(서로 간)")]
    [SerializeField] private float obstacleMinSeparation = 3f;
    [Tooltip("장애물 스폰 단계 지속 시간(초). 각 단계별 최대 동시 개수를 결정")]
    [SerializeField] private float obstacleStageDuration = 30f;

    [Header("점수 텍스트")]
    [Tooltip("점수 플로팅 텍스트 프리팹(ScoreFloatingText 포함)")]
    [SerializeField] private GameObject scoreFloatingTextPrefab;

    [Header("슈팅스타")]
    [Tooltip("슈팅스타 프리팹(화면 밖→포물선 경로→화면 밖)")]
    [SerializeField] private GameObject shootingPrefab;
    [Tooltip("첫 생성까지의 최소 지연(첫 탭 이후, 초)")]
    [SerializeField] private float shootingInitialDelay = 10f;
    [Tooltip("최대 동시 개수 도달 시 쿨타임(초)")]
    [SerializeField] private float shootingPostMaxCooldown = 10f;
    [Tooltip("생성 시도 간 랜덤 추가 지연 범위(초)")]
    [SerializeField] private Vector2 shootingRandomDelayRange = new Vector2(0.5f, 3.0f);
    [Tooltip("슈팅스타 동시 최대 수")]
    [SerializeField] private int shootingMaxAlive = 6;
    private float _shootingSpeed; // 슈팅스타의 이동 속도

    [Header("블랙홀")]
    [Tooltip("블랙홀 프리팹(BlackHole 컴포넌트 포함 권장)")]
    [SerializeField] private GameObject blackHolePrefab;
    [Tooltip("블랙홀 수명(초)")]
    [SerializeField] private float blackHoleLifetimeSeconds = 5f;
    [Tooltip("블랙홀 스폰 지연 범위(초). 초기/종료 후 동일 적용")]
    [SerializeField] private Vector2 blackHoleSpawnDelayRange = new Vector2(20f, 30f);
    [Tooltip("블랙홀: 공전 원 내부 판정 여유값")]
    [SerializeField] private float blackHoleOrbitEpsilon = 0.001f;
    private float _blackHoleSpawnRadius = 6f; // 블랙홀 스폰 범위 반경

    // 내부 진행 상태
    private float _timer;
    private float _obstacleTimer;
    private bool _running;
    private readonly List<Transform> _spawnedAsteroid = new List<Transform>(); // 관리 중인 소행성 목록
    private readonly List<Transform> _spawnedObstacles = new List<Transform>(); // 장애물 소행성 목록
    private readonly List<Transform> _spawnedShootingStars = new List<Transform>(); // 슈팅스타 목록
    private float _baseSpawnInterval; // 동적 조정을 위한 기준 스폰 간격(초)
    private bool _spawnIntervalHalved; // 현재 스폰 간격이 절반 모드인지 여부
    // 유니티 내장 풀 딕셔너리: key = 프리팹 참조, value = ObjectPool
    private readonly Dictionary<GameObject, ObjectPool<GameObject>> _pools = new Dictionary<GameObject, ObjectPool<GameObject>>();
    private PlayerController _player;
    private Camera _camera;
    private bool _startSignalReceived;           // 시작 신호(첫 중심 전환) 수신 여부
    // 리스트 주기 정리 누적 타이머(성능 최적화)
    private float _cleanupAccum;
    // 블랙홀 진행 상태
    private CancellationTokenSource _blackHoleCts;
    private CancellationTokenSource _shootingStarCts; // 슈팅스타 스케줄 루프용 CTS
    private GameObject _activeBlackHole;

    // 스포너 파괴 시 진행 중 애니메이션이 있더라도 자연 종료에 맡긴다(컴포넌트가 자체 처리).

    #region 생명 주기
    private void Awake()
    {
        // 플레이어 참조(궤도 규칙 적용 시 필요)
        _player = FindFirstObjectByType<PlayerController>();

        // 메인 카메라 캐시(직교 카메라 가로 절반 길이로 반경 계산)
        _camera = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
        if (_camera == null)
        {
            Debug.LogWarning("[ObjectSpawner] 카메라를 찾지 못했습니다. 스폰 반경은 설정 값(spawnRadius)을 사용합니다.");
        }
        else if (_camera.orthographic)
        {
            // 직교 카메라 기반으로 스폰 반경을 설정한다(화면 가로 절반 길이).
            // 그런 다음 해당 반경을 기준으로 초기 개수와 최대 동시 개수를 산출한다.

            float cameraHalfWidth = _camera.orthographicSize * _camera.aspect;
            
            _shootingSpeed = Random.Range(cameraHalfWidth * 4/5, cameraHalfWidth * 6/5);
            _spawnRadius = cameraHalfWidth - 1f;
            _blackHoleSpawnRadius = _spawnRadius - 1f;
            // 반경 기반 파생 값 설정
            // spawnRadius = 6 기준 initialCount 20개, maxAlive 40개가 밸런스가 적합하다고 판단.
            // 계산하면
            // spawnRadius = r 기준 적합한 initialCount 개수는 (5/9) * (r^2) 개.
            // maxAlive는 (10/9) * (r^2) 개.
            _initialCount = Mathf.Max(0, Mathf.RoundToInt(5f / 9f * _spawnRadius * _spawnRadius));
            _maxAlive = Mathf.Max(0, _initialCount * 2);
            Debug.Log($"[ObjectSpawner] 카메라 기반 스폰 반경 적용: radius={_spawnRadius:F2}, initialCount={_initialCount}, maxAlive={_maxAlive}");
        }

        // 스폰 간격 기준값 저장(런타임 시작 시점의 인스펙터 값을 기준으로 사용)
        _baseSpawnInterval = Mathf.Max(0.0001f, spawnInterval);
        _spawnIntervalHalved = false;
    }

    private void Update()
    {
        if (!_running) return;
        // 현재 일반 소행성 수에 따라 스폰 간격을 동적으로 조정한다.
        AdjustSpawnInterval();
        _timer += Time.deltaTime;
        if (_timer >= spawnInterval)
        {
            _timer = 0f;
            SpawnAsteroid(ignoreOrbitRule: false);
        }

        // 장애물 스폰 타이머
        _obstacleTimer += Time.deltaTime;
        if (_obstacleTimer >= obstacleSpawnInterval)
        {
            _obstacleTimer = 0f;
            SpawnObstacle();
        }

        // 슈팅스타는 비동기 루프로 스케줄링하므로 매 프레임 갱신 불필요

        // 리스트 주기 정리(매 프레임 O(n) 스캔 방지)
        _cleanupAccum += Time.deltaTime;
        if (_cleanupAccum >= 0.5f)
        {
            _cleanupAccum = 0f;
            CleanupAsteroidList();
            CleanupObstacleList();
            CleanupShootingList();
        }
    }
    private void OnDestroy()
    {
        if (_player != null)
        {
            _player.OnCenterToggled -= HandleCenterToggled;
        }

        // 점수 텍스트는 외부 토큰 관리가 필요 없으므로 별도 정리 없음
        StopBlackHoleLoop(despawn: true);
        StopShootingLoop();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // 프리팹 구성 검증: Asteroid 컴포넌트가 반드시 있어야 함
        if (asteroidPrefab != null)
        {
            var hasAsteroid = asteroidPrefab.GetComponent<Asteroid>() != null;
            if (!hasAsteroid)
            {
                Debug.LogWarning("[ObjectSpawner] asteroidPrefab에 Asteroid 컴포넌트가 없습니다. 런타임 스폰이 취소될 수 있습니다.", asteroidPrefab);
            }
        }
        if (obstacleAsteroidPrefab != null)
        {
            var hasAsteroid2 = obstacleAsteroidPrefab.GetComponent<Asteroid>() != null;
            if (!hasAsteroid2)
            {
                Debug.LogWarning("[ObjectSpawner] obstacleAsteroidPrefab에 Asteroid 컴포넌트가 없습니다. 런타임 스폰이 취소될 수 있습니다.", obstacleAsteroidPrefab);
            }
        }

        if (shootingPrefab != null)
        {
            var hasStar = shootingPrefab.GetComponent<ShootingStar>() != null;
            if (!hasStar)
            {
                Debug.LogWarning("[ObjectSpawner] shootingStarPrefab에 ShootingStar 컴포넌트가 없습니다. 런타임 스폰이 취소될 수 있습니다.", shootingPrefab);
            }
        }

        // 값 보정 유틸(로컬 함수)
        static float ClampMinF(float v, float min) => v < min ? min : v;
        static int ClampMinI(int v, int min) => v < min ? min : v;
        static Vector2 EnsureRange(Vector2 range, float minStart)
        {
            float x = Mathf.Max(minStart, range.x);
            float y = Mathf.Max(x, range.y);
            return new Vector2(x, y);
        }

        // 일반/장애물/슈팅스타 보정
        spawnInterval = ClampMinF(spawnInterval, 0.05f);
        _maxAlive = ClampMinI(_maxAlive, 0);
        _spawnRadius = ClampMinF(_spawnRadius, 0f);
        _initialCount = ClampMinI(_initialCount, 0);
        minSeparation = ClampMinF(minSeparation, 0f);
        orbitEpsilon = ClampMinF(orbitEpsilon, 0f);
        despawnMargin = ClampMinF(despawnMargin, 0f);
        orbitGap = ClampMinF(orbitGap, 0f);
        obstacleSpawnInterval = ClampMinF(obstacleSpawnInterval, 0.05f);
        obstacleMinSeparation = ClampMinF(obstacleMinSeparation, 0f);
        obstacleStageDuration = ClampMinF(obstacleStageDuration, 1f);

        shootingMaxAlive = ClampMinI(shootingMaxAlive, 0);
        shootingInitialDelay = ClampMinF(shootingInitialDelay, 0f);
        shootingPostMaxCooldown = ClampMinF(shootingPostMaxCooldown, 0f);
        shootingRandomDelayRange = EnsureRange(shootingRandomDelayRange, 0f);

        if (scoreFloatingTextPrefab != null)
        {
            var hasComp = scoreFloatingTextPrefab.GetComponent<ScoreFloatingText>() != null;
            if (!hasComp)
            {
                Debug.LogWarning("[ObjectSpawner] scoreFloatingTextPrefab에 ScoreFloatingText 컴포넌트가 없습니다.", scoreFloatingTextPrefab);
            }
        }

        // 블랙홀 관련 값 보정
        _blackHoleSpawnRadius = ClampMinF(_blackHoleSpawnRadius, 0f);
        blackHoleLifetimeSeconds = ClampMinF(blackHoleLifetimeSeconds, 0f);
        blackHoleSpawnDelayRange = EnsureRange(blackHoleSpawnDelayRange, 0f);
        blackHoleOrbitEpsilon = ClampMinF(blackHoleOrbitEpsilon, 0f);
    }
#endif
#endregion 생명 주기

    // 일반 소행성 수가 initialCount보다 적으면 스폰 간격을 절반으로, 아니면 원래 값으로 복구한다.
    // Update() 에서 호출.
    private void AdjustSpawnInterval()
    {
        int alive = _spawnedAsteroid.Count;
        if (alive < Mathf.Max(0, _initialCount))
        {
            if (!_spawnIntervalHalved)
            {
                spawnInterval = _baseSpawnInterval * 0.5f;
                _spawnIntervalHalved = true;
                Debug.Log("[ObjectSpawner] 일반 소행성 수가 기준 미만으로 감소 — 스폰 간격을 절반으로 감소 (더욱 빨리 생성)");
            }
        }
        else
        {
            if (_spawnIntervalHalved)
            {
                spawnInterval = _baseSpawnInterval;
                _spawnIntervalHalved = false;
                Debug.Log("[ObjectSpawner] 일반 소행성 수가 기준 이상 — 스폰 간격을 원래 값으로 복구");
            }
        }
    }

    /// <summary>
    /// 스포너 초기화: 기존 소행성 정리 후 초기 배치를 다시 생성한다.
    /// </summary>
    public void Initialize()
    {
        // 기존 스폰된 오브젝트 제거
        RemoveAllObjects();

        // 초기 배치 생성(규칙 4 적용: 공전 범위 제외)
        for (int i = 0; i < Mathf.Max(0, _initialCount); i++)
        {
            SpawnAsteroid(ignoreOrbitRule: false);
        }

        // 시작 신호 대기 상태로 리셋(첫 탭으로 중심 전환 시까지 주기 스폰 보류)
        _startSignalReceived = false;
        _running = false; // 주기 스폰은 보류
        _timer = 0f;
        _obstacleTimer = 0f;
        Debug.Log("[ObjectSpawner] 초기화 완료: 초기 배치 생성 후 시작 신호(첫 중심 전환) 대기");

        // 블랙홀 루프 정지 및 정리
        StopBlackHoleLoop(despawn: true);
        // 슈팅스타 루프 정지
        StopShootingLoop();

        // 소프트 리스타트 대비: 플레이어 중심 전환 이벤트를 다시 구독하여
        // 다음 라운드의 첫 신호를 받을 수 있도록 재설정한다(중복 방지 위해 선해제 후 재구독).
        if (_player == null)
        {
            _player = FindFirstObjectByType<PlayerController>();
            if (_player == null)
            {
                Debug.LogWarning("[ObjectSpawner] Initialize 시 PlayerController를 찾지 못했습니다. 시작 신호를 수신하지 못할 수 있습니다.");
            }
        }

        if (_player != null)
        {
            _player.OnCenterToggled -= HandleCenterToggled;
            _player.OnCenterToggled += HandleCenterToggled;
        }
    }

    // 플레이어 중심 전환 이벤트 처리
    private void HandleCenterToggled(bool isSun)
    {
        if (_startSignalReceived) return; // 이미 시작 신호를 받았던 상태(게임이 시작됨)라면 return
        _startSignalReceived = true;
        // 스폰 시작
        _running = true;
        _timer = 0f;
        Debug.Log("[ObjectSpawner] 시작 신호 감지: 주기 스폰 시작");
        // 첫 생성은 초기 지연 + 랜덤 추가 지연 이후(비동기 루프 시작)
        float extra = Random.Range(shootingRandomDelayRange.x, shootingRandomDelayRange.y);
        float initialDelay = shootingInitialDelay + Mathf.Max(0f, extra);

        // 슈팅스타 루프 시작
        StartShootingLoop(initialDelay);

        // 블랙홀 루프 시작(초기 20~30초 랜덤 지연 후 스폰)
        StartBlackHoleLoop();
    }

    /// <summary>
    /// 스폰 중지(게임 일시정지/종료 등).
    /// </summary>
    public void Stop()
    {
        _running = false;
        Debug.Log("[ObjectSpawner] 스폰 중지");

        // 블랙홀 루프 중지 및 디스폰
        StopBlackHoleLoop(despawn: true);
        // 슈팅스타 루프 중지
        StopShootingLoop();
    }

    #region 풀 헬퍼: 유니티 내장 풀(ObjectPool) 딕셔너리
    private ObjectPool<GameObject> GetPoolForPrefab(GameObject prefab)
    {
        if (prefab == null) return null;
        if (_pools.TryGetValue(prefab, out var pool)) return pool;

        // 풀 생성: 동일 프리팹만 생성되도록 createFunc에 프리팹 캡처
        bool collectionCheck = true;
        int defaultCapacity = 16;
        int maxSize = 256;
        pool = new ObjectPool<GameObject>(
            createFunc: () =>
            {
                var go = Instantiate(prefab);
                // 풀 키 태그 설정
                var tag = go.GetComponent<PooledObjectTag>();
                if (tag == null) tag = go.AddComponent<PooledObjectTag>();
                tag.SetPrefab(prefab);
                go.SetActive(false);
                return go;
            },
            actionOnGet: go =>
            {
                if (go == null) return;
                go.SetActive(true);
            },
            actionOnRelease: go =>
            {
                if (go == null) return;
                go.SetActive(false);
                // 관리 편의를 위해 스포너 자식으로 귀속
                go.transform.SetParent(transform, false);
            },
            actionOnDestroy: go =>
            {
                if (go != null) Destroy(go);
            },
            collectionCheck: collectionCheck,
            defaultCapacity: defaultCapacity,
            maxSize: maxSize
        );

        _pools[prefab] = pool;
        return pool;
    }

    private GameObject GetFromPool(GameObject prefab)
    {
        var pool = GetPoolForPrefab(prefab);
        if (pool == null) return null;
        return pool.Get();
    }

    private void ReturnToPool(GameObject go)
    {
        if (go == null) return;
        var tag = go.GetComponent<PooledObjectTag>();
        var prefab = tag != null ? tag.SourcePrefab : null;
        if (prefab == null || !_pools.TryGetValue(prefab, out var pool))
        {
            Debug.LogWarning("[ObjectSpawner] 풀 키를 찾지 못해 오브젝트를 비활성화만 합니다.", go);
            go.SetActive(false);
            go.transform.SetParent(transform, false);
            return;
        }
        pool.Release(go);
    }
    #endregion 풀 헬퍼

    #region 공통 로직/외부 접근

    // 스폰 시도 공통 로직: 위치 획득 함수가 true를 반환하면 spawnAt을 호출하고 true 반환
    private delegate bool GetPosition(out Vector3 pos);
    private bool SpawnWith(int tries, GetPosition getPos, Action<Vector3> spawnAt)
    {
        int t = Mathf.Max(1, tries);
        for (int i = 0; i < t; i++)
        {
            if (getPos(out var pos))
            {
                spawnAt?.Invoke(pos);
                return true;
            }
        }
        return false;
    }

    // CancellationTokenSource 해제 공통 유틸
    private static void CancelAndDispose(ref CancellationTokenSource cts)
    {
        if (cts == null) return;
        try { cts.Cancel(); }
        catch { /* 취소 중 예외 무시 */ }
        cts.Dispose();
        cts = null;
    }

    /// <summary>
    /// 점수 팝업 표시
    /// </summary>
    public void ShowScorePopup(int amount, Vector3 worldPos)
    {
        if (scoreFloatingTextPrefab == null)
        {
            Debug.LogWarning("[ObjectSpawner] scoreFloatingTextPrefab이 설정되지 않아 점수 텍스트를 표시할 수 없습니다.");
            return;
        }
        var go = GetFromPool(scoreFloatingTextPrefab);
        Vector3 startPos = worldPos;
        SetupSpawned(go, startPos, Quaternion.identity);

        var comp = go.GetComponent<ScoreFloatingText>();
        if (comp == null)
        {
            Debug.LogWarning("[ObjectSpawner] scoreFloatingTextPrefab에 ScoreFloatingText 컴포넌트가 없습니다.", go);
            ReturnToPool(go);
            return;
        }
        comp.SetAmount(amount);
        comp.PlayAsync(startPos).ContinueWith(() => { ReturnToPool(go); }).Forget();
    }

    // 공통 스폰 후 기본 설정(부모/위치/회전)
    private void SetupSpawned(GameObject go, Vector3 pos, Quaternion rot)
    {
        if (go == null) return;
        go.transform.SetParent(transform, false);
        go.transform.position = pos;
        go.transform.rotation = rot;
    }


    // 목록 내 null 항목 정리(공통)
    private static void CleanNulls(List<Transform> list)
    {
        if (list == null) return;
        for (int i = list.Count - 1; i >= 0; i--)
        {
            if (list[i] == null) list.RemoveAt(i);
        }
    }

    private void CleanupAsteroidList() => CleanNulls(_spawnedAsteroid);
    private void CleanupObstacleList() => CleanNulls(_spawnedObstacles);
    private void CleanupShootingList() => CleanNulls(_spawnedShootingStars);


    // 모든 오브젝트 제거(태그 기반 월드 정리 + 풀/목록 정리)
    private void RemoveAllObjects()
    {
        // 풀/목록에 등록된 오브젝트 정리 후 풀에 반환
        CleanupAsteroidList();
        for (int i = _spawnedAsteroid.Count - 1; i >= 0; i--)
        {
            var tr = _spawnedAsteroid[i];
            if (tr == null) continue;
            ReturnToPool(tr.gameObject);
        }
        _spawnedAsteroid.Clear();

        // 장애물도 동일 처리
        CleanupObstacleList();
        for (int i = _spawnedObstacles.Count - 1; i >= 0; i--)
        {
            var tr = _spawnedObstacles[i];
            if (tr == null) continue;
            ReturnToPool(tr.gameObject);
        }
        _spawnedObstacles.Clear();

        // 슈팅스타도 동일 처리
        CleanupShootingList();
        for (int i = _spawnedShootingStars.Count - 1; i >= 0; i--)
        {
            var tr = _spawnedShootingStars[i];
            if (tr == null) continue;
            ReturnToPool(tr.gameObject);
        }
        _spawnedShootingStars.Clear();
    }
    /// <summary>
    /// 외부(소행성, 슈팅스타)에서 종료 요청: 풀로 반환
    /// </summary>
    public void Despawn(Transform tr)
    {
        if (tr == null) return;
        var go = tr.gameObject;
        // 어떤 풀인지 구분하여 반환
        if (_spawnedAsteroid.Remove(tr) || _spawnedObstacles.Remove(tr) || _spawnedShootingStars.Remove(tr))
        {
            ReturnToPool(go);
            return;
        }
        // 목록에 없더라도 풀로 반환(외부에서 직접 호출된 경우 대비)
        ReturnToPool(go);
    }
    

    public float GetGameOverRadius()
    {
        if (_camera != null && _camera.orthographic)
        {
            return _camera.orthographicSize * _camera.aspect;
        }
        // 폴백: _spawnRadius + 1 사용(원근 카메라 또는 미발견 시. _spawnRadius는 가로 절반길이의 -1 이므로)
        return _spawnRadius + 1f;
    }

    /// <summary>
    /// 카메라 화면 반경(가로 절반 길이, GameOverRadius) + 여유 마진 기준으로 바깥인지 판정한다.
    /// 중심은 카메라의 현재 XY 위치를 사용하며, 카메라가 없으면 월드 원점을 사용한다.
    /// </summary>
    public bool IsOutsideDespawnRadius(Vector3 worldPos)
    {
        Vector2 center = Vector2.zero;
        if (_camera != null)
        {
            var cp = _camera.transform.position;
            center = new Vector2(cp.x, cp.y);
        }
        float r = GetGameOverRadius() + Mathf.Max(0f, despawnMargin);
        Vector2 d = new Vector2(worldPos.x, worldPos.y) - center;
        return d.sqrMagnitude > r * r;
    }

    // 공전 원 내부 금지 판정: 현재 플레이어 중심 기준으로 pos가 플레이어 반경 내부인지 확인한다.
    private bool IsInsidePlayerOrbit(Vector3 pos, float epsilon, float gap)
    {
        if (_player == null || _player.CurrentCenter == null) return false;
        Vector3 oc = _player.CurrentCenter.position; oc.z = 0f;
        float orbitR = Mathf.Max(0f, _player.Distance + Mathf.Max(0f, gap));
        float boundary = Mathf.Max(0f, orbitR - Mathf.Max(0f, epsilon));
        float d2 = (pos - oc).sqrMagnitude;
        return d2 < boundary * boundary;
    }

    // 최소 간격 판정: list 내 트랜스폼들과의 거리가 minDist 이상인지 확인한다.
    private static bool IsFarEnoughFrom(List<Transform> list, Vector3 pos, float minDist)
    {
        if (list == null || list.Count == 0) return true;
        float minSep2 = Mathf.Max(0f, minDist) * Mathf.Max(0f, minDist);
        for (int i = 0; i < list.Count; i++)
        {
            var tr = list[i];
            if (tr == null) continue;
            if ((tr.position - pos).sqrMagnitude < minSep2) return false;
        }
        return true;
    }

    #endregion 공통 로직/외부 접근

    #region 소행성

    // 스폰 위치 생성 규칙 적용
    private bool GetAsteroidSpawnPosition(bool ignoreOrbitRule, out Vector3 pos)
    {
        // 중심 (0, 0)
        Vector3 c = Vector3.zero;

        // 원의 범위(내부)에서 균등 분포로 선택
        Vector2 rnd = Random.insideUnitCircle * _spawnRadius;
        pos = c + new Vector3(rnd.x, rnd.y, 0f);

        // 규칙 4: 플레이어 공전 원(현재 중심 기준) 내부 금지 — 초기/일반 스폰 모두 적용
        if (!ignoreOrbitRule && IsInsidePlayerOrbit(pos, orbitEpsilon, orbitGap))
        {
            return false; // 공전 원 내부는 배치 불가
        }

        // 규칙 5: 기존 소행성과 최소 간격 유지(0.5)
        if (!IsFarEnoughFrom(_spawnedAsteroid, pos, minSeparation)) return false;

        return true;
    }

    // 소행성 한 개 스폰 시도
    private void SpawnAsteroid(bool ignoreOrbitRule)
    {
        if (asteroidPrefab == null) return;
        CleanupAsteroidList();
        if (_spawnedAsteroid.Count >= _maxAlive) return;

        // 유효 위치 탐색(최대 시도 횟수 제한)
        const int kMaxTries = 24;
        SpawnWith(kMaxTries,
            getPos: (out Vector3 p) => GetAsteroidSpawnPosition(ignoreOrbitRule, out p),
            spawnAt: SpawnAsteroidAt);

        // 유효 위치를 찾지 못한 경우(드문 상황)
        // ..
    }

    // 해당 pos에 소행성 스폰
    private void SpawnAsteroidAt(Vector3 pos)
    {
        var go = GetFromPool(asteroidPrefab);
        // 스폰 시 Z 회전값을 랜덤으로 부여하여 소행성 방향을 다양화
        float z = Random.Range(0f, 360f);
        SetupSpawned(go, pos, Quaternion.Euler(0f, 0f, z));

        // 구성 요소 준비(프리팹 구성 보장: Asteroid 컴포넌트 필수)
        var asteroid = go.GetComponent<Asteroid>();
        if (asteroid == null)
        {
            Debug.LogError("[ObjectSpawner] 소행성 프리팹에 Asteroid 컴포넌트가 없습니다. 스폰을 취소합니다.");
            ReturnToPool(go);
            return;
        }
        asteroid.Initialize(this);
        asteroid.ResetForSpawn();

        _spawnedAsteroid.Add(go.transform);
    }

    #endregion 소행성

    #region 장애물 소행성

    private bool GetObstacleSpawnPosition(out Vector3 pos)
    {
        Vector3 c = Vector3.zero;
        Vector2 rnd = Random.insideUnitCircle * _spawnRadius;
        pos = c + new Vector3(rnd.x, rnd.y, 0f);

        // 플레이어 공전 원 내부 금지 규칙 적용
        if (IsInsidePlayerOrbit(pos, orbitEpsilon, orbitGap)) return false;

        // 장애물 간 최소 간격 3
        if (!IsFarEnoughFrom(_spawnedObstacles, pos, obstacleMinSeparation)) return false;

        // 일반 소행성과의 최소 간격은 일반 소행성 규칙(minSeparation)을 따른다
        if (!IsFarEnoughFrom(_spawnedAsteroid, pos, minSeparation)) return false;

        return true;
    }

    // 장애물 소행성 스폰 시도
    private void SpawnObstacle()
    {
        if (obstacleAsteroidPrefab == null) return;
        CleanupObstacleList();

        int maxObstacles = GetObstacleMaxAlive();
        if (_spawnedObstacles.Count >= maxObstacles) return;

        const int kMaxTries = 24;
        SpawnWith(kMaxTries,
            getPos: (out Vector3 p) => GetObstacleSpawnPosition(out p),
            spawnAt: SpawnObstacleAt);
    }
    
    // 해당 pos에 장애물 소행성 스폰
    private void SpawnObstacleAt(Vector3 pos)
    {
        var go = GetFromPool(obstacleAsteroidPrefab);
        float z = Random.Range(0f, 360f);
        SetupSpawned(go, pos, Quaternion.Euler(0f, 0f, z));

        var asteroid = go.GetComponent<Asteroid>();
        if (asteroid == null)
        {
            Debug.LogError("[ObjectSpawner] 장애물 프리팹에 Asteroid 컴포넌트가 없습니다. 스폰을 취소합니다.");
            ReturnToPool(go);
            return;
        }
        asteroid.Initialize(this);
        asteroid.SetAsObstacle(true);
        asteroid.ResetForSpawn();

        _spawnedObstacles.Add(go.transform);
    }

    // 플레이어 생존 시간에 따른 장애물 최대 동시 개수(1단계:2, 2단계:3, 3단계+:4)
    private int GetObstacleMaxAlive()
    {
        float t = 0f;

        var gm = GameManager.Instance;
        if (gm != null) t = gm.ElapsedTime;

        float stageDur = Mathf.Max(1f, obstacleStageDuration);
        int stage = Mathf.FloorToInt(t / stageDur) + 1; // 1부터 시작
        if (stage <= 1) return 2;
        if (stage == 2) return 3;
        return 4; // 3단계 이상
    }

    #endregion 장애물 소행성

    #region 슈팅스타

    // 카메라 뷰 사각형과 (뷰+마진) 사각형 사이의 띠 영역 내 임의의 점을 선택
    // 반환: 성공 시 true, pos는 월드 좌표
    private bool GetShootingSpawnPoint(out Vector3 pos)
    {
        pos = Vector3.zero;
        if (_camera == null || !_camera.orthographic) return false;

        var cp = _camera.transform.position;
        float hi = _camera.orthographicSize;                  // 안 쪽 half-height
        float wi = _camera.orthographicSize * _camera.aspect; // 안 쪽 half-width
        float margin = Mathf.Max(0f, despawnMargin);
        float ho = hi + margin; // 바깥 쪽 half-height. margin을 더해서 카메라 영역보다 조금 더 큰 외부 사각형을 정의
        float wo = wi + margin; // 바깥 쪽 half-width

        const int kMaxTries = 64; // 후보 고르기를 64번 반복.
        for (int i = 0; i < kMaxTries; i++)
        {
            float x = Random.Range(cp.x - wo, cp.x + wo);
            float y = Random.Range(cp.y - ho, cp.y + ho);
            float dx = Mathf.Abs(x - cp.x);
            float dy = Mathf.Abs(y - cp.y);
            bool insideOuter = dx <= wo && dy <= ho;
            bool outsideInner = dx > wi || dy > hi;
            if (insideOuter && outsideInner) // 후보 고르기에 성공하면 pos를 설정 후 true를 return.
            {
                pos = new Vector3(x, y, 0f);
                return true;
            }
        }

        // 드물게 실패 시, 외곽 테두리 중 하나에서 선택
        int edge = Random.Range(0, 4); // 0 ~ 3
        switch (edge)
        {
            case 0: pos = new Vector3(Random.Range(cp.x - wo, cp.x + wo), cp.y + ho, 0f); break; // 위
            case 1: pos = new Vector3(Random.Range(cp.x - wo, cp.x + wo), cp.y - ho, 0f); break; // 아래
            case 2: pos = new Vector3(cp.x - wo, Random.Range(cp.y - ho, cp.y + ho), 0f); break; // 왼쪽
            default: pos = new Vector3(cp.x + wo, Random.Range(cp.y - ho, cp.y + ho), 0f); break; // 오른쪽
        }
        return true;
    }

    private bool SpawnShooting()
    {
        if (shootingPrefab == null) return false;
        CleanupShootingList();
        if (_spawnedShootingStars.Count >= shootingMaxAlive) return false;

        const int kMaxTries = 32;
        // 카메라 뷰 사각형과 (뷰+마진) 사각형 사이의 띠 영역에서 시작 지점을 선택
        if (_camera != null && _camera.orthographic)
        {
            // 시작/종점 선택 후 좌우대칭 2차곡선을 다양한 중점으로 시도하며
            // 플레이어 공전 범위를 지나는 경로를 찾는다. 실패 시 새 시작/종점으로 재시도.
            for (int seTry = 0; seTry < kMaxTries; seTry++)
            {
                if (!GetShootingSpawnPoint(out Vector3 start)) return false;
                Vector3 end = new Vector3(-start.x, -start.y, start.z);

                if (GetPassingCurve(start, end, out Vector3 passPoint))
                {
                    SpawnShootingAt(start, end, passPoint);
                    return true;
                }
                // 중점 시도 실패 → 새 시작/종점으로 재시도
            }
            return false; // 최대 시도 실패
        }

        return false;
    }

    private void SpawnShootingAt(Vector3 start, Vector3 end, Vector3 passPoint)
    {
        var go = GetFromPool(shootingPrefab);
        SetupSpawned(go, start, Quaternion.identity);

        var star = go.GetComponent<ShootingStar>();
        if (star == null)
        {
            Debug.LogError("[ObjectSpawner] shootingStarPrefab에 ShootingStar 컴포넌트가 없습니다. 스폰을 취소합니다.");
            ReturnToPool(go);
            return;
        }
        // 먼저 목록에 추가하여 이동 시작 직후 디스폰되어도 올바른 풀로 복귀되게 함
        _spawnedShootingStars.Add(go.transform);
        star.Initialize(this);
        star.Launch(start, end, passPoint, _shootingSpeed);
    }

    private void StartShootingLoop(float initialDelaySeconds)
    {
        if (_shootingStarCts != null) return; // 이미 동작 중
        _shootingStarCts = new CancellationTokenSource();
        RunShootingLoopAsync(_shootingStarCts.Token, Mathf.Max(0f, initialDelaySeconds)).Forget();
        Debug.Log("[ObjectSpawner] 슈팅스타 스케줄 루프 시작");
    }

    private void StopShootingLoop()
    {
        // 공통 CTS 해제 헬퍼 사용
        CancelAndDispose(ref _shootingStarCts);
        // (제거됨) 구 로직 잔재 타임스탬프 리셋 코드
    }

    private async UniTaskVoid RunShootingLoopAsync(CancellationToken ct, float initialDelaySeconds)
    {
        // 초기 지연 대기
        if (initialDelaySeconds > 0f)
        {
            try { await UniTask.Delay(TimeSpan.FromSeconds(initialDelaySeconds), cancellationToken: ct); }
            catch (OperationCanceledException) { return; }
        }

        while (!ct.IsCancellationRequested)
        {
            CleanupShootingList();
            // 최대 동시 개수 도달 시 쿨다운 진입
            if (_spawnedShootingStars.Count >= shootingMaxAlive)
            {
                float extraCd = Random.Range(shootingRandomDelayRange.x, shootingRandomDelayRange.y);
                float cd = shootingPostMaxCooldown + Mathf.Max(0f, extraCd);
                try { await UniTask.Delay(TimeSpan.FromSeconds(cd), cancellationToken: ct); }
                catch (OperationCanceledException) { break; }
                continue;
            }

            bool spawned = SpawnShooting();
            float wait;
            if (spawned)
            {
                if (_spawnedShootingStars.Count >= shootingMaxAlive)
                {
                    float extraCd = Random.Range(shootingRandomDelayRange.x, shootingRandomDelayRange.y);
                    wait = shootingPostMaxCooldown + Mathf.Max(0f, extraCd);
                }
                else
                {
                    wait = Mathf.Max(0f, Random.Range(shootingRandomDelayRange.x, shootingRandomDelayRange.y));
                }
            }
            else
            {
                wait = 0.5f; // 실패 시 짧게 재시도
            }

            try { await UniTask.Delay(TimeSpan.FromSeconds(wait), cancellationToken: ct); }
            catch (OperationCanceledException) { break; }
        }
    }

    // 좌우대칭 2차 곡선(시작/종점 고정)의 u=0.5 경유점을 여러 후보로 시도하며
    // 플레이어의 공전 범위를 교차하는 곡선을 찾는다.
    private bool GetPassingCurve(Vector3 start, Vector3 end, out Vector3 passPoint)
    {
        passPoint = Vector3.zero;

        // 플레이어 위치(축 정렬용)
        Vector3 playerPos = Vector3.zero;
        if (_player != null)
        {
            var pt = _player.transform.position;
            playerPos = new Vector3(pt.x, pt.y, 0f);
        }

        const int kMaxTries = 32; // 최대 32번 시도

        for (int i = 0; i < kMaxTries; i++)
        {
            var candidate = ComputePassPoint(start, end, playerPos);
            if (IslinePassingPlayerOrbit(start, end, candidate))
            {
                passPoint = candidate;
                return true;
            }
        }
        return false;
    }

    // 슈팅스타 경유점 계산: end-start의 법선 벡터 2가지 중 플레이어 방향을 향하는 쪽을 선택해
    // 원점 기준으로 랜덤 크기(|offset|)만큼 이동한 점을 반환한다.
    private Vector3 ComputePassPoint(Vector3 start, Vector3 end, Vector3 playerPos)
    {
        // 세그먼트 벡터 계산
        Vector2 seg = new Vector2(end.x - start.x, end.y - start.y);

        float h = _camera.orthographicSize;                  // 카메라 half-height
        float w = _camera.orthographicSize * _camera.aspect; // 카메라 half-width

        float maxMag = (float) Math.Sqrt(h * h + w * w);
        float mag = Random.Range(1f, maxMag);

        // 접선/법선 계산(2D)
        Vector2 t = seg.normalized;
        Vector2 n = new Vector2(-t.y, t.x); // 좌측 법선(우측은 -n)

        // 플레이어 방향과 일치하는 법선 벡터를 선택
        Vector2 toPl = new Vector2(playerPos.x, playerPos.y);
        float sign = Vector2.Dot(toPl, n) >= 0f ? 1f : -1f;


        Vector2 offset = n * (sign * mag);

        return new Vector3(offset.x, offset.y, 0f);
    }

    // 주어진 슈팅스타 이동경로가
    // 현재 플레이어의 공전 범위을 교차/관통하는지 검사한다.
    // 베지어-원 정확한 교차 확인은 비용이 커서, 샘플링+부호 변화로 근사 판정
    private bool IslinePassingPlayerOrbit(Vector3 start, Vector3 end, Vector3 passPoint)
    {
        if (_player == null || _player.CurrentCenter == null)
        {
            // 플레이어가 없으면 교차 검증 불가 — 이 경우 통과로 간주해 생성 진행
            return true;
        }

        // 제어점 복원: B(0.5) = passPoint ⇒ C = 2*B(0.5) - 0.5*(P0+P2)
        Vector3 control = (2f * passPoint) - 0.5f * (start + end);

        // 공전 원 파라미터
        Vector3 cp = _player.CurrentCenter.position;
        float r = Mathf.Max(0f, _player.Distance);
        float tol = Mathf.Max(0f, 0.25f);

        // 샘플링으로 교차 판정
        // 곡선과 원 사이의 거리 함수 f(u)=|P(u)-oc|-r
        // f(u) 부호 변화(원 내부↔외부 전환)로 교차를 검출
        const int kSeg = 32;
        float prev = 0f;
        bool hasPrev = false;
        float minAbs = float.MaxValue;
        for (int i = 0; i <= kSeg; i++)
        {
            float u = i / (float)kSeg;
            Vector3 p = PosOnLine(start, control, end, u);
            float d = (p - cp).magnitude - r;
            minAbs = Mathf.Min(minAbs, Mathf.Abs(d));
            if (hasPrev)
            {
                if (prev == 0f || Mathf.Sign(prev) != Mathf.Sign(d))
                {
                    // 부호 변화 ⇒ 원과 교차
                    return true;
                }
            }
            prev = d; hasPrev = true;
        }

        // 직접 교차는 없었지만 허용 오차 내로 근접하면 통과로 인정
        return minAbs <= tol;
    }

    // 2차 베지어 곡선 상의 좌표 반환
    private Vector3 PosOnLine(Vector3 p0, Vector3 c, Vector3 p2, float u)
    {
        u = Mathf.Clamp01(u);
        float omt = 1f - u;
        return (omt * omt) * p0 + 2f * omt * u * c + (u * u) * p2;
    }
    #endregion 슈팅스타

    #region 블랙홀

    public float GetBlackholeSpawnRadius()
    {
        return _blackHoleSpawnRadius;
    }
    private void StartBlackHoleLoop()
    {
        if (_blackHoleCts != null) return; // 이미 동작 중
        _blackHoleCts = new CancellationTokenSource();
        RunBlackHoleLoopAsync(_blackHoleCts.Token).Forget();
        Debug.Log("[ObjectSpawner] 블랙홀 스폰 루프 시작");
    }

    private void StopBlackHoleLoop(bool despawn)
    {
        // 공통 CTS 해제 헬퍼 사용
        CancelAndDispose(ref _blackHoleCts);
        if (despawn)
        {
            // 디스폰 시 애니메이션(트리거: despawn)을 먼저 재생 후 파괴
            DespawnBlackHoleNow();
        }
    }

    private async UniTaskVoid RunBlackHoleLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            float delay = GetBlackHoleRandomDelay();
            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(delay), cancellationToken: ct);
            }
            catch (OperationCanceledException) { break; }

            if (ct.IsCancellationRequested) break;
            if (_activeBlackHole != null) continue; // 단일 개체 보장

            // 스폰 시도(여러 번 샘플링)
            const int kMaxTries = 32;
            bool ok = false; Vector3 pos = Vector3.zero;
            for (int i = 0; i < kMaxTries; i++)
            {
                if (GetBlackHoleSpawnPosition(out pos)) { ok = true; break; }
            }
            if (!ok) continue; // 다음 사이클로

            SpawnBlackHoleAt(pos);

            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(Mathf.Max(0.01f, blackHoleLifetimeSeconds)), cancellationToken: ct);
            }
            catch (OperationCanceledException) { break; }

            // 수명 종료: 디스폰 애니메이션을 재생하고 완료를 기다린 뒤 파괴
            DespawnBlackHoleNow();
        }
    }

    private float GetBlackHoleRandomDelay()
    {
        float a = Mathf.Max(0f, blackHoleSpawnDelayRange.x);
        float b = Mathf.Max(a, blackHoleSpawnDelayRange.y);
        return Random.Range(a, b);
    }

    private bool GetBlackHoleSpawnPosition(out Vector3 pos)
    {
        // (0,0) 중심, 반경 = blackHoleSpawnRadius 내부 균등 샘플링
        Vector2 r = Random.insideUnitCircle * Mathf.Max(0f, _blackHoleSpawnRadius);
        pos = new Vector3(r.x, r.y, 0f);

        // 공전 원 내부 금지(블랙홀은 gap=0 적용)
        if (IsInsidePlayerOrbit(pos, blackHoleOrbitEpsilon, 0f)) return false;
        return true;
    }

    // 해당 pos에 블랙홀 소환
    private void SpawnBlackHoleAt(Vector3 pos)
    {
        if (blackHolePrefab == null)
        {
            Debug.LogWarning("[ObjectSpawner] blackHolePrefab이 설정되지 않아 블랙홀을 스폰할 수 없습니다.");
            return;
        }
        if (_activeBlackHole != null) return;

        _activeBlackHole = Instantiate(blackHolePrefab, pos, Quaternion.identity, gameObject.transform);
    }

    private void DespawnBlackHoleNow()
    {
        if (_activeBlackHole == null) return;

        var go = _activeBlackHole;
        _activeBlackHole = null; // 다음 스폰을 막지 않기 위해 즉시 해제
        if (go == null) return;


        var bh = go.GetComponent<BlackHole>();
        if (bh != null)
        {
            // 블랙홀 컴포넌트에 디스폰 연출 및 파괴를 위임
            bh.DespawnAndDestroyAsync().Forget();
            return;
        }
        // 컴포넌트가 없으면 즉시 파괴(폴백)
        Destroy(go);
    }

    #endregion 블랙홀

    #region Gizmo
    private void OnDrawGizmos()
    {
        // 스폰/디스폰 영역을 Scene 뷰에서 시각화한다.
        // 화면 가로 절반 길이를 반경으로 하는 원(월드 원점 중심)

        // 기즈모 행렬 초기화(외부에서 변경되었을 수 있음에 대비)
        Gizmos.matrix = Matrix4x4.identity;

        // ------- 소행성 디스폰 경계(카메라 중심 + 반경 + 마진) -------
        if (GetDespawnCircle(out Vector3 c, out float r))
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.9f); // 주황색: 디스폰 경계
            Gizmos.DrawWireSphere(c, r);
            // 카메라 중심 마커
            Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.9f);
            Gizmos.DrawSphere(c, Mathf.Max(0.04f, r * 0.015f));
        }

        // ------- 슈팅스타 사각형 디스폰 경계(카메라 뷰 + 마진)--------
        if (GetDespawnRect(out Vector3 rc, out float halfW, out float halfH))
        {
            Gizmos.color = new Color(0.7f, 0.2f, 1f, 0.9f); // 보라색: 사각형 경계
            DrawWireRect(rc, halfW, halfH);
        }

        // ---------- 블랙홀 스폰 범위(월드 원점 기준) ----------
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(Vector3.zero, Mathf.Max(0f, _blackHoleSpawnRadius));
    }

    // 디스폰 경계 계산(중심 + 반경 + 마진)
    private bool GetDespawnCircle(out Vector3 center, out float radius)
    {
        center = Vector3.zero;
        radius = GetGameOverRadius() + Mathf.Max(0f, despawnMargin);
        return true;
    }

    // 카메라 직교 뷰 기준 사각형 디스폰 경계(슈팅스타용) 계산: 성공 시 true
    private bool GetDespawnRect(out Vector3 center, out float halfWidth, out float halfHeight)
    {
        center = Vector3.zero;
        halfWidth = 0f; halfHeight = 0f;

        Camera cam = null;
        if (Application.isPlaying)
        {
            cam = _camera;
        }
        else
        {
            cam = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
        }

        if (cam == null || !cam.orthographic)
        {
            return false; // 직교 카메라가 아닐 때는 사각형 경계를 그리지 않음
        }

        center = cam.transform.position; center.z = 0f;
        halfHeight = cam.orthographicSize + Mathf.Max(0f, despawnMargin);
        halfWidth = cam.orthographicSize * cam.aspect + Mathf.Max(0f, despawnMargin);
        return true;
    }

    // 사각형 외곽선 그리기(카메라 뷰 + 마진 시각화)
    private void DrawWireRect(Vector3 center, float halfWidth, float halfHeight)
    {
        Vector3 bl = new Vector3(center.x - halfWidth, center.y - halfHeight, 0f);
        Vector3 br = new Vector3(center.x + halfWidth, center.y - halfHeight, 0f);
        Vector3 tr = new Vector3(center.x + halfWidth, center.y + halfHeight, 0f);
        Vector3 tl = new Vector3(center.x - halfWidth, center.y + halfHeight, 0f);

        Gizmos.DrawLine(bl, br);
        Gizmos.DrawLine(br, tr);
        Gizmos.DrawLine(tr, tl);
        Gizmos.DrawLine(tl, bl);
    }
    #endregion Gizmo
}
