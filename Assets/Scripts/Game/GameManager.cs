using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Cysharp.Threading.Tasks;
using UnityEngine.InputSystem; // 신 Input System 사용

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

    [Header("설정")]
    [Tooltip("게임 시작 시 자동으로 Ready 상태로 전환할지 여부")]
    [SerializeField] private bool autoReadyOnStart = true;
    [Tooltip("Ready 상태에서 첫 중심 전환 이벤트로 게임을 시작할지 여부")]
    [SerializeField] private bool startOnFirstCenterToggle = true;
    // 목숨 개념 제거됨 — 반지름 임계값 기반 게임오버 사용
    
    [Header("플레이어 반지름(거리)")]
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
    [Header("게임오버 조건")]
    [Tooltip("플레이어의 현재 회전 중심이 스폰 영역(원점 중심 원)을 벗어나면 게임오버 처리")]
    [SerializeField] private bool gameOverWhenCenterOut = true;

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

    protected override void Awake()
    {
        // 싱글턴 기본 초기화
        base.Awake();

        // 현재 씬의 레퍼런스 바인딩(플레이어/스포너)
        BindSceneReferences();

        // 씬 로드 시마다 레퍼런스 재바인딩을 위해 이벤트 구독
        SceneManager.sceneLoaded += OnSceneLoaded;

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
        if (autoReadyOnStart)
        {
            ToReady();
        }
    }

    private void OnEnable()
    {
        // 에디터에서 재컴파일/활성화 시 이벤트 재구독 누락 방지
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Update()
    {
        // 경과 시간은 Playing 상태에서만 누적
        if (_state == GameState.Playing)
        {
            _elapsedTime += Time.deltaTime;
            CheckCenterBoundsAndMaybeGameOver();

            // 플레이어 반지름 자연 감소(초당)
            DecayPlayerRadius(Time.deltaTime);
        }

        // Ready 상태에서 탭 입력 시 게임 시작(이벤트 시작 비활성 시에만)
        if (_state == GameState.Ready && !startOnFirstCenterToggle && IsTap())
        {
            StartGame();
        }
    }

    protected override void OnDestroy()
    {
        // 기본 싱글톤 정리 호출
        base.OnDestroy();

        // 씬 로드 이벤트 구독 해제
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (player != null)
        {
            player.OnCenterToggled -= OnPlayerCenterToggled;
        }
    }

    // 플레이어 중심 전환 이벤트: Ready 상태이고 정책이 허용되면 게임 시작
    private void OnPlayerCenterToggled(bool isSun)
    {
        // 게임 시작 트리거는 옵션에 따르고, 콤보 초기화는 항상 수행한다.
        if (_state == GameState.Ready && startOnFirstCenterToggle)
        {
            StartGame();
        }
        // 공전 중심이 바뀌면 콤보 초기화(게임 상태/옵션과 무관하게 적용)
        _comboCount = 0;
    }

    // 씬 로드 시 레퍼런스 재바인딩 및 초기화
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 새 씬의 오브젝트로 참조를 갱신하고, Ready 상태로 초기화
        BindSceneReferences();
        ToReady();
    }

    // 현재 씬의 Player/Spawner 참조 및 이벤트 재구독 처리
    private void BindSceneReferences()
    {
        // 기존 구독 해제
        if (player != null)
        {
            try { player.OnCenterToggled -= OnPlayerCenterToggled; } catch {}
        }

        // 새 참조 탐색
        player = FindFirstObjectByType<PlayerController>();
        if (player == null)
        {
            Debug.LogWarning("[GameManager] PlayerController 참조가 없습니다. 씬에 배치했는지 또는 스크립트에서 연결했는지 확인해 주세요.");
        }
        else
        {
            player.OnCenterToggled += OnPlayerCenterToggled;
        }

        if (objectSpawner == null)
        {
            objectSpawner = FindFirstObjectByType<ObjectSpawner>();
        }
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

    // 외부 제어 API
    public void ToReady()
    {
        // 점수/시간 초기화 후 Ready 진입
        _elapsedTime = 0f;
        _score = 0;
        _comboCount = 0;
        OnScoreChanged?.Invoke(_score);
        SetState(GameState.Ready);

        // 플레이어 초기 배치: 중심을 지구로, 지구를 (0,0)으로 이동
        if (player == null)
            player = FindFirstObjectByType<PlayerController>();
        // 시작 반지름(거리)를 최소값으로 설정한 뒤 배치 초기화
        if (player != null)
        {
            _hasDestroyedAsteroid = false;
            _currentMinPlayerRadius = Mathf.Max(0f, initialMinPlayerRadius);
            player.Distance = Mathf.Clamp(_currentMinPlayerRadius, 0f, maxPlayerRadius);
            player.ResetOrbitToEarthAtOrigin();
        }

        // 스포너 초기화(초기 배치 생성 + 스폰 시작)
        if (objectSpawner == null)
            objectSpawner = FindFirstObjectByType<ObjectSpawner>();
        objectSpawner?.Initialize();

        // Ready 진입 시 대기 중인 전면 광고가 있으면 표시 시도
        if (AdsManager.Instance != null)
        {
            AdsManager.Instance.TryShowInterstitialAsync().Forget();
        }
    }

    public void StartGame()
    {
        if (_state == GameState.Playing) return;
        if (_state == GameState.GameOver) ToReady();
        _elapsedTime = 0f;
        SetState(GameState.Playing);

        // UI 정리: 게임오버 패널 숨김
        UIManager.Instance?.HideGameOver();
    }

    public void PauseGame()
    {
        if (_state != GameState.Playing) return;
        SetState(GameState.Paused);
        Time.timeScale = 0f;
    }

    public void ResumeGame()
    {
        if (_state != GameState.Paused) return;
        Time.timeScale = 1f;
        SetState(GameState.Playing);
    }

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
                am.PlaySfx("GameOver");
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
            UIManager.Instance?.ShowGameOver(_score);
        else
            Debug.Log("[GameManager] UI매니저가 Null");
    }

    // 점수/생명 관리
    public void AddScore(int amount)
    {
        if (amount == 0) return;
        _score = Mathf.Max(0, _score + amount);
        OnScoreChanged?.Invoke(_score);
    }

    /// <summary>
    /// 소행성 파괴 보상: 현재 공전 중심 유지 중 연속 파괴에 따라 10, 20, 30... 가산.
    /// 중심이 변경되면 ToReady/OnCenterToggled에서 콤보가 0으로 리셋된다.
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
        AdjustPlayerRadius(playerRadiusIncreaseOnAsteroid);
        EnsureGameOverIfAtOrBelowMin();
        return amount;
    }
    // 목숨 로직 제거됨 — 반지름 임계 도달 시 게임오버 처리 사용

    // Player 제어 유틸(거리 조정/현재 중심 조회)
    public void SetPlayerDistance(float newDistance)
    {
        if (player == null) return;
        if (_lockPlayerDistance)
        {
            // 거리 고정 상태에서는 고정값을 유지
            player.Distance = _lockedDistanceValue;
            return;
        }
        // 최소/최대 반지름 범위로 클램프(최소는 런타임 최소값)
        float minR = Mathf.Max(0f, _currentMinPlayerRadius);
        float maxR = Mathf.Max(minR, maxPlayerRadius);
        float clamped = Mathf.Clamp(newDistance, minR, maxR);
        player.Distance = clamped;
        // 설정 이후 임계 확인
        EnsureGameOverIfAtOrBelowMin();
    }

    public Transform GetCurrentOrbitCenter()
    {
        return player != null ? player.CurrentCenter : null;
    }

    // 반지름 증가/감소 유틸
    private void AdjustPlayerRadius(float delta)
    {
        if (player == null) return;
        if (_lockPlayerDistance) return; // 고정 중에는 반경 변경 무시
        SetPlayerDistance(player.Distance + delta);
    }

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
        EnsureGameOverIfAtOrBelowMin();
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
            AdjustPlayerRadius(-Mathf.Abs(playerRadiusHitPenalty));
            EnsureGameOverIfAtOrBelowMin();
            return false; // 반지름만 감소
        }
        else
        {
            EndGame();
            return true; // 게임오버 발생
        }
    }

    private void EnsureGameOverIfAtOrBelowMin()
    {
        if (player == null) return;
        if (_state != GameState.Playing) return;
        float min = Mathf.Max(0f, _currentMinPlayerRadius);
        if (player.Distance < min)
        {
            EndGame();
        }
    }

    // 입력: 모바일 터치 Began 또는 PC 마우스 클릭
    private bool IsTap()
    {
        // 새 Input System: 터치 또는 마우스 클릭의 프레임 시작을 검출
        var touchscreen = Touchscreen.current;
        if (touchscreen != null)
        {
            foreach (var t in touchscreen.touches)
            {
                if (t.press.wasPressedThisFrame)
                    return true;
            }
        }

        var mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            return true;

        return false;
    }

    // 씬 전환 유틸
    public void LoadMainScene()
    {
        SceneManager.LoadScene(GameConstants.Scenes.Main);
    }

    public void LoadGameScene()
    {
        SceneManager.LoadScene(GameConstants.Scenes.Game);
    }

    public void RestartGameScene()
    {
        var active = SceneManager.GetActiveScene();
        SceneManager.LoadScene(active.name);
    }

    /// <summary>
    /// 소프트 리스타트: 씬 리로드 없이 런타임 상태만 초기화한다.
    /// - 게임오버 패널 숨김 → Ready 상태 초기화(ObjectSpawner.Initialize 포함) → 1프레임 대기 → (옵션) 즉시 시작
    /// - startOnFirstCenterToggle=true면 첫 중심 전환까지 Ready 유지, false면 즉시 StartGame 호출
    /// </summary>
    public async UniTask SoftRestartAsync()
    {
        // UI 정리 및 타임스케일 복원
        UIManager.Instance?.HideGameOver();
        if (Time.timeScale != 1f) Time.timeScale = 1f;

        // Ready로 초기화(점수/시간/콤보 리셋 + 스포너 Initialize)
        ToReady();

        // 한 프레임 양보하여 초기화 반영
        await UniTask.Yield(PlayerLoopTiming.Update);

        // 정책에 따라 즉시 시작 또는 첫 중심 전환 대기
        if (!startOnFirstCenterToggle)
        {
            StartGame();
        }
    }

    /// <summary>
    /// Ready 상태로만 되돌리는 리스타트(즉시 시작하지 않음).
    /// - 상단 좌측 "재시작" 버튼 등, "게임 시작 전" 상태로 복귀가 필요한 경우 사용.
    /// </summary>
    public async UniTask RestartToReadyAsync()
    {
        UIManager.Instance?.HideGameOver();
        if (Time.timeScale != 1f) Time.timeScale = 1f;
        ToReady();
        await UniTask.Yield(PlayerLoopTiming.Update);
    }

    // 회전 중심이 스폰 영역을 벗어났는지 확인하여 필요 시 게임오버 처리
    private void CheckCenterBoundsAndMaybeGameOver()
    {
        if (!gameOverWhenCenterOut) return;
        var centerTr = GetCurrentOrbitCenter();
        if (centerTr == null) return;

        // 스폰 반경 가져오기(가능하면 스포너에서, 없으면 카메라로 계산)
        if (objectSpawner == null)
            objectSpawner = FindFirstObjectByType<ObjectSpawner>();

        float baseRadius = 0f;
        if (objectSpawner != null)
        {
            baseRadius = objectSpawner.GetSpawnRadius();
        }
        else
        {
            var cam = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
            if (cam != null && cam.orthographic)
                baseRadius = cam.orthographicSize * cam.aspect;
            else
                baseRadius = 6f; // 폴백
        }

        // 게임오버 임계 반경: 스폰 반경 + 추가 허용 마진
        // 게임오버 임계 반경: 스폰 반경(추가 마진 없음)
        float limit = baseRadius;
        Vector2 p = centerTr.position;
        if (p.sqrMagnitude > limit * limit)
        {
            Debug.Log("[GameManager] 회전 중심이 스폰 영역을 벗어났습니다. 게임오버 처리");
            EndGame();
        }
    }

    // ===== 더블 스코어/거리 감쇠/거리 고정 제어 API =====
    public void SetDoubleScoreActive(bool value)
    {
        _doubleScoreActive = value;
    }

    public bool IsDoubleScoreActive => _doubleScoreActive;

    public void SetRadiusDecaySuppressed(bool value)
    {
        _suppressRadiusDecay = value;
    }

    public void LockPlayerDistance(float distance)
    {
        _lockedDistanceValue = Mathf.Max(0f, distance);
        _lockPlayerDistance = true;
        if (player != null)
        {
            player.Distance = _lockedDistanceValue;
        }
    }

    public void UnlockPlayerDistance()
    {
        _lockPlayerDistance = false;
    }
}
