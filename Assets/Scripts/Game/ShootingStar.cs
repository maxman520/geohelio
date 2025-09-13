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
    [SerializeField] private Collider2D col;

    [Header("예측 라인(점선)")]
    [Tooltip("예측 경로를 표시할 LineRenderer")]
    [SerializeField] private LineRenderer trajectory;
    [Header("이동 설정")]
    [Tooltip("발사 지연 시간(초): 경로 라인을 먼저 표시한 후 이 시간이 지난 뒤 이동 시작")]
    [SerializeField] private float launchDelaySeconds = 0.5f;

    // SFX 키
    private string _startSfxKey = GameConstants.SFX.ShootingStarStart;
    private string _burnSfxKey = GameConstants.SFX.ShootingStarBurn;

    private ObjectSpawner _spawner;
    private Vector3 _start, _end;     // 시작/도착 지점
    private Vector3 _control;         // 2차 베지어 제어점(통과 지점 조건으로 산출)
    private float _u;               // 0..1 진행도
    private float _speed;           // 현재 속도(단위/초)
    private CancellationTokenSource _moveCts;
    private Vector3[] _trajPoints;  // 라인 포인트(샘플)
    private float _trajTotalLength; // 전체 길이
    private bool _hasHitPlayer;     // 동일 개체로는 플레이어를 한 번만 피격 처리
    private AudioManager.SfxHandle _burnSfxHandle; // 루프 SFX 핸들

    // 라인 그라디언트 캐시(간소화): 색상 키는 인스펙터 값을 1회 복사해 보관하고,
    // 알파 키는 매 프레임 간단히 생성하여 적용한다.
    private Gradient _gradient;
    private GradientColorKey[] _colorKeys;
    private GradientAlphaKey[] _alphaKeys;

    public void Initialize(ObjectSpawner spawner)
    {
        _spawner = spawner;
    }

    private void Awake()
    {
        if (col == null) col = GetComponent<Collider2D>();
        if (trajectory == null) trajectory = GetComponentInChildren<LineRenderer>();

        // 초기값 저장해놓기
        if (_gradient == null) _gradient = trajectory.colorGradient;
        if (_colorKeys == null) _colorKeys = _gradient.colorKeys;
        if (_alphaKeys == null) _alphaKeys = _gradient.alphaKeys;
    }

    private void OnEnable()
    {
        _u = 0f;
        _moveCts?.Cancel();
        _moveCts?.Dispose();
        _moveCts = null;
        // 루프 SFX 핸들 정지
        try { _burnSfxHandle?.Stop(0.03f); } catch { }
        _burnSfxHandle = null;
        // 최초 활성화 시 중복 피격 상태를 리셋한다.
        _hasHitPlayer = false; // 한 슈팅스타당 1회만 피격 허용
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
        try { _burnSfxHandle?.Stop(0.03f); } catch { }
        _burnSfxHandle = null;
    }

    /// <summary>
    /// 경로와 속도를 지정하고 이동을 시작한다.
    /// - 포물선(2차 베지어) 경로: start/end를 지나고, passPoint를 u=0.5에서 통과하도록 제어점을 계산한다.
    /// </summary>
    public void Launch(Vector3 start, Vector3 end, Vector3 passPoint, float speed)
    {
        _start = start; _end = end; _speed = speed;

        // 제어점 산출: B(0.5) = passPoint 조건으로 2차 베지어 제어점 계산
        // B(0.5) = 0.25*P0 + 0.5*C + 0.25*P2 
        // --> C = 2*B(0.5) - 0.5*(P0+P2)
        _control = (2f * passPoint) - 0.5f * (_start + _end);

        // 시작 위치 및 초기 방향 설정
        _u = 0f;
        Vector3 p0 = PosOnLine(0f);
        Vector3 p1 = PosOnLine(0.001f);
        transform.position = p0;
        Vector3 v0 = p1 - p0;
        if (v0.sqrMagnitude > 1e-8f)
            transform.right = -v0.normalized;

        // 예측 라인 준비(샘플 기반 곡선 표시)
        BuildFullTrajectory();
        InitTrajectoryStyle();
        trajectory.enabled = true;

        // 이동 시작(UniTask) — 라인 표시 후 지연을 두고 발사
        _moveCts = new CancellationTokenSource();
        MoveAsync(_moveCts.Token).Forget();
    }

    // 슈팅스타 이동 비동기 메소드
    private async UniTaskVoid MoveAsync(CancellationToken ct)
    {
        // 시작 SFX 재생
        try
        {
            AudioManager.Instance.PlaySfx(_startSfxKey);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[ShootingStar] 시작 SFX 재생 중 예외: {e.Message}");
        }

        // 출발 전 잠시 딜레이
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

        // 이동 중 버닝 SFX를 채널/핸들 기반 루프 재생
        try
        {
            _burnSfxHandle = AudioManager.Instance.PlayLoopAttached(_burnSfxKey, transform);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[ShootingStar] 버닝 SFX 루프 시작 중 예외: {e.Message}");
        }

        while (_u < 1f)
        {
            if (ct.IsCancellationRequested) return;
            // 진행도 증분을 전체 경로 길이(_trajTotalLength)로 보정해
            // 속도 체감 편차를 줄인다(균일 이동에 가깝게).
            float du = (_trajTotalLength > 1e-4f) ? (
                _speed * Time.deltaTime / _trajTotalLength) : 1f;
            _u = Mathf.Clamp01(_u + du);

            // 위치/회전 업데이트 — 베지어 포물선
            Vector3 pos = PosOnLine(_u);
            float uNext = Mathf.Min(1f, _u + Mathf.Max(1e-4f, du));
            Vector3 posNext = PosOnLine(uNext);

            Vector3 vel = posNext - pos;
            transform.position = pos;
            if (vel.sqrMagnitude > 1e-8f)
            {
                // 슈팅스타 프리팹의 방향은 왼쪽이므로 right를 이동 방향의 반대로
                transform.right = -vel.normalized;
            }

            // 라인 알파를 업데이트하여 지나간 구간을 투명화(지워나가는 느낌)
            UpdateTrajectoryErase(_u);

            await UniTask.Yield(PlayerLoopTiming.Update, ct);
        }

        // 경로 이동 종료 시 반환/루프 SFX 정지
        try { _burnSfxHandle?.Stop(0.03f); } catch { }
        _burnSfxHandle = null;
        _spawner?.Despawn(transform);
    }

    // 전체 경로를 샘플링하여 라인 포인트를 1회 세팅한다.
    private void BuildFullTrajectory()
    {
        const int kSegments = 20; // 간단한 샘플 수
        if (_trajPoints == null || _trajPoints.Length != (kSegments + 1))
            _trajPoints = new Vector3[kSegments + 1];

        _trajTotalLength = 0f;
        Vector3 prev = Vector3.zero;
        for (int i = 0; i <= kSegments; i++)
        {
            float t = i / (float)kSegments; // 진행도
            Vector3 p = PosOnLine(t); // 진행도 기반 라인 상 위치
            _trajPoints[i] = p;
            if (i > 0) _trajTotalLength += Vector3.Distance(prev, p); // 곡선의 길이 계산
            prev = p;
        }
        trajectory.positionCount = _trajPoints.Length;
        trajectory.SetPositions(_trajPoints);
        _trajTotalLength = Mathf.Max(0.001f, _trajTotalLength);
    }

    // 라인 스타일(두께/색/타일링) 초기 세팅. 타일 수는 전체 길이를 기준으로 1회만 설정.
    private void InitTrajectoryStyle()
    {
        if (trajectory == null) return;

        // 재사용 시 초기값으로 되돌리기 위한 로직
        // 전체가 보이도록 알파 키를 0->1 모두 초기값으로 설정
        _gradient.SetKeys(_colorKeys, _alphaKeys);
        trajectory.colorGradient = _gradient;

    }

    // 진행도 u에 맞춰 [0..u] 구간 알파=0, [u..1] 구간 알파=1로 설정하여
    // 앞부분을 "지워나가는" 느낌을 만든다.
    private void UpdateTrajectoryErase(float u)
    {
        if (trajectory == null || trajectory.positionCount <= 0) return;
        float cut = Mathf.Clamp01(u);

        // 간단한 알파 키 4개 구성: [0..cut]=0, (cut..1]=1
        var newAlphaKeys = new[]
        {
            new GradientAlphaKey(0f, 0f),
            new GradientAlphaKey(0f, cut),
            new GradientAlphaKey(1f, cut+0.001f),
            new GradientAlphaKey(1f, 1f),
        };
        _gradient.SetKeys(_colorKeys, newAlphaKeys);
        trajectory.colorGradient = _gradient;
    }

    // u(0..1)에 대한 위치 반환
    private Vector3 PosOnLine(float u)
    {
        u = Mathf.Clamp01(u);
        float omt = 1f - u;
        return (omt * omt) * _start + 2f * omt * u * _control + (u * u) * _end;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null) return;
        // 플레이어과의 충돌 체크
        if (other.CompareTag(GameConstants.Tags.Player) || other.transform.root.CompareTag(GameConstants.Tags.Player))
        {
            // 동일 슈팅스타 개체로는 1회만 피격 처리한다.
            if (_hasHitPlayer) return;
            _hasHitPlayer = true;

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

            // 충돌 시 Hurt SFX 재생
            AudioManager.Instance.PlaySfx(GameConstants.SFX.Hurt);

            // 충돌 시 진동 재생(슈팅스타는 장애물로 간주하여 강한 진동 적용)
            try
            {
                var vm = VibrationManager.Instance;
                if (vm != null)
                {
                    vm.VibrateHeavy();
                }
            }
            catch (System.Exception ve)
            {
                Debug.LogWarning($"[ShootingStar] 진동(강하게) 재생 중 예외: {ve.Message}");
            }

            // 동일 개체로 재충돌하지 않도록 즉시 콜라이더를 비활성화한다.
            if (col != null) col.enabled = false; // "한 번만 아프게" 처리
        }
    }
}
