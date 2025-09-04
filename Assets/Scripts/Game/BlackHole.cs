using UnityEngine;

/// <summary>
/// 블랙홀: 플레이어(또는 플레이어 루트가 가진 콜라이더)와 충돌 시 즉시 게임오버 처리.
/// 수명/스폰 주기 등은 BlackHoleSpawner에서 관리한다.
/// </summary>
public class BlackHole : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private Collider2D collider2d;   // 트리거 콜라이더(프리팹에서 지정 권장)
    [SerializeField] private Rigidbody2D rb2d;        // 선택(필수 아님)

    [Header("흡인 설정")]
    [Tooltip("플레이어의 공전 중심을 블랙홀로 이동시키는 속도(초당 거리)")]
    [SerializeField] private float pullStrength = 8f;
    [Tooltip("흡인이 적용되는 최대 범위(월드 단위). 0이면 항상 적용")]
    [SerializeField] private float pullRange = 6f;

    // 캐시: 플레이어 컨트롤러
    private PlayerController _player;

    private void Awake()
    {
        // 컴포넌트 자동 할당(누락 시 보완)
        if (collider2d == null) collider2d = GetComponent<Collider2D>();
        if (rb2d == null) rb2d = GetComponent<Rigidbody2D>();

        if (collider2d == null)
        {
            // 최소한의 안전장치: 콜라이더가 없으면 원형 트리거를 추가
            collider2d = gameObject.AddComponent<CircleCollider2D>();
            ((CircleCollider2D)collider2d).isTrigger = true;
            Debug.LogWarning("[BlackHole] 프리팹에 Collider2D가 없어 CircleCollider2D(Trigger)를 자동 추가했습니다.");
        }
        else if (!collider2d.isTrigger)
        {
            collider2d.isTrigger = true; // 충돌은 트리거로 처리
        }
    }

    private void OnValidate()
    {
        // 인스펙터에서 음수값 방지
        if (pullStrength < 0f) pullStrength = 0f;
        if (pullRange < 0f) pullRange = 0f;
    }

    private void Update()
    {
        // 플레이어 참조 확보(없으면 재탐색)
        if (_player == null)
        {
            _player = Object.FindFirstObjectByType<PlayerController>();
        }

        if (_player == null) return;

        // 현재 공전 중심(지구 또는 태양)의 위치를 블랙홀로 이동
        Transform centerTr = _player.CurrentCenter;
        if (centerTr == null) return;

        Vector3 toCenter = transform.position - centerTr.position;
        float dist = toCenter.magnitude;
        if (pullRange <= 0f || dist <= pullRange)
        {
            float step = pullStrength * Time.deltaTime;
            centerTr.position = Vector3.MoveTowards(centerTr.position, transform.position, step);
        }

        // 공전 중심이 블랙홀 트리거 내부에 들어왔는지 검사하여 게임오버 처리
        if (collider2d != null && collider2d.OverlapPoint(centerTr.position))
        {
            var gm = GameManager.Instance != null ? GameManager.Instance : Object.FindFirstObjectByType<GameManager>();
            if (gm != null)
            {
                Debug.Log("[BlackHole] 공전 중심이 블랙홀에 진입 — 게임오버 처리");
                gm.EndGame();
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null) return;

        // 플레이어 판정: 충돌체 또는 루트의 태그를 확인(자식 콜라이더 대응)
        bool isPlayer = other.CompareTag(GameConstants.Tags.Player)
                       || (other.transform.root != null && other.transform.root.CompareTag(GameConstants.Tags.Player))
                       || (other.attachedRigidbody != null && other.attachedRigidbody.CompareTag(GameConstants.Tags.Player));

        if (!isPlayer) return;

        // 하위 호환: 플레이어 콜라이더 진입 시에도, 실제 공전 중심이 트리거 내부일 때만 게임오버
        Transform centerTr = _player != null ? _player.CurrentCenter : null;
        if (collider2d != null && centerTr != null && collider2d.OverlapPoint(centerTr.position))
        {
            var gm = GameManager.Instance != null ? GameManager.Instance : Object.FindFirstObjectByType<GameManager>();
            if (gm != null)
            {
                Debug.Log("[BlackHole] 공전 중심이 블랙홀에 진입 — 게임오버 처리");
                gm.EndGame();
            }
            else
            {
                Debug.LogWarning("[BlackHole] GameManager를 찾지 못해 게임오버를 수행할 수 없습니다.");
            }
        }
        else
        {
            Debug.Log("[BlackHole] 플레이어 콜라이더 진입 감지 — 공전 중심 미진입으로 무시");
        }
    }
}
