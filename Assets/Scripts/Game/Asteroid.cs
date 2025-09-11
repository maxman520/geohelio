using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using System.Threading;
using Random = UnityEngine.Random; // UnityEngine.Random 사용 고정

/// <summary>
/// 소행성 동작: 플레이어와 충돌 시 폭발 애니메이션 재생 후 스포너로 반환(풀링).
/// - 애니메이터에 "explode" 트리거가 있어야 하며, 폭발 상태에 태그 "Explode"가 지정되면 정확한 종료 대기 가능.
/// - 태그가 없으면 대기 시간이 부족할 수 있어 _fallbackExplodeDuration 를 사용.
/// </summary>
public class Asteroid : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private Animator animator;
    [SerializeField] private Collider2D col;
    [SerializeField] private Rigidbody2D rb2d;
    [Tooltip("알파 페이드 인을 적용할 스프라이트 렌더러들. 비워두면 자식에서 자동 수집")]
    [SerializeField] private SpriteRenderer[] spriteRenderers;

    [Header("설정")]
    [SerializeField] private string explodeTrigger = GameConstants.Anim.ExplodeTrigger;
    [SerializeField] private string explodeStateTag = GameConstants.Anim.ExplodeStateTag;
    [Tooltip("애니메이터 상태 태그가 없을 때 대체로 기다릴 폭발 시간(초)")]
    [SerializeField] private float fallbackExplodeDuration = 0.6f;

    [Header("스폰 이펙트")]
    [Tooltip("스폰 시 알파 0→1로 서서히 나타나기")]
    [SerializeField] private bool fadeInOnSpawn = true;
    [Tooltip("스폰 페이드 인 시간(초)")]
    [SerializeField] private float fadeInDuration = 0.25f;

    [Header("타입")]
    [Tooltip("장애물 소행성 여부(점수 미지급, 플레이어 Hurt 연출)")]
    [SerializeField] private bool isObstacle = false;

    [Header("부유감(드리프트)")]
    [Tooltip("스폰 후 무작위 방향으로 천천히 이동(부유감)")]
    [SerializeField] private bool driftOnSpawn = true;
    [Tooltip("드리프트 속도 범위(세계 좌표, 단위/초)")]
    [SerializeField] private Vector2 driftSpeedRange = new Vector2(0.1f, 0.5f);

    private ObjectSpawner _spawner;
    private bool _exploding;
    private CancellationTokenSource _fadeCts;
    private bool _driftActive;
    private Vector2 _driftVelocity;

    public void Initialize(ObjectSpawner spawner)
    {
        _spawner = spawner;
    }

    private void Awake()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (col == null) col = GetComponent<Collider2D>();
        if (rb2d == null) rb2d = GetComponent<Rigidbody2D>();
        if (spriteRenderers == null || spriteRenderers.Length == 0)
            spriteRenderers = GetComponentsInChildren<SpriteRenderer>(includeInactive: true);
    }

    private void OnEnable()
    {
        // 재사용 대비 초기화
        _exploding = false;
        if (col != null) col.enabled = true;
        if (animator != null)
        {
            animator.ResetTrigger(explodeTrigger);
            animator.Rebind();
            animator.Update(0f);
        }
    }

    public void ResetForSpawn()
    {
        _exploding = false;
        if (col != null) col.enabled = true;
        if (rb2d != null) rb2d.angularVelocity = 0f; // 초기화
        if (animator != null)
        {
            animator.ResetTrigger(explodeTrigger);
            animator.Rebind();
            animator.Update(0f);
        }

        // 2D 환경 보장: X/Y 회전 제거, Z만 유지
        var e = transform.eulerAngles;
        if (!Mathf.Approximately(e.x, 0f) || !Mathf.Approximately(e.y, 0f))
        {
            transform.eulerAngles = new Vector3(0f, 0f, e.z);
        }

        // 스폰 시 페이드 인 처리
        if (fadeInOnSpawn && spriteRenderers != null && spriteRenderers.Length > 0)
        {
            // 기존 페이드 작업 취소
            _fadeCts?.Cancel();
            _fadeCts?.Dispose();
            _fadeCts = new CancellationTokenSource();
            SetAlpha(0f);
            FadeInAsync(_fadeCts.Token).Forget();
        }

        // 스폰 후 드리프트 설정
        SetupDrift();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_exploding) return;
        if (other == null) return;

        // 태그 비교로 플레이어 판정 (콜라이더 자체 또는 루트 오브젝트)
        if (other.CompareTag(GameConstants.Tags.Player) || other.transform.root.CompareTag(GameConstants.Tags.Player))
        {
            // 장애물인 경우: 반지름/게임오버 처리 수행 + Hurt 애니메이션 재생
            if (isObstacle)
            {
                try
                {
                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.TryHandlePlayerHit();
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[Asteroid] 피격 처리 중 예외: {e.Message}");
                }

                // 충돌 시 항상 Hurt 애니메이션 재생 및 진동 재생
                var playerRoot = other.transform.root != null ? other.transform.root.gameObject : other.gameObject;
                var playerAnimator = playerRoot.GetComponent<Animator>();
                if (playerAnimator == null)
                    playerAnimator = playerRoot.GetComponentInChildren<Animator>();
                if (playerAnimator != null)
                {
                    playerAnimator.Play(GameConstants.Anim.PlayerHurtState, 0, 0f);
                }

                // 충돌 시 Hurt SFX 재생
                AudioManager.Instance.PlaySfx("Hurt");

                // 장애물 소행성: 강한 진동
                try
                {
                    var vm = VibrationManager.Instance;
                    if (vm != null)
                    {
                        vm.VibrateHeavy();
                    }
                }
                catch (Exception ve)
                {
                    Debug.LogWarning($"[Asteroid] 진동(강하게) 재생 중 예외: {ve.Message}");
                }
            }
            else
            {
                // 일반 소행성: 짧은 진동
                try
                {
                    var vm = VibrationManager.Instance;
                    if (vm != null)
                    {
                        vm.VibrateShort();
                    }
                }
                catch (Exception ve)
                {
                    Debug.LogWarning($"[Asteroid] 진동(짧게) 재생 중 예외: {ve.Message}");
                }
            }
            ExplodeAsync().Forget();
        }
    }

    private async UniTaskVoid ExplodeAsync()
    {
        _exploding = true;
        _driftActive = false; // 폭발 시 드리프트 정지
        if (col != null) col.enabled = false;

        // 폭발 SFX 재생
        try
        {
            var am = AudioManager.Instance;
            if (am != null)
            {
                am.PlaySfx("Explode");
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Asteroid] 폭발 SFX 재생 중 예외: {e.Message}");
        }

        // 애니메이션 트리거
        if (animator != null && !string.IsNullOrEmpty(explodeTrigger))
        {
            animator.ResetTrigger(explodeTrigger);
            animator.SetTrigger(explodeTrigger);
        }

        // 점수 지급/팝업: 장애물이 아닌 경우에만 수행
        if (!isObstacle)
        {
            try
            {
                int amount = 0;
                if (GameManager.Instance != null)
                {
                    amount = GameManager.Instance.AwardAsteroidScore();
                }
                if (amount > 0)
                {
                    _spawner?.ShowScorePopup(amount, transform.position);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Asteroid] 점수 추가/팝업 처리 중 예외: {e.Message}");
            }
        }

        // 상태 전이 프레임 반영
        await UniTask.Yield(PlayerLoopTiming.Update);

        float timeout = Mathf.Max(0.1f, fallbackExplodeDuration * 3f);
        float start = Time.time;

        if (animator != null)
        {
            // 태그가 있다면 해당 상태 종료까지 대기, 없으면 fallback
            bool hasExplodeTag = false;
            for (int i = 0; i < animator.layerCount; i++)
            {
                var info = animator.GetCurrentAnimatorStateInfo(i);
                if (info.IsTag(explodeStateTag)) { hasExplodeTag = true; break; }
            }

            if (hasExplodeTag)
            {
                while (Time.time - start < timeout)
                {
                    bool done = true;
                    for (int i = 0; i < animator.layerCount; i++)
                    {
                        var info = animator.GetCurrentAnimatorStateInfo(i);
                        if (info.IsTag(explodeStateTag) && info.normalizedTime < 1f)
                        {
                            done = false; break;
                        }
                    }
                    if (done) break;
                    await UniTask.Yield(PlayerLoopTiming.Update);
                }
            }
            else
            {
                await UniTask.Delay(TimeSpan.FromSeconds(fallbackExplodeDuration));
            }
        }
        else
        {
            await UniTask.Delay(TimeSpan.FromSeconds(fallbackExplodeDuration));
        }

        // 풀로 반환
        _spawner?.Despawn(transform);
    }

    private void OnDisable()
    {
        // 페이드 작업 취소 및 알파 복구(풀로 돌아갔을 때 잔상 방지)
        _fadeCts?.Cancel();
        _fadeCts?.Dispose();
        _fadeCts = null;
        SetAlpha(1f);
        _driftActive = false;
    }

    // 스프라이트 알파 일괄 설정
    private void SetAlpha(float a)
    {
        if (spriteRenderers == null) return;
        a = Mathf.Clamp01(a);
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            var r = spriteRenderers[i];
            if (r == null) continue;
            var c = r.color;
            c.a = a;
            r.color = c;
        }
    }

    // 스폰 시 페이드 인 비동기 처리(UniTask)
    private async UniTaskVoid FadeInAsync(CancellationToken ct)
    {
        float dur = Mathf.Max(0.01f, fadeInDuration);
        float t = 0f;
        while (t < dur)
        {
            if (ct.IsCancellationRequested) return;
            t += Time.deltaTime;
            SetAlpha(Mathf.Clamp01(t / dur));
            await UniTask.Yield(PlayerLoopTiming.Update, ct);
        }
        SetAlpha(1f);
    }

    // 드리프트 설정: 무작위 방향과 속도를 설정하고 활성화
    private void SetupDrift()
    {
        if (!driftOnSpawn)
        {
            _driftActive = false;
            _driftVelocity = Vector2.zero;
            return;
        }
        float min = Mathf.Max(0f, Mathf.Min(driftSpeedRange.x, driftSpeedRange.y));
        float max = Mathf.Max(min, Mathf.Max(driftSpeedRange.x, driftSpeedRange.y));
        float speed = Random.Range(min, max);
        // 무작위 단위 방향(2D)
        Vector2 dir = (Random.insideUnitCircle.sqrMagnitude < 1e-6f)
            ? Vector2.right
            : Random.insideUnitCircle.normalized;
        _driftVelocity = dir * speed;
        _driftActive = true;
    }

    private void Update()
    {
        // 게임이 진행 중(Playing)일 때만 이동 로직 수행
        var gm = GameManager.Instance;
        if (gm == null || gm.State != GameManager.GameState.Playing)
        {
            return;
        }

        if (_driftActive && !_exploding)
        {
            // 간단한 부유감: 선형 드리프트만 적용(2D 평면)
            Vector3 p = transform.position;
            p.x += _driftVelocity.x * Time.deltaTime;
            p.y += _driftVelocity.y * Time.deltaTime;
            transform.position = p;

            // 화면 경계 + 마진을 넘어가면 풀로 반환
            if (_spawner != null && _spawner.IsOutsideDespawnBounds(transform.position))
            {
                _spawner.Despawn(transform);
                return;
            }
        }
    }

    // 외부에서 장애물 플래그 설정(스포너에서 지정)
    public void SetAsObstacle(bool value)
    {
        isObstacle = value;
    }
}
