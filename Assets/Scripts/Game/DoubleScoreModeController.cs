using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 더블 스코어 모드 컨트롤러
/// - 라운드당 1회, 지정 시간 동안 점수 보너스 증가, 거리 고정, 감쇠 중지, 오빗 속도 상향을 적용한다.
/// - UI(DoubleScorePanel)와 연동하여 게이지 표시/남은 시간 바를 갱신한다.
/// </summary>
public class DoubleScoreModeController : MonoBehaviour
{
    [Header("설정")]
    [SerializeField] private float durationSeconds = 10f;     // 지속 시간(초)
    [SerializeField] private float fixedDistance = 3f;        // 모드 중 고정 거리
    [SerializeField] private float overrideOrbitSpeed = 180f; // 모드 중 오빗 속도

    [Header("참조(자동 탐색 가능)")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private PlayerController player;
    [SerializeField] private DoubleScorePanel ui;

    // 상태
    private bool _active;
    private bool _usedThisRun;
    private float _timeLeft;
    private CancellationTokenSource _cts;

    public bool IsActive => _active;
    public bool UsedThisRun => _usedThisRun;
    public float TimeLeft => Mathf.Max(0f, _timeLeft);
    public float Duration => Mathf.Max(0.01f, durationSeconds);

    private void Awake()
    {
        if (gameManager == null) gameManager = GameManager.Instance != null ? GameManager.Instance : FindFirstObjectByType<GameManager>();
        if (player == null) player = FindFirstObjectByType<PlayerController>();
        if (ui == null) ui = FindFirstObjectByType<DoubleScorePanel>();

        // UI에 자신을 바인딩(있다면)
        if (ui != null) ui.BindController(this);
    }

    private void OnEnable()
    {
        var gm = gameManager != null ? gameManager : (GameManager.Instance != null ? GameManager.Instance : FindFirstObjectByType<GameManager>());
        if (gm != null)
        {
            gm.OnGameStarted += HandleGameStarted;
            gm.OnGameOver += HandleGameOver;
        }
    }

    private void OnDisable()
    {
        var gm = gameManager != null ? gameManager : (GameManager.Instance != null ? GameManager.Instance : FindFirstObjectByType<GameManager>());
        if (gm != null)
        {
            gm.OnGameStarted -= HandleGameStarted;
            gm.OnGameOver -= HandleGameOver;
        }
        CancelTimer();
    }

    private void HandleGameStarted()
    {
        // 라운드 시작: 1회 사용 제한 해제 및 UI 초기화
        _usedThisRun = false;
        if (ui != null)
        {
            ui.SetButtonInteractable(true);
            ui.SetGaugeVisible(false);
            ui.SetFill01(0f);
        }
    }

    private void HandleGameOver()
    {
        // 진행 중이면 즉시 종료 정리
        if (_active)
        {
            EndMode();
        }
        if (ui != null)
        {
            ui.SetButtonInteractable(false);
            ui.SetGaugeVisible(false);
        }
    }

    public bool CanActivate()
    {
        var gm = gameManager != null ? gameManager : GameManager.Instance;
        return gm != null && gm.State == GameManager.GameState.Playing && !_active && !_usedThisRun;
    }

    public void Activate()
    {
        if (!CanActivate()) return;
        ActivateAsync().Forget();
    }

    public async UniTask ActivateAsync()
    {
        if (!CanActivate()) return;

        _active = true;
        _usedThisRun = true;
        _timeLeft = Duration;

        // 게임/플레이어/점수 효과 적용
        if (gameManager != null)
        {
            gameManager.SetDoubleScoreActive(true);
            gameManager.SetRadiusDecaySuppressed(true);
            gameManager.LockPlayerDistance(fixedDistance);
        }
        if (player != null)
        {
            player.OverrideOrbitSpeed(overrideOrbitSpeed);
        }

        // UI 시작 상태
        if (ui != null)
        {
            ui.SetGaugeVisible(true);
            ui.SetButtonInteractable(false);
        }

        // 타이머 루프
        CancelTimer();
        _cts = new CancellationTokenSource();
        var ct = _cts.Token;

        try
        {
            while (_timeLeft > 0f)
            {
                if (ct.IsCancellationRequested) break;
                float dur = Duration;
                if (ui != null) ui.SetFill01(Mathf.Clamp01(_timeLeft / dur));
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
                _timeLeft -= Time.unscaledDeltaTime;
            }
        }
        catch (OperationCanceledException)
        {
            // 취소는 정상 종료로 간주
        }
        finally
        {
            EndMode();
        }
    }

    private void EndMode()
    {
        if (!_active)
        {
            // 중복 호출 방지
            CancelTimer();
            return;
        }

        CancelTimer();

        // 효과 원복
        if (player != null)
        {
            player.ClearOrbitSpeedOverride();
        }
        if (gameManager != null)
        {
            gameManager.UnlockPlayerDistance();
            gameManager.SetRadiusDecaySuppressed(false);
            gameManager.SetDoubleScoreActive(false);
        }

        // UI 정리
        if (ui != null)
        {
            ui.SetGaugeVisible(false);
        }

        _active = false;
    }

    private void CancelTimer()
    {
        if (_cts != null)
        {
            _cts.Cancel();
            _cts.Dispose();
            _cts = null;
        }
    }
}

