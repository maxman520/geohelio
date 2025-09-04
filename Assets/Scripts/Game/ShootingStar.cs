using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

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

    [Header("예측 라인(점선)")]
    [Tooltip("예측 경로를 표시할 LineRenderer. 비워두면 자동 생성됨")]
    [SerializeField] private LineRenderer trajectory;
    [Tooltip("라인 색상")]
    [SerializeField] private Color trajectoryColor = Color.white;
    [Tooltip("라인 두께(월드 단위)")]
    [SerializeField] private float lineWidthWorld = 0.04f;
    [Tooltip("점선 간격(월드 단위). 초기 1회 타일 수를 설정하고 이후에는 고정")]
    [SerializeField] private float dashWorldSize = 0.25f;
    

    [Header("이동 설정")]
    [Tooltip("이동 속도 범위(단위/초)")]
    [SerializeField] private Vector2 speedRange = new Vector2(4f, 7f);
    [Tooltip("플레이어 Hurt 재생 후 재충돌까지 무시 시간(초)")]
    [SerializeField] private float hitCooldown = 0.5f;
    [Tooltip("발사 지연 시간(초): 경로 라인을 먼저 표시한 후 이 시간이 지난 뒤 이동 시작")]
    [SerializeField] private float launchDelaySeconds = 0.5f;

    private ObjectSpawner _spawner;
    private Vector3 _start, _end;     // 시작/도착 지점
    private Vector3 _control;         // 2차 베지어 제어점(통과 지점 조건으로 산출)
    private float _u;               // 0..1 진행도
    private float _pathLength;      // 경로 근사 길이
    private float _speed;           // 현재 속도(단위/초)
    private CancellationTokenSource _moveCts;
    private float _lastHitTime = -999f;
    private Vector3[] _trajPoints;  // 라인 포인트(샘플)
    private float _trajTotalLength; // 전체 길이

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
        if (trajectory != null)
        {
            trajectory.enabled = false;
            trajectory.positionCount = 0;
        }
    }

    private void OnDisable()
    {
        _moveCts?.Cancel();
        _moveCts?.Dispose();
        _moveCts = null;
    }

    /// <summary>
    /// 경로와 속도를 지정하고 이동을 시작한다.
    /// - 포물선(2차 베지어) 경로: start/end를 지나고, passPoint를 u=0.5에서 통과하도록 제어점을 계산한다.
    /// </summary>
    public void Launch(Vector3 start, Vector3 end, Vector3 passPoint, float speed)
    {
        _start = start; _end = end;
        _speed = (speed > 0f) ? speed : Mathf.Max(0.1f, Random.Range(speedRange.x, speedRange.y));
        _pathLength = Mathf.Max(0.001f, Vector3.Distance(_start, _end));

        // 제어점 산출: B(0.5) = passPoint 조건으로 2차 베지어 제어점 계산
        // B(0.5) = 0.25*P0 + 0.5*C + 0.25*P2 => C = 2*B(0.5) - 0.5*(P0+P2)
        _control = (2f * passPoint) - 0.5f * (_start + _end);

        // 시작 위치 및 초기 방향 설정(미세한 u증분으로 방향 근사)
        _u = 0f;
        Vector3 p0 = EvaluatePosition(0f);
        Vector3 p1 = EvaluatePosition(Mathf.Min(1f, 0.001f));
        transform.position = p0;
        Vector3 v0 = p1 - p0;
        if (v0.sqrMagnitude > 1e-8f)
            transform.right = -v0.normalized;

        // 예측 라인 준비(샘플 기반 곡선 표시)
        EnsureTrajectory();
        BuildFullTrajectory();
        ApplyTrajectoryStyle();
        trajectory.enabled = true;

        // 이동 시작(UniTask) — 라인 표시 후 지연을 두고 발사
        _moveCts = new CancellationTokenSource();
        DelayedMoveAsync(_moveCts.Token).Forget();
    }

    private async UniTaskVoid DelayedMoveAsync(CancellationToken ct)
    {
        float delay = Mathf.Max(0f, launchDelaySeconds);
        if (delay > 0f)
        {
            try
            {
                await UniTask.Delay(System.TimeSpan.FromSeconds(delay), cancellationToken: ct);
            }
            catch
            {
                return; // 취소 시 종료
            }
        }
        MoveAsync(ct).Forget();
    }

    private async UniTaskVoid MoveAsync(CancellationToken ct)
    {
        while (_u < 1f)
        {
            if (ct.IsCancellationRequested) return;
            float du = (_pathLength > 1e-4f) ? (
                _speed * Time.deltaTime / _pathLength) : 1f;
            _u = Mathf.Clamp01(_u + du);

            // 위치/회전 업데이트 — 베지어 포물선
            Vector3 pos = EvaluatePosition(_u);
            float uNext = Mathf.Min(1f, _u + Mathf.Max(1e-4f, du));
            Vector3 posNext = EvaluatePosition(uNext);
            Vector3 vel = posNext - pos;
            transform.position = pos;
            if (vel.sqrMagnitude > 1e-8f)
            {
                transform.right = -vel.normalized; // 이동 방향의 반대로 +X를 향하게
            }

            // 라인 알파를 업데이트하여 지나간 구간을 투명화(지워나가는 느낌)
            UpdateTrajectoryEraseByAlpha(_u);

            await UniTask.Yield(PlayerLoopTiming.Update, ct);
        }

        // 경로 종료 시 반환
        _spawner?.Despawn(transform);
    }

    /// <summary>
    /// Trajectory LineRenderer가 없으면 생성한다.
    /// </summary>
    private void EnsureTrajectory()
    {
        if (trajectory != null) return;
        var child = transform.Find("Trajectory");
        GameObject go;
        if (child == null)
        {
            go = new GameObject("Trajectory");
            go.transform.SetParent(transform, false);
        }
        else go = child.gameObject;

        trajectory = go.GetComponent<LineRenderer>();
        if (trajectory == null) trajectory = go.AddComponent<LineRenderer>();

        trajectory.useWorldSpace = true;
#if UNITY_2022_1_OR_NEWER
        trajectory.alignment = LineAlignment.View;
#endif
        trajectory.textureMode = LineTextureMode.Tile; // 대시 텍스처를 타일로 반복
        trajectory.loop = false;
        trajectory.shadowCastingMode = ShadowCastingMode.Off;
        trajectory.receiveShadows = false;
        // 소팅은 라인렌더러의 기본값 사용(필요 시 프리팹/인스펙터에서 직접 지정)
    }

    /// <summary>
    /// 전체 경로를 샘플링하여 라인 포인트를 1회 세팅한다.
    /// </summary>
    private void BuildFullTrajectory()
    {
        const int kSegments = 20; // 간단한 샘플 수(이해/성능 균형)
        if (_trajPoints == null || _trajPoints.Length != (kSegments + 1))
            _trajPoints = new Vector3[kSegments + 1];

        _trajTotalLength = 0f;
        Vector3 prev = Vector3.zero;
        for (int i = 0; i <= kSegments; i++)
        {
            float t = i / (float)kSegments;
            Vector3 p = EvaluatePosition(t);
            _trajPoints[i] = p;
            if (i > 0) _trajTotalLength += Vector3.Distance(prev, p);
            prev = p;
        }
        trajectory.positionCount = _trajPoints.Length;
        trajectory.SetPositions(_trajPoints);
        _trajTotalLength = Mathf.Max(0.001f, _trajTotalLength);
    }

    /// <summary>
    /// 라인 스타일(두께/색/타일링) 초기 세팅. 타일 수는 전체 길이를 기준으로 1회만 설정.
    /// 이후에는 변경하지 않아 패턴이 밀리지 않음.
    /// </summary>
    private void ApplyTrajectoryStyle()
    {
        if (trajectory == null) return;
        trajectory.widthMultiplier = Mathf.Max(0.001f, lineWidthWorld);

        // 색/알파 그라디언트 기본값(시작 시 전체 보이도록)
        var grad = new Gradient();
        grad.mode = GradientMode.Blend;
        grad.SetKeys(
            new[] { new GradientColorKey(trajectoryColor, 0f), new GradientColorKey(trajectoryColor, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
        );
        trajectory.colorGradient = grad;

        // 머티리얼이 있다면 타일링 1회 설정(패턴 고정). 머티리얼은 프리팹/인스펙터에서 지정.
        var mat = trajectory != null ? trajectory.material : null;
        if (mat != null && mat.mainTexture != null)
        {
            try
            {
                var tiling = mat.mainTextureScale;
                float dash = Mathf.Max(0.001f, dashWorldSize);
                tiling.x = _trajTotalLength / dash; // 전체 길이 기준 타일 수 고정
                // 세로 타일은 1 유지
                tiling.y = 1f;
                mat.mainTextureScale = tiling;
                // 오프셋은 0으로 고정 — 패턴이 밀리지 않도록
                var ofs = mat.mainTextureOffset; ofs.x = 0f; ofs.y = 0f; mat.mainTextureOffset = ofs;
            }
            catch { }
        }
    }

    /// <summary>
    /// 진행도 u에 맞춰 [0..u] 구간 알파=0, (u..1] 구간 알파=1로 설정하여
    /// 앞부분을 "지워나가는" 느낌을 만든다.
    /// </summary>
    private void UpdateTrajectoryEraseByAlpha(float u)
    {
        if (trajectory == null || trajectory.positionCount <= 0) return;
        float cut = Mathf.Clamp01(u);
        // 계단처럼 급격히 바꾸되, 키가 동일 시간일 때 일부 버전에서 정렬 문제가 있어 작은 에피실론 사용
        float eps = 0.0001f;
        float a0t = Mathf.Clamp01(cut - eps);
        float a1t = Mathf.Clamp01(cut);

        var grad = trajectory.colorGradient;
        var colorKeys = grad.colorKeys.Length > 0 ? grad.colorKeys : new[]
        {
            new GradientColorKey(trajectoryColor, 0f),
            new GradientColorKey(trajectoryColor, 1f)
        };
        var alphaKeys = new[]
        {
            new GradientAlphaKey(0f, 0f),
            new GradientAlphaKey(0f, a0t),
            new GradientAlphaKey(1f, a1t),
            new GradientAlphaKey(1f, 1f),
        };
        var newGrad = new Gradient { mode = GradientMode.Blend };
        newGrad.SetKeys(colorKeys, alphaKeys);
        trajectory.colorGradient = newGrad;
    }

    // u(0..1)에 대한 위치 평가: 2차 베지어 보간
    private Vector3 EvaluatePosition(float u)
    {
        u = Mathf.Clamp01(u);
        float omt = 1f - u;
        return (omt * omt) * _start + 2f * omt * u * _control + (u * u) * _end;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null) return;
        // 플레이어와 충돌 시 Hurt 애니메이션 재생(연타 방지)
        if (other.CompareTag(GameConstants.Tags.Player) || other.transform.root.CompareTag(GameConstants.Tags.Player))
        {
            if (Time.time - _lastHitTime < Mathf.Max(0f, hitCooldown)) return;
            _lastHitTime = Time.time;

            try
            {
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.TryHandlePlayerHit();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[ShootingStar] 피격 처리 중 예외: {e.Message}");
            }

            // 충돌 시 Hurt 애니메이션 재생
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
