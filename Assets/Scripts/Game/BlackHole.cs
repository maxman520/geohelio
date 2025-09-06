using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 블랙홀: 스폰 시 서서히 나타나는(알파 페이드 인) 연출을 담당한다.
/// - 스폰 시 자식 포함 SpriteRenderer 알파를 0→1로 보간한다.
/// - 파괴/비활성화 시 페이드 작업을 정리하고 알파를 복구한다.
/// </summary>
public class BlackHole : MonoBehaviour
{
    [Header("스폰 이펙트")]
    [Tooltip("스폰 시 알파 0→1로 서서히 나타나기")]
    [SerializeField] private bool fadeInOnSpawn = true;
    [Tooltip("스폰 페이드 인 시간(초)")]
    [SerializeField] private float fadeInDuration = 0.25f;
    [Tooltip("알파 페이드 인을 적용할 스프라이트 렌더러들. 비워두면 자식에서 자동 수집")]
    [SerializeField] private SpriteRenderer[] spriteRenderers;

    [Header("흡인(플레이어)")]
    [Tooltip("플레이어를 블랙홀 중심으로 끌어당기는 속도(단위/초)")]
    [SerializeField] private float maxPullSpeed = 3.0f;
    [Tooltip("디스폰 상태 판정용 애니메이터(비워두면 자식에서 자동 수집)")]
    [SerializeField] private Animator animator;
    [Tooltip("디스폰 상태 태그 이름(애니메이터 상태 태그)")]
    [SerializeField] private string despawnStateTag = GameConstants.Anim.BlackHoleDespawnStateTag;

    [Header("SFX")]
    [Tooltip("흡인 중 반복 재생할 SFX 키")]
    [SerializeField] private string pullSfxKey = "BlackholePull";
    [Tooltip("디스폰 진입 시 1회 재생할 SFX 키")]
    [SerializeField] private string despawnSfxKey = "BlackholeDespawn";
    [Tooltip("흡인 SFX 반복 재생 간격(초)")]
    [SerializeField] private float pullSfxInterval = 1.0f;

    private CancellationTokenSource _fadeCts;
    private PlayerController _player;
    private bool _gameOverTriggered;
    private CancellationTokenSource _pullSfxCts;
    private bool _despawnSfxPlayed;

    private void Awake()
    {
        // 렌더러 목록 자동 수집(명시되지 않은 경우)
        if (spriteRenderers == null || spriteRenderers.Length == 0)
            spriteRenderers = GetComponentsInChildren<SpriteRenderer>(includeInactive: true);

        // 플레이어 참조 캐시(가능 시)
        _player = FindFirstObjectByType<PlayerController>();

        // 애니메이터 참조(없으면 자식에서 탐색)
        if (animator == null) animator = GetComponentInChildren<Animator>();
    }

    private void OnEnable()
    {
        // 기존 페이드 작업 정리 후 새 작업 준비
        _fadeCts?.Cancel();
        _fadeCts?.Dispose();
        _fadeCts = null;
        _gameOverTriggered = false;
        _despawnSfxPlayed = false;
        StopPullSfxLoop();

        if (!fadeInOnSpawn || spriteRenderers == null || spriteRenderers.Length == 0)
        {
            SetAlpha(1f);
            return;
        }

        _fadeCts = new CancellationTokenSource();
        SetAlpha(0f);
        FadeInAsync(_fadeCts.Token).Forget();
    }

    private void OnDisable()
    {
        // 페이드 작업 취소 및 알파 복구(잔상 방지)
        _fadeCts?.Cancel();
        _fadeCts?.Dispose();
        _fadeCts = null;
        SetAlpha(1f);
        StopPullSfxLoop();
    }

    private void Update()
    {
        // 게임 진행 중에만 흡인 처리
        var gm = GameManager.Instance;
        if (gm == null || gm.State != GameManager.GameState.Playing)
        {
            StopPullSfxLoop();
            return;
        }

        if (_player == null)
        {
            _player = FindFirstObjectByType<PlayerController>();
            if (_player == null) { StopPullSfxLoop(); return; }
        }

        // 디스폰 중에는 흡인을 중단한다.
        if (IsDespawning())
        {
            StopPullSfxLoop();
            return;
        }

        // 플레이어의 '회전 중심'을 기준으로 끌어당긴다(지구/태양 중 현재 중심)
        var centerTr = _player.CurrentCenter;
        if (centerTr == null) { StopPullSfxLoop(); return; }
        Vector3 p = centerTr.position;
        Vector3 c = transform.position;
        Vector3 v = c - p; v.z = 0f;
        float d = v.magnitude;
        if (d <= 1e-4f) { StopPullSfxLoop(); return; }

        // 반경 제한 없이 항상 플레이어를 블랙홀 쪽으로 이동(상한 속도 사용)
        float speed = Mathf.Max(0f, maxPullSpeed);
        float step = speed * Time.deltaTime;
        if (step <= 0f) { StopPullSfxLoop(); return; }

        Vector3 delta = v.normalized * Mathf.Min(step, d);
        centerTr.position = p + delta;
        EnsurePullSfxLoop();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 트리거: 블랙홀과 플레이어의 현재 공전 중심(지구/태양)이 접촉하면 게임오버
        if (_gameOverTriggered) return;

        var gm = GameManager.Instance;
        if (gm == null || gm.State != GameManager.GameState.Playing) return;

        if (_player == null)
        {
            _player = FindFirstObjectByType<PlayerController>();
            if (_player == null) return;
        }

        var centerTr = _player.CurrentCenter;
        if (centerTr == null || other == null) return;

        var t = other.transform;
        bool isCenter = (t == centerTr) || t.IsChildOf(centerTr);
        if (!isCenter) return;

        _gameOverTriggered = true;
        Debug.Log("[BlackHole] 공전 중심이 블랙홀과 접촉하여 게임 오버 처리");
        try
        {
            gm.EndGame();
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[BlackHole] 게임오버 처리 중 예외: {e.Message}");
        }
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
            var c = r.color; c.a = a; r.color = c;
        }
    }

    // 페이드 인 비동기 처리(UniTask)
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

    private void OnValidate()
    {
        if (fadeInDuration < 0f) fadeInDuration = 0f;
        if (maxPullSpeed < 0f) maxPullSpeed = 0f;
        if (pullSfxInterval < 0.05f) pullSfxInterval = 0.05f;
    }

    // 애니메이터가 디스폰 상태(또는 그 전이)인지 확인
    private bool IsDespawning()
    {
        if (animator == null) return false;
        string tag = string.IsNullOrEmpty(despawnStateTag) ? GameConstants.Anim.BlackHoleDespawnStateTag : despawnStateTag;
        for (int i = 0; i < animator.layerCount; i++)
        {
            if (animator.IsInTransition(i))
            {
                var next = animator.GetNextAnimatorStateInfo(i);
                if (next.IsTag(tag)) return true;
            }
            var st = animator.GetCurrentAnimatorStateInfo(i);
            if (st.IsTag(tag)) return true;
        }
        return false;
    }

    // 흡인 SFX 반복 재생 시작(이미 동작 중이면 무시)
    private void EnsurePullSfxLoop()
    {
        if (_pullSfxCts != null) return;
        _pullSfxCts = new CancellationTokenSource();
        PullSfxLoopAsync(_pullSfxCts.Token).Forget();
    }

    // 흡인 SFX 반복 재생 중단
    private void StopPullSfxLoop()
    {
        if (_pullSfxCts != null)
        {
            _pullSfxCts.Cancel();
            _pullSfxCts.Dispose();
            _pullSfxCts = null;
        }
    }

    // 흡인 중 SFX를 주기적으로 재생(PlayOneShot 기반)
    private async UniTaskVoid PullSfxLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var am = AudioManager.Instance;
                if (am != null && !string.IsNullOrEmpty(pullSfxKey))
                {
                    am.PlaySfx(pullSfxKey);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[BlackHole] 흡인 SFX 재생 중 예외: {e.Message}");
            }

            float wait = Mathf.Max(0.05f, pullSfxInterval);
            try
            {
                await UniTask.Delay(System.TimeSpan.FromSeconds(wait), cancellationToken: ct);
            }
            catch (System.OperationCanceledException)
            {
                break;
            }
        }
    }

    // 애니메이션 이벤트 호출용 메소드
    private void PlayDespawnSFX()
    {
        AudioManager.Instance.PlaySfx("BlackholeDespawn");
    }
}
