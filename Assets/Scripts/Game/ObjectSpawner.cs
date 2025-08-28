using UnityEngine;

// 소행성 스포너: 간단한 타이머 기반 스폰. 비동기 필요 시 UniTask로 전환 가능.
public class ObjectSpawner : MonoBehaviour
{
    [Header("설정")]
    [SerializeField] private GameObject asteroidPrefab; // 소행성 프리팹
    [SerializeField] private float spawnInterval = 1.0f; // 스폰 주기(초)
    [SerializeField] private int maxAlive = 50;          // 최대 동시 소행성 수
    [SerializeField] private float spawnRadius = 6f;     // 스폰 반경(중심 기준)
    [SerializeField] private Transform center;           // 스폰 기준 중심(없으면 월드 원점)

    private float _timer;
    private int _alive;
    private bool _running;

    private void Awake()
    {
        if (center == null)
        {
            // 게임 진행에서 중심은 GameManager/PlayerController가 제공하는 중심으로 갱신 가능
            var gm = FindFirstObjectByType<GameManager>();
            if (gm != null)
            {
                var c = gm.GetCurrentOrbitCenter();
                if (c != null) center = c;
            }
        }
    }

    private void Update()
    {
        if (!_running) return;
        _timer += Time.deltaTime;
        if (_timer >= spawnInterval)
        {
            _timer = 0f;
            TrySpawn();
        }
    }

    public void Begin()
    {
        _running = true;
        _timer = 0f;
    }

    public void Stop()
    {
        _running = false;
    }

    private void TrySpawn()
    {
        if (asteroidPrefab == null) return;
        if (_alive >= maxAlive) return;

        Vector3 c = center != null ? center.position : Vector3.zero;
        Vector2 rnd = Random.insideUnitCircle.normalized * spawnRadius;
        Vector3 pos = c + new Vector3(rnd.x, rnd.y, 0f);
        var go = Instantiate(asteroidPrefab, pos, Quaternion.identity);
        _alive++;

        // 간단한 수명 관리(임시): 10~20초 뒤 파괴하여 alive 감소
        float life = Random.Range(10f, 20f);
        Destroy(go, life);
        Invoke(nameof(DecrementAlive), life);
    }

    private void DecrementAlive()
    {
        _alive = Mathf.Max(0, _alive - 1);
    }
}
