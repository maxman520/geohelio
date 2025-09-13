using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Cysharp.Threading.Tasks;

/// <summary>
/// 게임 진행 로직을 총괄하는 GameManager.
/// - 상태 전환(Init/Ready/Playing/Paused/GameOver)
/// - 점수/시간/생명 관리 등 기본 게임 진행 요소 처리
/// - 스포너 및 UI 매니저 등과의 연동 처리
/// - PlayerController 참조와 거리 설정 유틸 제공
/// </summary>
public class GameManager : SingletonMonoBehaviour<GameManager>
{
    public enum GameState
    {
        Init,
        Ready,
        Playing,
        Paused,
        GameOver
    }

    [Header("참조")]
    [SerializeField] private PlayerController player; // 플레이어 컨트롤러 참조
    [SerializeField] private ObjectSpawner objectSpawner; // 오브젝트 스포너

    [Header("플레이어 설정")]
    [Tooltip("게임 시작 시 최소 반지름(처음 소행성 파괴 전)")]
    [SerializeField] private float initialMinPlayerRadius = 1.5f;
    [Tooltip("플레이어가 소행성을 한 번이라도 파괴한 후의 최소 반지름")]
    [SerializeField] private float postFirstDestroyMinPlayerRadius = 0.8f;
    [Tooltip("플레이어 반지름(거리) 최대값")]
    [SerializeField] private float maxPlayerRadius = 3.0f;
    [Tooltip("소행성 파괴 시 증가량")]
    [SerializeField] private float playerRadiusIncreaseOnAsteroid = 0.2f;
    [Tooltip("1초당 자연 감소량")]
    [SerializeField] private float playerRadiusDecayPerSecond = 0.05f;
    [Tooltip("피격 시 반지름 감소량(반지름이 최소 초과일 때만 적용)")]
    [SerializeField] private float playerRadiusHitPenalty = 0.2f;

    // 진행 상태/통계
    private GameState _state = GameState.Init;
    private int _score;
    private float _elapsedTime;
    private int _comboCount; // 동일 중심에서 연속 파괴 콤보 수(0부터 시작)
    private bool _hasDestroyedAsteroid; // 최초 소행성 파괴 여부
    private float _currentMinPlayerRadius; // 런타임 최소 반지름

    // 더블 스코어/거리 제어
    private bool _doubleScoreActive;         // 더블 스코어 모드 여부
    private bool _suppressRadiusDecay;       // 거리 자연 감소 일시 중지
    private bool _lockPlayerDistance;        // 거리 고정 여부
    private float _lockedDistanceValue = 3f; // 고정 거리 값

    // 이벤트 훅(UI/스포너/외부에서 구독)
    public event Action<GameState> OnStateChanged;   // 상태 변경 알림
    public event Action<int> OnScoreChanged;         // 점수 변경 알림
    public event Action OnGameStarted;               // Playing 진입
    public event Action OnGameOver;                  // GameOver 진입

    // 읽기 전용 프로퍼티
    public GameState State => _state;
    public int Score => _score;
    public float ElapsedTime => _elapsedTime;
    public float PlayerRadius => player != null ? player.Distance : 0f;

    #region 생명주기
    protected override void Awake()
    {
        // 싱글턴 기본 초기화
        base.Awake();

        // 기본 값 초기화
        _score = 0;
        _elapsedTime = 0f;
        _comboCount = 0;
        _hasDestroyedAsteroid = false;
        _currentMinPlayerRadius = Mathf.Max(0f, initialMinPlayerRadius);
        SetState(GameState.Init);
    }

    private void Start()
    {
        // Ready 초기화(내부에서 참조 보장 처리)
        ToReady();
    }

    private void Update()
    {
        // 경과 시간은 Playing 상태에서만 누적
        if (_state == GameState.Playing)
        {
            _elapsedTime += Time.deltaTime;
            CheckGameOverByBoundary();

            // 플레이어 반지름 자연 감소(초당)
            DecayPlayerRadius(Time.deltaTime);
        }
    }

    protected override void OnDestroy()
    {
        // 기본 싱글톤 정리 호출
        base.OnDestroy();

        if (player != null)
        {
            player.OnCenterToggled -= HandlePlayerCenterToggled;
        }
    }
    #endregion 생명주기

    // 플레이어 중심 전환 이벤트: Ready 상태이면 게임 시작
    private void HandlePlayerCenterToggled(bool isSun)
    {
        // 게임 시작 트리거는 옵션에 따르고, 콤보 초기화는 항상 수행한다.
        if (_state == GameState.Ready)
        {
            StartGame();
        }
        // 공전 중심이 바뀌면 콤보 초기화(게임 상태/옵션과 무관하게 적용)
        _comboCount = 0;
    }

    // 참조 보장 유틸: Player/ObjectSpawner 참조 및 이벤트 구독을 일원화한다.
    private void EnsureReferences()
    {
        // 플레이어 참조 및 이벤트 구독 보장
        if (player == null)
            player = FindFirstObjectByType<PlayerController>();
        if (player != null)
        {
            // 중복 구독 방지 위해 선제 해제 후 재구독
            try { player.OnCenterToggled -= HandlePlayerCenterToggled; } catch { }
            player.OnCenterToggled += HandlePlayerCenterToggled;
        }

        // 스포너 참조 보장
        if (objectSpawner == null)
            objectSpawner = FindFirstObjectByType<ObjectSpawner>();
    }

    // 상태 전환 공통 처리
    private void SetState(GameState next)
    {
        if (_state == next) return;

        _state = next;
        OnStateChanged?.Invoke(_state);
        Debug.Log($"[GameManager] 상태 전환: {_state}");

        if (_state == GameState.Playing)
            OnGameStarted?.Invoke();
        else if (_state == GameState.GameOver)
            OnGameOver?.Invoke();
    }

    /// <summary>
    /// Ready 상태로 전환하고 게임을 초기화한다.
    /// - 점수/시간/콤보를 리셋하고 상태를 Ready로 설정한다.
    /// - 플레이어/스포너 참조를 보장하고 플레이어 배치를 초기화한다.
    /// - 스포너를 Initialize하여 초기 배치를 생성한다.
    /// - (광고 사용 시) 대기 중 전면 광고를 표시 시도한다.
    /// </summary>
    public void ToReady()
    {
        // 점수/시간 초기화 후 Ready 진입
        _elapsedTime = 0f;
        _score = 0;
        _comboCount = 0;
        OnScoreChanged?.Invoke(_score);
        SetState(GameState.Ready);

        // 참조 보장(플레이어/스포너 + 이벤트 구독)
        EnsureReferences();

        // 시작 반지름(거리)를 최소값으로 설정한 뒤 배치 초기화
        if (player != null)
        {
            _hasDestroyedAsteroid = false;
            _currentMinPlayerRadius = Mathf.Max(0f, initialMinPlayerRadius);
            player.Distance = Mathf.Clamp(_currentMinPlayerRadius, 0f, maxPlayerRadius);
            player.ResetOrbitToEarthAtOrigin();
        }

        // 스포너 초기화(초기 배치 생성 + 스폰 시작)
        if (objectSpawner != null)
            objectSpawner.Initialize();

        // Ready 진입 시 대기 중인 전면 광고가 있으면 표시 시도
        if (AdsManager.Instance != null)
        {
            AdsManager.Instance.TryShowInterstitialAsync().Forget();
        }
    }

    /// <summary>
    /// 게임을 시작(Playing) 상태로 전환한다.
    /// - 경과 시간을 0으로 초기화하고 GameOver UI를 숨긴다.
    /// </summary>
    public void StartGame()
    {
        if (_state == GameState.Playing) return;
        if (_state == GameState.GameOver) ToReady();
        _elapsedTime = 0f;
        SetState(GameState.Playing);

        // UI 정리: 게임오버 패널 숨김
        UIManager.Instance?.HideGameOver();
    }

    /// <summary>
    /// 게임을 일시정지(Paused) 상태로 전환한다.
    /// - Time.timeScale을 0으로 설정한다.
    /// </summary>
    public void PauseGame()
    {
        if (_state != GameState.Playing) return;
        SetState(GameState.Paused);
        Time.timeScale = 0f;
    }

    /// <summary>
    /// 일시정지 상태에서 게임을 재개한다.
    /// - Time.timeScale을 1로 복구하고 상태를 Playing으로 변경한다.
    /// </summary>
    public void ResumeGame()
    {
        if (_state != GameState.Paused) return;
        Time.timeScale = 1f;
        SetState(GameState.Playing);
    }

    /// <summary>
    /// 게임오버 처리.
    /// - 상태를 GameOver로 전환하고 콤보를 초기화한다.
    /// - GameOver SFX 재생 및 최고 점수 저장을 시도한다.
    /// - 스포너를 중지하고 GameOver UI를 표시한다.
    /// </summary>
    public void EndGame()
    {
        if (_state == GameState.GameOver) return;
        SetState(GameState.GameOver);
        _comboCount = 0; // 게임 종료 시 콤보 초기화

        // SFX: 게임 오버 사운드 재생
        try
        {
            var am = AudioManager.Instance;
            if (am != null)
            {
                am.PlaySfx(GameConstants.SFX.GameOver);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[GameManager] 게임오버 SFX 재생 중 예외: {e.Message}");
        }

        // 데이터 저장: 최고 점수 갱신 및 즉시 저장 시도
        try
        {
            var dm = DataManager.Instance;
            if (dm != null)
            {
                dm.TrySetBestScore(_score); // 내부에서 저장 수행
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[GameManager] 게임오버 시 데이터 저장 처리 중 예외: {e.Message}");
        }

        // 스폰 중지
        if (objectSpawner == null)
            objectSpawner = FindFirstObjectByType<ObjectSpawner>();
        objectSpawner?.Stop();

        // 결과 표시(UI Manager 연동)
        if (UIManager.Instance != null)
            UIManager.Instance.ShowGameOver(_score);
        else
            Debug.Log("[GameManager] UI매니저가 Null");
    }

    /// <summary>
    /// 점수 추가 유틸
    /// </summary>
    public void AddScore(int amount)
    {
        if (amount == 0) return;
        _score = Mathf.Max(0, _score + amount);
        OnScoreChanged?.Invoke(_score);
    }

    /// <summary>
    /// 소행성 파괴 보상을 지급하고 콤보에 따라 가산한다.
    /// - 최초 파괴 시 최소 반지름을 더 낮은 값으로 전환한다.(전환 후 최초 파괴 시부터 반지름 감쇠가 시작되도록 함)
    /// - 플레이어 반지름을 증가시키고 임계 검사 후 반환한다.
    /// </summary>
    public int AwardAsteroidScore()
    {
        // 기본 증가폭은 10, 더블 스코어 모드 중에는 20
        int inc = _doubleScoreActive ? 20 : 10;
        int amount = 10 + inc * _comboCount; // 10, 30, 50, ... (inc=20)

        AddScore(amount);
        _comboCount++;

        // 최소 반지름 전환: 최초 파괴 시 더 낮은 최소값 적용
        if (!_hasDestroyedAsteroid)
        {
            _hasDestroyedAsteroid = true;
            _currentMinPlayerRadius = Mathf.Max(0f, postFirstDestroyMinPlayerRadius);
        }

        // 소행성 파괴 보상: 플레이어 반지름 증가
        SetPlayerDistance(player.Distance + playerRadiusIncreaseOnAsteroid);
        CheckGameOverByRadius();
        return amount;
    }

    /// <summary>
    /// 플레이어의 공전 반지름을 설정한다(최소/최대 범위로 클램프).
    /// - 거리 고정 모드일 경우 고정값을 유지한다.
    /// - 설정 후 즉시 게임오버 임계값을 확인한다.
    /// </summary>
    public void SetPlayerDistance(float newDistance)
    {
        if (player == null) return;
        
        // 거리 고정 상태에서는 고정값을 유지
        if (_lockPlayerDistance)
        {
            player.Distance = _lockedDistanceValue;
            return;
        }

        // 최소/최대 반지름 범위로 클램프(최소는 런타임 최소값)
        float minR = Mathf.Max(0f, _currentMinPlayerRadius);
        float maxR = Mathf.Max(minR, maxPlayerRadius);
        float clamped = Mathf.Clamp(newDistance, minR, maxR);
        player.Distance = clamped;

        // 설정 이후 임계 확인
        CheckGameOverByRadius();
    }

    /// <summary>
    /// 현재 플레이어의 공전 중심 Transform을 반환한다(지구/태양 중 현재 중심).
    /// 플레이어가 없으면 null을 반환한다.
    /// </summary>
    public Transform GetCurrentOrbitCenter()
    {
        return player != null ? player.CurrentCenter : null;
    }

    // 플레이어의 반지름 감쇠
    private void DecayPlayerRadius(float deltaTime)
    {
        if (player == null) return;
        if (_suppressRadiusDecay) return; // 감쇠 일시 중지
        if (playerRadiusDecayPerSecond <= 0f) return;

        float dec = Mathf.Max(0f, playerRadiusDecayPerSecond) * Mathf.Max(0f, deltaTime);
        if (dec <= 0f) return;

        // 최소값 아래로 내려가지 않도록 클램프
        float target = Mathf.Max(_currentMinPlayerRadius, player.Distance - dec);
        SetPlayerDistance(target);
        CheckGameOverByRadius();
    }

    /// <summary>
    /// 플레이어 피격 처리: 반지름이 최소값보다 크면 반지름만 감소, 그렇지 않으면 게임오버.
    /// 반환값: true면 게임오버 발생, false면 반지름만 감소.
    /// </summary>
    public bool TryHandlePlayerHit()
    {
        if (player == null) return false;
        float r = player.Distance;
        float min = Mathf.Max(0f, _currentMinPlayerRadius);

        if (r > min)
        {
            SetPlayerDistance(player.Distance - Mathf.Abs(playerRadiusHitPenalty));
            CheckGameOverByRadius();
            return false; // 반지름만 감소
        }
        else
        {
            EndGame();
            return true; // 게임오버 발생
        }
    }

    private void CheckGameOverByRadius()
    {
        if (player == null) return;
        if (_state != GameState.Playing) return;

        float min = Mathf.Max(0f, _currentMinPlayerRadius);

        if (player.Distance <= min)
        {
            EndGame();
        }
    }

    // 회전 중심이 게임 오버 경계를 벗어났는지 확인하여 필요 시 게임오버 처리
    private void CheckGameOverByBoundary()
    {
        var centerTr = GetCurrentOrbitCenter();
        if (centerTr == null) return;

        // 스폰 반경 가져오기(가능하면 스포너에서, 없으면 카메라로 계산)
        float baseRadius = 0f;
        if (objectSpawner != null)
        {
            baseRadius = objectSpawner.GetGameOverRadius();
        }
        else
        {
            var cam = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
            if (cam != null && cam.orthographic)
                baseRadius = cam.orthographicSize * cam.aspect;
            else
                baseRadius = 6f; // 폴백
        }

        // 게임오버 임계 반경: 스폰 반경(추가 마진 없음)
        float limit = baseRadius;
        Vector2 p = centerTr.position;
        if (p.sqrMagnitude > limit * limit)
        {
            Debug.Log("[GameManager] 회전 중심이 스폰 영역을 벗어났습니다. 게임오버 처리");
            EndGame();
        }
    }

    /// <summary>
    /// 소프트 리스타트: 씬 리로드 없이 Ready 상태로 되돌린다.
    /// - GameOver UI 숨김 → timeScale 복원 → ToReady() → 1프레임 대기
    /// </summary>
    public async UniTask RestartAsync()
    {
        // UI 정리 및 타임스케일 복원
        UIManager.Instance?.HideGameOver();
        if (Time.timeScale != 1f) Time.timeScale = 1f;

        // Ready로 초기화(점수/시간/콤보 리셋 + 스포너 Initialize)
        ToReady();

        // 한 프레임 양보하여 초기화 반영
        await UniTask.Yield(PlayerLoopTiming.Update);
    }

    #region 더블 스코어
    /// <summary>
    /// 더블 스코어 모드 활성/비활성 설정.
    /// </summary>
    public void SetDoubleScoreActive(bool value)
    {
        _doubleScoreActive = value;
    }

    public bool IsDoubleScoreActive => _doubleScoreActive;

    /// <summary>
    /// 플레이어 반지름 자연 감쇠를 일시 중지/해제한다.
    /// </summary>
    public void SetRadiusDecaySuppressed(bool value)
    {
        _suppressRadiusDecay = value;
    }

    /// <summary>
    /// 플레이어 반지름을 지정 값으로 고정한다.
    /// </summary>
    public void LockPlayerDistance(float distance)
    {
        _lockedDistanceValue = Mathf.Max(0f, distance);
        _lockPlayerDistance = true;
        if (player != null)
        {
            player.Distance = _lockedDistanceValue;
        }
    }

    /// <summary>
    /// 플레이어 반지름 고정 모드를 해제한다.
    /// </summary>
    public void UnlockPlayerDistance()
    {
        _lockPlayerDistance = false;
    }
    #endregion 더블 스코어
}
