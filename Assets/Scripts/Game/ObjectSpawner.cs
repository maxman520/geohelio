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
    [Header("설정")]
    [SerializeField] private GameObject asteroidPrefab;      // 소행성 프리팹
    [SerializeField] private float spawnInterval = 1.0f;     // 스폰 간격(초)
    [SerializeField] private int maxAlive = 50;               // 최대 동시 소행성 수
    [SerializeField] private float spawnRadius = 6f;          // 스폰 반경 기본값(카메라 미발견 시 폴백 값)
    [SerializeField] private int initialCount = 8;            // 초기 생성 개수(Initialize 시 사용)
    [Tooltip("기존 월드에 남아있는 소행성 제거용 태그(선택). Initialize에서 사용")]
    [SerializeField] private string asteroidTag = "";         // 삭제/검색용 태그

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
    [SerializeField] private GameObject shootingStarPrefab;
    [Tooltip("슈팅스타 속도 범위(단위/초)")]
    [SerializeField] private Vector2 shootingStarSpeedRange = new Vector2(4f, 7f);
    [Tooltip("플레이어 관통 보정용 오프셋 범위(축 기준 랜덤). 가로 경로는 y, 세로 경로는 x에 적용")]
    [SerializeField] private Vector2 shootingStarPassOffsetRange = new Vector2(-0.6f, 0.6f);
    [Tooltip("첫 생성까지의 최소 지연(첫 탭 이후, 초)")]
    [SerializeField] private float shootingStarInitialDelay = 10f;
    [Tooltip("최대 동시 개수 도달 시 쿨타임(초)")]
    [SerializeField] private float shootingStarPostMaxCooldown = 10f;
    [Tooltip("생성 시도 간 랜덤 추가 지연 범위(초)")]
    [SerializeField] private Vector2 shootingStarRandomDelayRange = new Vector2(0.5f, 3.0f);
    [Tooltip("슈팅스타 동시 최대 수")]
    [SerializeField] private int shootingStarMaxAlive = 6;

    [Header("블랙홀")]
    [Tooltip("블랙홀 프리팹(BlackHole 컴포넌트 포함 권장)")]
    [SerializeField] private GameObject blackHolePrefab;
    [Tooltip("블랙홀 스폰 범위 반경(월드 단위). 중심은 (0,0)")]
    [SerializeField] private float blackHoleSpawnRadius = 6f;
    [Tooltip("블랙홀 수명(초)")]
    [SerializeField] private float blackHoleLifetimeSeconds = 5f;
    [Tooltip("블랙홀 스폰 지연 범위(초). 초기/종료 후 동일 적용")]
    [SerializeField] private Vector2 blackHoleSpawnDelayRange = new Vector2(20f, 30f);
    [Tooltip("블랙홀: 공전 원 내부 판정 여유값")]
    [SerializeField] private float blackHoleOrbitEpsilon = 0.001f;

    // 내부 진행 상태
    private float _timer;
    private float _obstacleTimer;
    private float _shootingStarNextAttemptTime = -1f;
    private bool _running;
    private readonly List<Transform> _spawned = new List<Transform>(); // 관리 중인 소행성 목록
    private readonly List<Transform> _spawnedObstacles = new List<Transform>(); // 장애물 소행성 목록
    private readonly List<Transform> _spawnedShootingStars = new List<Transform>(); // 슈팅스타 목록
    // 유니티 내장 풀 딕셔너리: key = 프리팹 이름, value = ObjectPool
    private readonly Dictionary<string, ObjectPool<GameObject>> _pools = new Dictionary<string, ObjectPool<GameObject>>();
    private PlayerController _player;
    private Camera _camera;
    private bool _startSignalReceived;           // 시작 신호(첫 중심 전환) 수신 여부
    private bool _initializedAfterReset;         // Initialize 이후 상태 플래그
    // 블랙홀 진행 상태
    private CancellationTokenSource _blackHoleCts;
    private GameObject _activeBlackHole;

    // 스포너 파괴 시 진행 중 애니메이션이 있더라도 자연 종료에 맡긴다(컴포넌트가 자체 처리).

    #region 일반 공통
    private void Awake()
    {
        // 플레이어 참조(궤도 규칙 적용 시 필요)
        _player = FindFirstObjectByType<PlayerController>();
        if (_player == null)
        {
            Debug.LogWarning("[ObjectSpawner] PlayerController를 찾지 못했습니다. 궤도 규칙(4) 적용이 제한됩니다.");
        }

        // 메인 카메라 캐시(직교 카메라 가로 절반 길이로 반경 계산)
        _camera = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
        if (_camera == null)
        {
            Debug.LogWarning("[ObjectSpawner] 카메라를 찾지 못했습니다. 스폰 반경은 설정 값(spawnRadius)을 사용합니다.");
        }

        // 플레이어 중심 전환 이벤트 구독(가능 시)
        if (_player != null)
        {
            _player.OnCenterToggled += OnPlayerCenterToggled;
        }
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

        if (shootingStarPrefab != null)
        {
            var hasStar = shootingStarPrefab.GetComponent<ShootingStar>() != null;
            if (!hasStar)
            {
                Debug.LogWarning("[ObjectSpawner] shootingStarPrefab에 ShootingStar 컴포넌트가 없습니다. 런타임 스폰이 취소될 수 있습니다.", shootingStarPrefab);
            }
        }

        if (spawnInterval < 0.05f) spawnInterval = 0.05f;
        if (maxAlive < 0) maxAlive = 0;
        if (spawnRadius < 0f) spawnRadius = 0f;
        if (initialCount < 0) initialCount = 0;
        if (minSeparation < 0f) minSeparation = 0f;
        if (orbitEpsilon < 0f) orbitEpsilon = 0f;
        if (despawnMargin < 0f) despawnMargin = 0f;
        if (orbitGap < 0f) orbitGap = 0f;
        if (obstacleSpawnInterval < 0.05f) obstacleSpawnInterval = 0.05f;
        if (obstacleMinSeparation < 0f) obstacleMinSeparation = 0f;
        if (obstacleStageDuration < 1f) obstacleStageDuration = 1f;

        if (shootingStarMaxAlive < 0) shootingStarMaxAlive = 0;
        if (shootingStarSpeedRange.x < 0.1f) shootingStarSpeedRange.x = 0.1f;
        if (shootingStarSpeedRange.y < shootingStarSpeedRange.x) shootingStarSpeedRange.y = shootingStarSpeedRange.x;
        if (shootingStarInitialDelay < 0f) shootingStarInitialDelay = 0f;
        if (shootingStarPostMaxCooldown < 0f) shootingStarPostMaxCooldown = 0f;
        if (shootingStarRandomDelayRange.x < 0f) shootingStarRandomDelayRange.x = 0f;
        if (shootingStarRandomDelayRange.y < shootingStarRandomDelayRange.x) shootingStarRandomDelayRange.y = shootingStarRandomDelayRange.x;
        if (shootingStarPassOffsetRange.y < shootingStarPassOffsetRange.x) shootingStarPassOffsetRange.y = shootingStarPassOffsetRange.x;

        if (scoreFloatingTextPrefab != null)
        {
            var hasComp = scoreFloatingTextPrefab.GetComponent<ScoreFloatingText>() != null;
            if (!hasComp)
            {
                Debug.LogWarning("[ObjectSpawner] scoreFloatingTextPrefab에 ScoreFloatingText 컴포넌트가 없습니다.", scoreFloatingTextPrefab);
            }
        }

        // 블랙홀 관련 값 보정
        if (blackHoleSpawnRadius < 0f) blackHoleSpawnRadius = 0f;
        if (blackHoleLifetimeSeconds < 0f) blackHoleLifetimeSeconds = 0f;
        if (blackHoleSpawnDelayRange.x < 0f) blackHoleSpawnDelayRange.x = 0f;
        if (blackHoleSpawnDelayRange.y < blackHoleSpawnDelayRange.x) blackHoleSpawnDelayRange.y = blackHoleSpawnDelayRange.x;
        if (blackHoleOrbitEpsilon < 0f) blackHoleOrbitEpsilon = 0f;
    }
#endif

    private void OnDestroy()
    {
        if (_player != null)
        {
            _player.OnCenterToggled -= OnPlayerCenterToggled;
        }

        // 점수 텍스트는 외부 토큰 관리가 필요 없으므로 별도 정리 없음
        StopBlackHoleLoop(despawn: true);
    }

    // GameManager 이벤트에 의존하지 않고, 실제 회전 중심 전환(탭)에 의해만 시작되도록 한다.

    private void Update()
    {
        if (!_running) return;
        _timer += Time.deltaTime;
        if (_timer >= spawnInterval)
        {
            _timer = 0f;
            TrySpawn(ignoreOrbitRule: false);
        }

        // 장애물 스폰 타이머
        _obstacleTimer += Time.deltaTime;
        if (_obstacleTimer >= obstacleSpawnInterval)
        {
            _obstacleTimer = 0f;
            TrySpawnObstacle();
        }

        UpdateShootingStarSchedule();
    }

    /// <summary>
    /// 스포너 초기화: 기존 소행성 정리 후 초기 배치를 생성하고 스폰을 시작한다.
    /// </summary>
    public void Initialize()
    {
        // 기존 소행성 제거
        RemoveAllAsteroids();

        // 초기 배치 생성(규칙 4 적용: 공전 범위 제외)
        for (int i = 0; i < Mathf.Max(0, initialCount); i++)
        {
            TrySpawn(ignoreOrbitRule: false);
        }

        // 시작 신호 대기 상태로 리셋(씬 진입/리셋 후 탭으로 중심 전환 시까지 주기 스폰 보류)
        _startSignalReceived = false;
        _running = false; // 주기 스폰은 보류
        _timer = 0f;
        _obstacleTimer = 0f;
        _shootingStarNextAttemptTime = -1f;
        
        _initializedAfterReset = true;
        Debug.Log("[ObjectSpawner] 초기화 완료: 초기 배치 생성 후 시작 신호(첫 중심 전환) 대기");

        // 블랙홀 루프 정지 및 정리
        StopBlackHoleLoop(despawn: true);

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
            try { _player.OnCenterToggled -= OnPlayerCenterToggled; } catch { }
            _player.OnCenterToggled += OnPlayerCenterToggled;
        }
    }

    // 시작 신호 처리: 주기 스폰 시작(Initialize에서 이미 초기 배치 완료)
    private void HandleStartSignal()
    {
        if (_startSignalReceived) return;
        _startSignalReceived = true;
        // 스폰 시작
        _running = true;
        _timer = 0f;
        Debug.Log("[ObjectSpawner] 시작 신호 감지: 주기 스폰 시작");
    }

    // 플레이어 중심 전환 이벤트 처리(첫 신호만 처리 후 구독 해제)
    private void OnPlayerCenterToggled(bool isSun)
    {
        if (!_initializedAfterReset || _startSignalReceived) return;
        HandleStartSignal();
        // 첫 생성은 초기 지연 + 랜덤 추가 지연 이후
        float extra = Random.Range(shootingStarRandomDelayRange.x, shootingStarRandomDelayRange.y);
        _shootingStarNextAttemptTime = Time.time + shootingStarInitialDelay + Mathf.Max(0f, extra);
        // 첫 신호 처리 후 더 이상 필요 없으므로 구독 해제
        if (_player != null)
        {
            _player.OnCenterToggled -= OnPlayerCenterToggled;
        }

        // 블랙홀 루프 시작(초기 20~30초 랜덤 지연 후 스폰)
        StartBlackHoleLoop();
    }
    #endregion // 일반 공통

    #region 슈팅스타
    private void UpdateShootingStarSchedule()
    {
        if (_shootingStarNextAttemptTime < 0f) return; // 아직 시작 신호 전
        if (Time.time < _shootingStarNextAttemptTime) return;

        // 최대 개수 도달 시 쿨타임 설정(즉시 생성 금지)
        if (_spawnedShootingStars.Count >= shootingStarMaxAlive)
        {
            float extra = Random.Range(shootingStarRandomDelayRange.x, shootingStarRandomDelayRange.y);
            _shootingStarNextAttemptTime = Time.time + shootingStarPostMaxCooldown + Mathf.Max(0f, extra);
            return;
        }

        // 생성 시도
        bool spawned = TrySpawnShootingStar();
        if (spawned)
        {
            // 스폰 직후 최대치에 도달하면 쿨타임 진입
            if (_spawnedShootingStars.Count >= shootingStarMaxAlive)
            {
                float extra = Random.Range(shootingStarRandomDelayRange.x, shootingStarRandomDelayRange.y);
                _shootingStarNextAttemptTime = Time.time + shootingStarPostMaxCooldown + Mathf.Max(0f, extra);
            }
            else
            {
                float extra = Random.Range(shootingStarRandomDelayRange.x, shootingStarRandomDelayRange.y);
                _shootingStarNextAttemptTime = Time.time + Mathf.Max(0f, extra);
            }
        }
        else
        {
            // 실패 시 짧게 재시도
            _shootingStarNextAttemptTime = Time.time + 0.5f;
        }
    }

    private bool TrySpawnShootingStar()
    {
        if (shootingStarPrefab == null) return false;
        CleanupShootingList();
        if (_spawnedShootingStars.Count >= shootingStarMaxAlive) return false;

        // 직교 카메라라면: 카메라 뷰 사각형과 (뷰+마진) 사각형 사이의 띠 영역에서 시작 지점을 선택
        if (_camera != null && _camera.orthographic)
        {
            if (!TryGetPointInCameraBand(out Vector3 start)) return false;
            // 도착 지점은 (0,0) 기준 거울 위치로 설정
            Vector3 end = new Vector3(-start.x, -start.y, start.z);

            // 경로 방향(가로/세로)을 판정 후, u=0.5에서 플레이어 축(+오프셋)을 통과하도록 passPoint를 설정
            Vector3 playerPos = Vector3.zero;
            if (_player != null) { var pt = _player.transform.position; playerPos = new Vector3(pt.x, pt.y, 0f); }
            float passOff = Random.Range(shootingStarPassOffsetRange.x, shootingStarPassOffsetRange.y);
            Vector2 d = end - start;
            bool horizontal = Mathf.Abs(d.x) >= Mathf.Abs(d.y);
            Vector3 passPoint;
            if (horizontal)
            {
                // 가로 경로: y를 플레이어 축으로 정렬, x는 중점
                float y = playerPos.y + passOff;
                float xMid = 0.5f * (start.x + end.x);
                passPoint = new Vector3(xMid, y, 0f);
            }
            else
            {
                // 세로 경로: x를 플레이어 축으로 정렬, y는 중점
                float x = playerPos.x + passOff;
                float yMid = 0.5f * (start.y + end.y);
                passPoint = new Vector3(x, yMid, 0f);
            }

            // 속도 선택
            float spd = Random.Range(Mathf.Min(shootingStarSpeedRange.x, shootingStarSpeedRange.y), Mathf.Max(shootingStarSpeedRange.x, shootingStarSpeedRange.y));
            SpawnShootingAt(start, end, passPoint, spd);
            return true;
        }

        // 폴백(원근 카메라 등): 카메라 중심 원 둘레에서 시작점을 고르고, 도착은 (0,0) 기준 거울 위치
        Vector3 camCenter = Vector3.zero;
        if (_camera != null)
        {
            var cp = _camera.transform.position; camCenter = new Vector3(cp.x, cp.y, 0f);
        }
        float baseR = GetSpawnRadiusWorld() + Mathf.Max(0f, despawnMargin) + 0.5f;
        float ang = Random.Range(0f, Mathf.PI * 2f);
        Vector2 dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
        Vector3 startFallback = camCenter + new Vector3(dir.x, dir.y, 0f) * baseR;
        Vector3 endFallback = new Vector3(-startFallback.x, -startFallback.y, startFallback.z);
        // u=0.5 통과 지점(passPoint) 계산(축 정렬)
        Vector3 passPointFb;
        {
            Vector3 playerPos = Vector3.zero;
            if (_player != null) { var pt = _player.transform.position; playerPos = new Vector3(pt.x, pt.y, 0f); }
            float passOff = Random.Range(shootingStarPassOffsetRange.x, shootingStarPassOffsetRange.y);
            Vector2 d2 = endFallback - startFallback;
            bool horizontal2 = Mathf.Abs(d2.x) >= Mathf.Abs(d2.y);
            if (horizontal2)
            {
                float y = playerPos.y + passOff;
                float xMid = 0.5f * (startFallback.x + endFallback.x);
                passPointFb = new Vector3(xMid, y, 0f);
            }
            else
            {
                float x = playerPos.x + passOff;
                float yMid = 0.5f * (startFallback.y + endFallback.y);
                passPointFb = new Vector3(x, yMid, 0f);
            }
        }
        float spdFb = Random.Range(Mathf.Min(shootingStarSpeedRange.x, shootingStarSpeedRange.y), Mathf.Max(shootingStarSpeedRange.x, shootingStarSpeedRange.y));
        SpawnShootingAt(startFallback, endFallback, passPointFb, spdFb);
        return true;
    }
    #endregion // 슈팅스타

    /// <summary>
    /// 외부에서 수동으로 스폰을 시작할 때 사용
    /// </summary>
    public void Begin()
    {
        _running = true; // 수동 시작: 스폰 시작
        _timer = 0f;
        Debug.Log("[ObjectSpawner] 스폰 시작");

        // 수동 시작 시에도 블랙홀 루프를 함께 시작
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
    }

    #region 블랙홀
    // -------------------- 블랙홀 루프(UniTask) --------------------
    private void StartBlackHoleLoop()
    {
        if (_blackHoleCts != null) return; // 이미 동작 중
        _blackHoleCts = new CancellationTokenSource();
        RunBlackHoleLoopAsync(_blackHoleCts.Token).Forget();
        Debug.Log("[ObjectSpawner] 블랙홀 스폰 루프 시작");
    }

    private void StopBlackHoleLoop(bool despawn)
    {
        if (_blackHoleCts != null)
        {
            _blackHoleCts.Cancel();
            _blackHoleCts.Dispose();
            _blackHoleCts = null;
        }
        if (despawn)
        {
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
            const int kMaxTry = 32;
            bool ok = false; Vector3 pos = Vector3.zero;
            for (int i = 0; i < kMaxTry; i++)
            {
                if (TryGetBlackHoleSpawnPosition(out pos)) { ok = true; break; }
            }
            if (!ok) continue; // 다음 사이클로

            SpawnBlackHoleAt(pos);

            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(Mathf.Max(0.01f, blackHoleLifetimeSeconds)), cancellationToken: ct);
            }
            catch (OperationCanceledException) { break; }

            DespawnBlackHoleNow();
        }
    }

    private float GetBlackHoleRandomDelay()
    {
        float a = Mathf.Max(0f, blackHoleSpawnDelayRange.x);
        float b = Mathf.Max(a, blackHoleSpawnDelayRange.y);
        return Random.Range(a, b);
    }

    private bool TryGetBlackHoleSpawnPosition(out Vector3 pos)
    {
        // (0,0) 중심, 반경 = blackHoleSpawnRadius 내부 균등 샘플링
        Vector2 r = Random.insideUnitCircle * Mathf.Max(0f, blackHoleSpawnRadius);
        pos = new Vector3(r.x, r.y, 0f);

        // 플레이어 공전 원 내부 금지(현재 중심 기준)
        if (_player != null && _player.CurrentCenter != null)
        {
            Vector3 center = _player.CurrentCenter.position; center.z = 0f;
            float orbitR = Mathf.Max(0f, _player.Distance);
            float or2 = (orbitR - Mathf.Max(0f, blackHoleOrbitEpsilon));
            or2 = or2 * or2;
            float d2 = (pos - center).sqrMagnitude;
            if (d2 < or2)
            {
                return false; // 공전 원 내부 금지
            }
        }
        return true;
    }

    private void SpawnBlackHoleAt(Vector3 pos)
    {
        if (blackHolePrefab == null)
        {
            Debug.LogWarning("[ObjectSpawner] blackHolePrefab이 설정되지 않아 블랙홀을 스폰할 수 없습니다.");
            return;
        }
        if (_activeBlackHole != null) return;

        _activeBlackHole = Instantiate(blackHolePrefab, pos, Quaternion.identity);
        _activeBlackHole.transform.SetParent(transform, true);
        // 2D 평면 보정
        var e = _activeBlackHole.transform.eulerAngles;
        _activeBlackHole.transform.eulerAngles = new Vector3(0f, 0f, e.z);
    }

    private void DespawnBlackHoleNow()
    {
        if (_activeBlackHole == null) return;
        Destroy(_activeBlackHole);
        _activeBlackHole = null;
    }
    #endregion // 블랙홀

    #region 소행성
    // 소행성 한 개 스폰 시도
    private void TrySpawn(bool ignoreOrbitRule)
    {
        if (asteroidPrefab == null) return;
        CleanupList();
        if (_spawned.Count >= maxAlive) return;

        // 유효 위치 탐색(최대 시도 횟수 제한)
        const int kMaxTries = 24;
        for (int t = 0; t < kMaxTries; t++)
        {
            if (TryGetSpawnPosition(ignoreOrbitRule, out Vector3 pos))
            {
                SpawnAt(pos);
                return;
            }
        }
        // 유효 위치를 찾지 못한 경우(드문 상황)
    }

    #region 장애물 소행성
    // 장애물 소행성 스폰 (별도 영역에서 상세 구현)
    private void TrySpawnObstacle()
    {
        if (obstacleAsteroidPrefab == null) return;
        CleanupObstacleList();

        int maxObstacles = GetObstacleMaxAlive();
        if (_spawnedObstacles.Count >= maxObstacles) return;

        const int kMaxTries = 24;
        for (int t = 0; t < kMaxTries; t++)
        {
            if (TryGetObstacleSpawnPosition(out Vector3 pos))
            {
                SpawnObstacleAt(pos);
                return;
            }
        }
    }

    #endregion // 장애물 소행성

    // 카메라 뷰 사각형과 (뷰+마진) 사각형 사이의 띠 영역 내 임의의 점을 선택(직교 카메라 전용)
    // 반환: 성공 시 true, pos는 월드 좌표
    private bool TryGetPointInCameraBand(out Vector3 pos)
    {
        pos = Vector3.zero;
        if (_camera == null || !_camera.orthographic) return false;

        var cp = _camera.transform.position;
        float hi = _camera.orthographicSize;                  // inner half-height
        float wi = _camera.orthographicSize * _camera.aspect; // inner half-width
        float margin = Mathf.Max(0f, despawnMargin);
        float ho = hi + margin; // outer half-height
        float wo = wi + margin; // outer half-width

        const int kMaxTries = 64;
        for (int i = 0; i < kMaxTries; i++)
        {
            float x = Random.Range(cp.x - wo, cp.x + wo);
            float y = Random.Range(cp.y - ho, cp.y + ho);
            float dx = Mathf.Abs(x - cp.x);
            float dy = Mathf.Abs(y - cp.y);
            bool insideOuter = (dx <= wo && dy <= ho);
            bool outsideInner = (dx > wi || dy > hi);
            if (insideOuter && outsideInner)
            {
                pos = new Vector3(x, y, 0f);
                return true;
            }
        }

        // 드물게 실패 시, 외곽 테두리 중 하나에서 선택
        int edge = Random.Range(0, 4);
        switch (edge)
        {
            case 0: pos = new Vector3(Random.Range(cp.x - wo, cp.x + wo), cp.y + ho, 0f); break; // top
            case 1: pos = new Vector3(Random.Range(cp.x - wo, cp.x + wo), cp.y - ho, 0f); break; // bottom
            case 2: pos = new Vector3(cp.x - wo, Random.Range(cp.y - ho, cp.y + ho), 0f); break; // left
            default: pos = new Vector3(cp.x + wo, Random.Range(cp.y - ho, cp.y + ho), 0f); break; // right
        }
        return true;
    }

    private bool TryGetObstacleSpawnPosition(out Vector3 pos)
    {
        Vector3 c = Vector3.zero;
        float r = GetSpawnRadiusWorld();
        Vector2 rnd = Random.insideUnitCircle * r;
        pos = c + new Vector3(rnd.x, rnd.y, 0f);

        // 플레이어 공전 원 내부 금지 규칙은 동일 적용
        if (_player != null && _player.CurrentCenter != null)
        {
            Vector3 oc = _player.CurrentCenter.position;
            // 플레이어 공전 반경 + 끝 오브젝트(지구/태양)의 크기를 고려한 갭을 더해 금지 영역을 확장한다.
            float orbitR = Mathf.Max(0f, _player.Distance + orbitGap);
            float d2 = (pos - oc).sqrMagnitude;
            if (d2 < (orbitR - orbitEpsilon) * (orbitR - orbitEpsilon))
            {
                return false;
            }
        }

        // 장애물 간 최소 간격 3
        float minSep2 = obstacleMinSeparation * obstacleMinSeparation;
        for (int i = 0; i < _spawnedObstacles.Count; i++)
        {
            var tr = _spawnedObstacles[i];
            if (tr == null) continue;
            if ((tr.position - pos).sqrMagnitude < minSep2)
                return false;
        }

        // 일반 소행성과의 최소 간격은 일반 소행성 규칙(minSeparation)을 따른다
        float generalSep2 = minSeparation * minSeparation;
        for (int i = 0; i < _spawned.Count; i++)
        {
            var tr = _spawned[i];
            if (tr == null) continue;
            if ((tr.position - pos).sqrMagnitude < generalSep2)
                return false;
        }

        return true;
    }

    private void SpawnObstacleAt(Vector3 pos)
    {
        var go = GetFromPool(obstacleAsteroidPrefab);
        go.transform.SetParent(transform, false);
        go.transform.position = pos;

        float z = Random.Range(0f, 360f);
        go.transform.rotation = Quaternion.Euler(0f, 0f, z);

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

    // 스폰 위치 생성 규칙 적용
    private bool TryGetSpawnPosition(bool ignoreOrbitRule, out Vector3 pos)
    {
        // 요구사항 1,2: 스폰 중심 = 월드 원점(0,0), 반경 = 화면 가로 길이 절반(직교 카메라 기준)
        Vector3 c = Vector3.zero;
        float r = GetSpawnRadiusWorld();
        // 요구사항 3: 원의 범위(내부)에서 균등 분포로 선택
        Vector2 rnd = Random.insideUnitCircle * r;
        pos = c + new Vector3(rnd.x, rnd.y, 0f);

        // 규칙 4: 플레이어 공전 원(현재 중심 기준) 내부 금지 — 초기/일반 스폰 모두 적용
        if (!ignoreOrbitRule && _player != null && _player.CurrentCenter != null)
        {
            Vector3 oc = _player.CurrentCenter.position;
            // 플레이어 공전 반경 + 끝 오브젝트(지구/태양) 크기 고려 갭 반영
            float orbitR = Mathf.Max(0f, _player.Distance + orbitGap);
            float d2 = (pos - oc).sqrMagnitude;
            if (d2 < (orbitR - orbitEpsilon) * (orbitR - orbitEpsilon))
            {
                return false; // 공전 원 내부는 배치 불가
            }
        }

        // 규칙 5: 기존 소행성과 최소 간격 유지(0.5)
        float minSep2 = minSeparation * minSeparation;
        for (int i = 0; i < _spawned.Count; i++)
        {
            var tr = _spawned[i];
            if (tr == null) continue;
            if ((tr.position - pos).sqrMagnitude < minSep2)
                return false;
        }

        return true;
    }

    // 카메라 기준 스폰 반경 계산: 직교 카메라의 세로 절반(orthographicSize) * 가로비(aspect) = 가로 절반 길이
    private float GetSpawnRadiusWorld()
    {
        if (_camera != null && _camera.orthographic)
        {
            return _camera.orthographicSize * _camera.aspect;
        }
        // 폴백: 설정된 spawnRadius 사용(원근 카메라 또는 미발견 시)
        return spawnRadius;
    }

    // 외부 접근용: 스폰 반경(월드 단위) 반환
    public float GetSpawnRadius()
    {
        return GetSpawnRadiusWorld();
    }

    /// <summary>
    /// 카메라 화면 반경(가로 절반 길이) + 여유 마진 기준으로 바깥인지 판정한다.
    /// 중심은 카메라의 현재 XY 위치를 사용하며, 카메라가 없으면 월드 원점을 사용한다.
    /// </summary>
    public bool IsOutsideDespawnBounds(Vector3 worldPos)
    {
        Vector2 center = Vector2.zero;
        if (_camera != null)
        {
            var cp = _camera.transform.position;
            center = new Vector2(cp.x, cp.y);
        }
        float r = GetSpawnRadiusWorld() + Mathf.Max(0f, despawnMargin);
        Vector2 d = new Vector2(worldPos.x, worldPos.y) - center;
        return d.sqrMagnitude > r * r;
    }

    /// <summary>
    /// 슈팅스타 전용: 카메라 뷰를 사각형으로 보고, margin(월드 단위)을 사방으로 확장한 영역을 벗어나면 true를 반환한다.
    /// - 직교 카메라인 경우에만 정확한 사각형 판정. 그 외에는 기존 원형 판정으로 폴백한다.
    /// - 다른 엔티티(소행성/플레이어)는 기존 원형 판정을 유지하므로, 이 메서드는 ShootingStar에서만 사용한다.
    /// </summary>
    public bool IsOutsideCameraRectWithMargin(Vector3 worldPos)
    {
        if (_camera != null && _camera.orthographic)
        {
            // 카메라 중심 기준, 가시 영역의 절반 너비/높이에 마진을 더해 확장 사각형을 구성한다.
            var cp = _camera.transform.position;
            float halfHeight = _camera.orthographicSize + Mathf.Max(0f, despawnMargin);
            float halfWidth = _camera.orthographicSize * _camera.aspect + Mathf.Max(0f, despawnMargin);

            float dx = worldPos.x - cp.x;
            float dy = worldPos.y - cp.y;
            if (Mathf.Abs(dx) > halfWidth) return true;
            if (Mathf.Abs(dy) > halfHeight) return true;
            return false;
        }

        // 원근 카메라 등 특수 케이스에서는 기존 원형 판정을 사용(보수적 폴백)
        return IsOutsideDespawnBounds(worldPos);
    }

    // ---------- 풀 헬퍼: 유니티 내장 풀(ObjectPool) 딕셔너리 ----------
    private ObjectPool<GameObject> GetPoolForPrefab(GameObject prefab)
    {
        if (prefab == null) return null;
        string key = prefab.name;
        if (_pools.TryGetValue(key, out var pool)) return pool;

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
                tag.SetKey(key);
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

        _pools[key] = pool;
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
        if (tag == null || string.IsNullOrEmpty(tag.PoolKey) || !_pools.TryGetValue(tag.PoolKey, out var pool))
        {
            Debug.LogWarning("[ObjectSpawner] 풀 키를 찾지 못해 오브젝트를 비활성화만 합니다.", go);
            go.SetActive(false);
            go.transform.SetParent(transform, false);
            return;
        }
        pool.Release(go);
    }

    private void SpawnAt(Vector3 pos)
    {
        var go = GetFromPool(asteroidPrefab);
        go.transform.SetParent(transform, false);
        go.transform.position = pos;
        // 스폰 시 Z 회전값을 랜덤으로 부여하여 소행성 방향을 다양화
        float z = Random.Range(0f, 360f);
        go.transform.rotation = Quaternion.Euler(0f, 0f, z);

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

        _spawned.Add(go.transform);
    }
    #endregion // 소행성

    // 점수 팝업 표시
    public void ShowScorePopup(int amount, Vector3 worldPos)
    {
        if (scoreFloatingTextPrefab == null)
        {
            Debug.LogWarning("[ObjectSpawner] scoreFloatingTextPrefab이 설정되지 않아 점수 텍스트를 표시할 수 없습니다.");
            return;
        }
        var go = GetFromPool(scoreFloatingTextPrefab);
        go.transform.SetParent(transform, false);

        Vector3 startPos = worldPos;
        go.transform.position = startPos;

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

    // 모든 소행성 제거(태그 기반 월드 정리 + 풀/목록 정리)
    private void RemoveAllAsteroids()
    {
        // 태그가 지정되었다면 해당 태그의 오브젝트를 먼저 제거
        if (!string.IsNullOrEmpty(asteroidTag))
        {
            var tagged = GameObject.FindGameObjectsWithTag(asteroidTag);
            foreach (var go in tagged)
            {
                if (go != null) Destroy(go);
            }
        }

        // 풀/목록에 등록된 오브젝트 정리 후 풀에 반환
        CleanupList();
        for (int i = _spawned.Count - 1; i >= 0; i--)
        {
            var tr = _spawned[i];
            if (tr == null) continue;
            ReturnToPool(tr.gameObject);
        }
        _spawned.Clear();

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

    // 목록 내 null 항목 정리
    private void CleanupList()
    {
        for (int i = _spawned.Count - 1; i >= 0; i--)
        {
            if (_spawned[i] == null) _spawned.RemoveAt(i);
        }
    }

    private void CleanupObstacleList()
    {
        for (int i = _spawnedObstacles.Count - 1; i >= 0; i--)
        {
            if (_spawnedObstacles[i] == null) _spawnedObstacles.RemoveAt(i);
        }
    }

    private void CleanupShootingList()
    {
        for (int i = _spawnedShootingStars.Count - 1; i >= 0; i--)
        {
            if (_spawnedShootingStars[i] == null) _spawnedShootingStars.RemoveAt(i);
        }
    }

    // 소행성이 파괴될 때: 목록에서 제거
    public void NotifyDestroyed(Transform tr)
    {
        if (tr == null) return;
        _spawned.Remove(tr);
        _spawnedObstacles.Remove(tr);
    }

    // 외부(소행성)에서 종료 요청: 풀로 반환
    public void Despawn(Transform tr)
    {
        if (tr == null) return;
        var go = tr.gameObject;
        // 어떤 풀인지 구분하여 반환
        if (_spawned.Remove(tr) || _spawnedObstacles.Remove(tr) || _spawnedShootingStars.Remove(tr))
        {
            ReturnToPool(go);
            return;
        }
        // 목록에 없더라도 풀로 반환(외부에서 직접 호출된 경우 대비)
        ReturnToPool(go);
    }

    

    private void SpawnShootingAt(Vector3 start, Vector3 end, Vector3 passPoint, float speed)
    {
        var go = GetFromPool(shootingStarPrefab);
        go.transform.SetParent(transform, false);
        go.transform.position = start;
        go.transform.rotation = Quaternion.identity;

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
        star.Launch(start, end, passPoint, speed);
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

    private void OnDrawGizmos()
    {
        // 생성 가능 영역을 Scene 뷰에서 시각화한다.
        // - 외곽: 화면 가로 절반 길이를 반경으로 하는 원(월드 원점 중심)
        // - 내부 금지 구역은 요구에 따라 표시하지 않음

        // 기즈모 행렬 초기화(외부에서 변경되었을 수 있음에 대비)
        Gizmos.matrix = Matrix4x4.identity;

        // 카메라 기반 반경 계산 (런타임과 동일한 로직에 최대한 맞춤)
        float radius = GetGizmoSpawnRadius();

        // (제거됨) 외곽 생성 가능 반경 Gizmo — 런타임 시각화로 대체

        // 월드 원점 마커(작은 점)
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.9f); // 하늘색: 월드 원점
        Gizmos.DrawSphere(Vector3.zero, Mathf.Max(0.05f, radius * 0.02f));

        // 디스폰 경계(카메라 중심 + 반경 + 마진)
        if (TryGetGizmoDespawnCircle(out Vector3 c, out float r))
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.9f); // 주황색: 디스폰 경계
            Gizmos.DrawWireSphere(c, r);
            // 카메라 중심 마커
            Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.9f);
            Gizmos.DrawSphere(c, Mathf.Max(0.04f, r * 0.015f));
        }

        // 슈팅스타용 사각형 디스폰 경계(카메라 뷰 + 마진)
        if (TryGetGizmoDespawnRect(out Vector3 rc, out float halfW, out float halfH))
        {
            Gizmos.color = new Color(0.7f, 0.2f, 1f, 0.9f); // 보라색: 사각형 경계
            DrawWireRect(rc, halfW, halfH);
        }

        // 블랙홀 스폰 범위(월드 원점 기준, 직렬화 반경)
        // 블랙홀 스폰 반경(항상 표시, 파란색 고정)
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(Vector3.zero, Mathf.Max(0f, blackHoleSpawnRadius));
    }

    // 에디터/플레이 공통: 기즈모 반경 계산을 안정화
    private float GetGizmoSpawnRadius()
    {
        // 플레이 중에는 런타임 계산과 동일하게 처리
        if (Application.isPlaying)
        {
            return GetSpawnRadiusWorld();
        }

        // 에디터(미플레이): 메인 카메라가 직교면 그것을 사용
        var cam = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
        if (cam != null && cam.orthographic)
        {
            return cam.orthographicSize * cam.aspect;
        }

        // 에디터에서 메인 카메라가 원근인 경우, 가능한 직교 카메라를 탐색
        // 실패 시 설정값(spawnRadius) 폴백
        try
        {
            var allCams = Camera.allCameras;
            for (int i = 0; i < allCams.Length; i++)
            {
                if (allCams[i] != null && allCams[i].orthographic)
                {
                    return allCams[i].orthographicSize * allCams[i].aspect;
                }
            }
        }
        catch
        {
            // 에디터/런타임 환경에 따라 접근 실패 가능 — 폴백 사용
        }

        return spawnRadius;
    }

    // 기즈모용 디스폰 경계 계산(카메라 중심 + 반경 + 마진)
    private bool TryGetGizmoDespawnCircle(out Vector3 center, out float radius)
    {
        center = Vector3.zero;
        radius = 0f;

        Camera cam = null;
        if (Application.isPlaying)
        {
            cam = _camera;
        }
        else
        {
            cam = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
        }

        if (cam == null)
        {
            // 카메라가 없으면 원점 기준으로라도 표시
            center = Vector3.zero;
            radius = GetGizmoSpawnRadius() + Mathf.Max(0f, despawnMargin);
            return true;
        }

        center = cam.transform.position;
        // Z는 0으로 맞춰 2D 평면에 그린다
        center.z = 0f;
        float baseR;
        if (cam.orthographic)
        {
            baseR = cam.orthographicSize * cam.aspect;
        }
        else
        {
            baseR = GetGizmoSpawnRadius(); // 원근 카메라면 대략적인 값 사용
        }
        radius = baseR + Mathf.Max(0f, despawnMargin);
        return true;
    }

    // 카메라 직교 뷰 기준 사각형 디스폰 경계(슈팅스타용) 계산: 성공 시 true
    private bool TryGetGizmoDespawnRect(out Vector3 center, out float halfWidth, out float halfHeight)
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
}
