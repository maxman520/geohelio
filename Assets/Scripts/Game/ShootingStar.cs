using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 슈팅스타: 화면 밖에서 생성되어 플레이어 공전 중심 부근을 포물선으로 스쳐 지나간다.
/// - 플레이어와 충돌 시 Hurt 애니메이션을 1회 재생한다(연타 방지 쿨다운 포함).
/// - 이동 방향에 맞춰 불꽃 꼬리가 오른쪽(로컬 +X)으로 흩날리도록 회전을 보정한다.
/// - 화면 바깥(카메라 반경+마진)을 벗어나면 스포너로 반환된다.
/// </summary>
public class ShootingStar : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private Collider2D col;                     // 트리거 콜라이더
    [SerializeField] private Rigidbody2D rb2d;                   // 선택(필수 아님)
    [SerializeField] private Animator animator;                  // 선택(점화 애니메이션이 있을 수 있음)

    [Header("이동 설정")]
    [Tooltip("베지어 경로 이동 속도 범위(단위/초)")]
    [SerializeField] private Vector2 speedRange = new Vector2(4f, 7f);
    [Tooltip("플레이어 Hurt 재생 후 재충돌까지 무시 시간(초)")]
    [SerializeField] private float hitCooldown = 0.5f;

    private ObjectSpawner _spawner;
    private Vector3 _p0, _p1, _p2; // 베지어 포인트
    private float _u;               // 0..1 진행도
    private float _pathLength;      // 경로 근사 길이
    private float _speed;           // 현재 속도(단위/초)
    private const int ArcSamples = 24;
    private CancellationTokenSource _moveCts;
    private float _lastHitTime = -999f;
    private bool _enteredBounds;

    public void Initialize(ObjectSpawner spawner)
    {
        _spawner = spawner;
    }

    private void Awake()
    {
        if (col == null) col = GetComponent<Collider2D>();
        if (rb2d == null) rb2d = GetComponent<Rigidbody2D>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
    }

    private void OnEnable()
    {
        _u = 0f;
        _moveCts?.Cancel();
        _moveCts?.Dispose();
        _moveCts = null;
        if (col != null) col.enabled = true;
        _enteredBounds = false; // 경계 진입 여부 초기화
    }

    private void OnDisable()
    {
        _moveCts?.Cancel();
        _moveCts?.Dispose();
        _moveCts = null;
    }

    /// <summary>
    /// 경로와 속도를 지정하고 이동을 시작한다.
    /// 회전은 이동 속도 벡터에 맞춰, 로컬 +X가 꼬리(불꽃) 방향이 되도록 설정한다.
    /// </summary>
    public void Launch(Vector3 start, Vector3 control, Vector3 end, float speed)
    {
        _p0 = start; _p1 = control; _p2 = end;
        _speed = (speed > 0f) ? speed : Mathf.Max(0.1f, Random.Range(speedRange.x, speedRange.y));
        _pathLength = ApproximateLength(_p0, _p1, _p2);
        transform.position = _p0;
        // 시작 시 초기 방향 설정(미세 이동 벡터로 결정)
        Vector3 v0 = GetBezierVelocity(0.001f);
        if (v0.sqrMagnitude > 1e-6f)
        {
            // 불꼬리가 오른쪽을 향하므로, 이동 방향은 로컬 -X가 되어야 한다.
            transform.right = -v0.normalized; // 로컬 +X가 반대(꼬리) 방향
        }

        // 이동 시작(UniTask)
        _moveCts = new CancellationTokenSource();
        MoveAsync(_moveCts.Token).Forget();
    }

    private async UniTaskVoid MoveAsync(CancellationToken ct)
    {
        while (_u < 1f)
        {
            if (ct.IsCancellationRequested) return;
            float du = (_pathLength > 1e-4f) ? (_speed * Time.deltaTime / _pathLength) : 1f;
            _u = Mathf.Clamp01(_u + du);

            // 위치/회전 업데이트
            Vector3 pos = GetBezierPoint(_u);
            Vector3 vel = GetBezierVelocity(_u);
            transform.position = pos;
            if (vel.sqrMagnitude > 1e-8f)
            {
                transform.right = -vel.normalized; // 이동 방향의 반대로 +X를 향하게
            }

            // 디스폰 조건: 한 번이라도 경계 안으로 들어온 이후 다시 바깥으로 나갔을 때
            if (_spawner != null)
            {
                // 슈팅스타는 반경이 아닌 사각형(카메라 뷰) + margin 기준으로 이탈 판정
                bool outside = _spawner.IsOutsideCameraRectWithMargin(pos);
                if (!outside) _enteredBounds = true;
                if (_enteredBounds && outside)
                {
                    _spawner.Despawn(transform);
                    return;
                }
            }

            await UniTask.Yield(PlayerLoopTiming.Update, ct);
        }

        // 경로 종료 시 반환
        _spawner?.Despawn(transform);
    }

    private Vector3 GetBezierPoint(float t)
    {
        t = Mathf.Clamp01(t);
        float it = 1f - t;
        return it * it * _p0 + 2f * it * t * _p1 + t * t * _p2;
    }

    private Vector3 GetBezierVelocity(float t)
    {
        t = Mathf.Clamp01(t);
        // 2차 베지어 1차 도함수: 2*( (1-t)*(p1-p0) + t*(p2-p1) )
        Vector3 a = _p1 - _p0;
        Vector3 b = _p2 - _p1;
        return 2f * ((1f - t) * a + t * b);
    }

    private float ApproximateLength(Vector3 p0, Vector3 p1, Vector3 p2)
    {
        float len = 0f;
        Vector3 prev = p0;
        for (int i = 1; i <= ArcSamples; i++)
        {
            float u = i / (float)ArcSamples;
            Vector3 pt = GetBezierPoint(u);
            len += Vector3.Distance(prev, pt);
            prev = pt;
        }
        return Mathf.Max(0.001f, len);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null) return;
        // 플레이어와 충돌 시 Hurt 애니메이션 재생(연타 방지)
        if (other.CompareTag(GameConstants.Tags.Player) || other.transform.root.CompareTag(GameConstants.Tags.Player))
        {
            if (Time.time - _lastHitTime < Mathf.Max(0f, hitCooldown)) return;
            _lastHitTime = Time.time;

            var playerRoot = other.transform.root != null ? other.transform.root.gameObject : other.gameObject;
            var playerAnimator = playerRoot.GetComponent<Animator>();
            if (playerAnimator == null)
                playerAnimator = playerRoot.GetComponentInChildren<Animator>();
            if (playerAnimator != null)
            {
                playerAnimator.Play(GameConstants.Anim.PlayerHurtState, 0, 0f);
            }
        }
    }
}
