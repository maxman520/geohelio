using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

// 소행성 스포너: 초기화 시 초기 배치를 만들고, 주기적으로 스폰을 수행한다.
// 동작 규칙 개요:
// 1) 자동 시작하지 않음. 2) Initialize 시 기존 소행성 정리 후 초기 개수 배치
// 3) Initialize 끝에서 스폰 시작 4) 플레이어 공전 원 안쪽은 스폰 금지(규칙 4)
// 5) 기존 소행성과 최소 간격 0.5 유지(규칙 5) 6) 초기 배치는 규칙 4만 무시
// 7) 게임 진행 중에는 주기적으로 스폰, 중지 시 스폰 정지
public class ObjectSpawner : MonoBehaviour
{
    [Header("설정")]
    [SerializeField] private GameObject asteroidPrefab;      // 소행성 프리팹
    [SerializeField] private float spawnInterval = 1.0f;     // 스폰 간격(초)
    [SerializeField] private int maxAlive = 50;               // 최대 동시 소행성 수
    [SerializeField] private float spawnRadius = 6f;          // 스폰 반경(중심 기준)
    [SerializeField] private int initialCount = 8;            // 초기 생성 개수(Initialize 시 사용)
    [SerializeField] private Transform center;                // 스폰 중심(없으면 GameManager의 현재 중심 또는 월드 원점)
    [Tooltip("기존 월드에 남아있는 소행성 제거용 태그(선택). Initialize에서 사용")]
    [SerializeField] private string asteroidTag = "";         // 삭제/검색용 태그

    [Header("규칙")]
    [SerializeField] private float minSeparation = 0.5f;      // 최소 간격 0.5 유지(규칙 5)
    [SerializeField] private float orbitEpsilon = 0.001f;     // 공전 원 내부 판정 여유값

    // 내부 진행 상태
    private float _timer;
    private bool _running;
    private readonly List<Transform> _spawned = new List<Transform>(); // 관리 중인 소행성 목록
    private readonly Queue<GameObject> _pool = new Queue<GameObject>(); // 풀(비활성 대기)
    private PlayerController _player;

    private void Awake()
    {
        // 중심 자동 보완: GameManager에서 현재 중심을 받아 사용
        if (center == null)
        {
            var gm = FindFirstObjectByType<GameManager>();
            if (gm != null)
            {
                var c = gm.GetCurrentOrbitCenter();
                if (c != null) center = c;
            }
        }

        // 플레이어 참조(궤도 규칙 적용 시 필요)
        _player = FindFirstObjectByType<PlayerController>();
        if (_player == null)
        {
            Debug.LogWarning("[ObjectSpawner] PlayerController를 찾지 못했습니다. 궤도 규칙(4) 적용이 제한됩니다.");
        }
    }

    private void Update()
    {
        if (!_running) return;
        _timer += Time.deltaTime;
        if (_timer >= spawnInterval)
        {
            _timer = 0f;
            TrySpawn(ignoreOrbitRule: false);
        }
    }

    /// <summary>
    /// 스포너 초기화: 기존 소행성 정리 후 초기 배치를 생성하고 스폰을 시작한다.
    /// </summary>
    public void Initialize()
    {
        // 기존 소행성 제거
        RemoveAllAsteroids();

        // 초기 배치 생성(규칙 4만 무시하고 5는 적용)
        for (int i = 0; i < Mathf.Max(0, initialCount); i++)
        {
            TrySpawn(ignoreOrbitRule: true);
        }

        // 스폰 시작
        _running = true;
        _timer = 0f;
        Debug.Log("[ObjectSpawner] 초기화 완료: 초기 배치 생성 및 스폰 시작");
    }

    /// <summary>
    /// 외부에서 수동으로 스폰을 시작할 때 사용(테스트/디버그용).
    /// </summary>
    public void Begin()
    {
        _running = true; // 수동 시작: 스폰 시작
        _timer = 0f;
        Debug.Log("[ObjectSpawner] 스폰 시작");
    }

    /// <summary>
    /// 스폰 중지(게임 일시정지/종료 등).
    /// </summary>
    public void Stop()
    {
        _running = false;
        Debug.Log("[ObjectSpawner] 스폰 중지");
    }

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

    // 스폰 위치 생성 규칙 적용
    private bool TryGetSpawnPosition(bool ignoreOrbitRule, out Vector3 pos)
    {
        Vector3 c = center != null ? center.position : Vector3.zero;
        Vector2 rnd = Random.insideUnitCircle.normalized * spawnRadius;
        pos = c + new Vector3(rnd.x, rnd.y, 0f);

        // 규칙 4: 플레이어 공전 원 내부 금지 (Initialize 초기 배치 시 무시 가능)
        if (!ignoreOrbitRule && _player != null && _player.CurrentCenter != null)
        {
            Vector3 oc = _player.CurrentCenter.position;
            float r = Mathf.Max(0f, _player.Distance);
            float d2 = (pos - oc).sqrMagnitude;
            if (d2 < (r - orbitEpsilon) * (r - orbitEpsilon))
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

    private void SpawnAt(Vector3 pos)
    {
        var go = GetFromPool();
        go.transform.SetParent(transform, false);
        go.transform.position = pos;
        go.transform.rotation = Quaternion.identity;
        go.SetActive(true);

        // 구성 요소 준비
        var asteroid = go.GetComponent<Asteroid>();
        if (asteroid == null) asteroid = go.AddComponent<Asteroid>();
        asteroid.Initialize(this);
        asteroid.ResetForSpawn();

        _spawned.Add(go.transform);
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
            InternalDespawn(tr.gameObject);
        }
        _spawned.Clear();
    }

    // 목록 내 null 항목 정리
    private void CleanupList()
    {
        for (int i = _spawned.Count - 1; i >= 0; i--)
        {
            if (_spawned[i] == null) _spawned.RemoveAt(i);
        }
    }

    // 소행성이 파괴될 때: 목록에서 제거
    public void NotifyDestroyed(Transform tr)
    {
        if (tr == null) return;
        _spawned.Remove(tr);
    }

    // 외부(소행성)에서 종료 요청: 풀로 반환
    public void Despawn(Transform tr)
    {
        if (tr == null) return;
        InternalDespawn(tr.gameObject);
        _spawned.Remove(tr);
    }

    private GameObject GetFromPool()
    {
        if (_pool.Count > 0)
        {
            var go = _pool.Dequeue();
            return go;
        }
        return Instantiate(asteroidPrefab);
    }

    private void InternalDespawn(GameObject go)
    {
        if (go == null) return;
        go.SetActive(false);
        go.transform.SetParent(transform, false);
        _pool.Enqueue(go);
    }
}

