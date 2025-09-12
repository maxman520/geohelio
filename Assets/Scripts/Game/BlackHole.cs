using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 블랙홀(위험 오브젝트).
/// - 활성화 시 자식 SpriteRenderer의 알파를 0→1로 페이드 인한다.
/// - 플레이어의 현재 공전 중심을 자신의 위치 쪽으로 지속적으로 끌어당긴다(maxPullSpeed).
/// - 디스폰 애니메이터 상태(태그) 전이/체류 중에는 흡인 및 게임오버 판정을 중단한다.
/// - 블랙홀 콜라이더와 플레이어의 공전 중심이 접촉하면 게임 오버를 트리거한다.
/// - 비활성화 시 진행 중인 페이드 작업을 취소하고 시각 상태를 복구한다.
/// </summary>
public class BlackHole : MonoBehaviour
{
    [Header("스폰 이펙트")]
    [Tooltip("스폰 페이드 인 시간(초)")]
    [SerializeField] private float fadeInDuration = 0.25f;
    [Tooltip("알파 페이드 인을 적용할 스프라이트 렌더러들. 비워두면 자식에서 자동 수집")]
    [SerializeField] private SpriteRenderer[] spriteRenderers;

    [Header("흡인(플레이어)")]
    [Tooltip("플레이어를 블랙홀 중심으로 끌어당기는 속도(단위/초)")]
    [SerializeField] private float maxPullSpeed;
    [Tooltip("디스폰 상태 판정용 애니메이터(비워두면 자동 수집)")]
    [SerializeField] private Animator animator;
    private string _despawnStateTag = GameConstants.Anim.BlackHoleDespawnStateTag; // 디스폰 상태 태그 이름(애니메이터 상태 태그)

    // SFX 키
    private string _pullSfxKey = GameConstants.SFX.BlackholePull;
    private string _despawnSfxKey = GameConstants.SFX.BlackholeDespawn;

    private CancellationTokenSource _fadeCts;
    private PlayerController _player;
    private bool _gameOverTriggered;
    private AudioManager.SfxHandle _pullSfxHandle; // 루프 SFX 핸들
    private ObjectSpawner _spawner;

    private void Awake()
    {
        // 렌더러 목록 자동 수집(명시되지 않은 경우)
        if (spriteRenderers == null || spriteRenderers.Length == 0)
            spriteRenderers = GetComponentsInChildren<SpriteRenderer>(includeInactive: true);

        // 애니메이터 참조(없으면 자동 탐색)
        if (animator == null) animator = GetComponent<Animator>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
    }

    private void Start()
    {
        // 플레이어 참조 캐시(가능 시)
        _player = FindFirstObjectByType<PlayerController>();
        if (_spawner == null)
            _spawner = GetComponentInParent<ObjectSpawner>();

        // 스포너의 spawnRadius를 기준으로 흡인 최대 속도를 설정한다(절반 값으로 고정).
        // 스포너가 부모에 배치되므로 상위에서 찾는다. 실패 시 2f 고정.
        if (_spawner != null)
        {
            float r = _spawner.GetBlackholeSpawnRadius();
            maxPullSpeed = Mathf.Max(2f, 2f + (1f / 4f * (float) Math.Sqrt(r)));
            Debug.Log($"[BlackHole] spawnRadius 기반 흡인 속도 설정: maxPullSpeed={maxPullSpeed:F2} (r={r:F2})");
        }
        else
        {
            maxPullSpeed = 2f;
            Debug.Log($"[BlackHole] _spawner가 null입니다. spawnRadius 기반 흡인 속도 설정: maxPullSpeed={maxPullSpeed:F2}");
        }
    }

    private void OnEnable()
    {
        // 기존 페이드 작업 정리 후 새 작업 준비
        CancelFade();
        _gameOverTriggered = false;
        StopPullSfxLoop();

        if (spriteRenderers == null || spriteRenderers.Length == 0)
        {
            SetAlpha(1f);
            return;
        }

        _fadeCts = new CancellationTokenSource();
        SetAlpha(0f);
        FadeInAsync(_fadeCts.Token).Forget();
    }

    private void Update()
    {
        // 게임 진행 중이 아니면 흡인 SFX 중단
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

        // 디스폰 중에는 흡인 SFX 중단
        if (IsDespawning())
        {
            StopPullSfxLoop();
            return;
        }

        var centerTr = _player.CurrentCenter;
        if (centerTr == null) { StopPullSfxLoop(); return; }
        Vector3 p = centerTr.position; // 플레이어의 위치
        Vector3 b = transform.position; // 블랙홀의 위치
        Vector3 v = b - p; v.z = 0f;
        float d = v.magnitude;

        // 플레이어의 '회전 중심'을 거리에 상관없이 끌어당긴다(지구/태양 중 현재 중심)
        float speed = Mathf.Max(0f, maxPullSpeed);
        float step = speed * Time.deltaTime;
        if (step <= 0f) { StopPullSfxLoop(); return; }

        Vector3 delta = v.normalized * Mathf.Min(step, d);
        centerTr.position = p + delta;
        EnsurePullSfxLoop();
    }

    private void OnDisable()
    {
        // 페이드 작업 취소 및 알파 복구
        CancelFade();
        SetAlpha(1f);
        StopPullSfxLoop();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 트리거: 블랙홀과 플레이어의 현재 공전 중심(지구/태양)이 접촉하면 게임오버
        TryTriggerGameOver(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        // 트리거 유지 중에도 동일 판정 적용(Enter 된 뒤 중심 전환하면 게임오버가 되지 않는 버그 방지)
        TryTriggerGameOver(other);
    }

    // 공통 게임오버 판정 로직: Enter/Stay에서 모두 호출
    private void TryTriggerGameOver(Collider2D other)
    {
        if (_gameOverTriggered) return;

        var gm = GameManager.Instance;
        if (gm == null || gm.State != GameManager.GameState.Playing) return;

        // 디스폰 중에는 게임오버 판정을 수행하지 않음
        if (IsDespawning()) return;

        var centerTr = _player?.CurrentCenter;
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

    // 블랙홀의 스프라이트 렌더러 알파 일괄 설정
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

    // 블랙홀 생성 시 연출. 페이드 인 비동기 처리(UniTask)
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
    }

    // 진행 중인 페이드 작업 취소/정리
    private void CancelFade()
    {
        if (_fadeCts == null) return;
        try { _fadeCts.Cancel(); } catch { }
        try { _fadeCts.Dispose(); } catch { }
        _fadeCts = null;
    }

    // 현재 애니메이터가 디스폰 상태(또는 그 전이)인지 확인
    private bool IsDespawning()
    {
        if (animator == null)
            return false;
        if (animator.IsInTransition(0))
        {
            var next = animator.GetNextAnimatorStateInfo(0);
            if (next.IsTag(_despawnStateTag))
                return true;
        }
        
        if (animator.GetCurrentAnimatorStateInfo(0).IsTag(_despawnStateTag))
            return true;

        return false;
    }

    // 채널/핸들 기반 흡인 SFX 재생 루프 시작(이미 동작 중이면 무시)
    private void EnsurePullSfxLoop()
    {
        if (_pullSfxHandle != null) return;
        try
        {
            if (!string.IsNullOrEmpty(_pullSfxKey) && AudioManager.Instance != null)
            {
                _pullSfxHandle = AudioManager.Instance.PlayLoopAttached(_pullSfxKey, transform);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[BlackHole] 흡인 SFX 루프 시작 중 예외: {e.Message}");
        }
    }

    // 흡인 SFX 반복 재생 중단
    private void StopPullSfxLoop()
    {
        try { _pullSfxHandle?.Stop(0.03f); } catch { }
        _pullSfxHandle = null;
    }

    // 애니메이션에서 호출. SFX 연출용 메소드
    private void PlayDespawnSFX()
    {
        AudioManager.Instance.PlaySfx(_despawnSfxKey);
    }
}
