using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

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
    [SerializeField] private ObjectSpawner asteroidSpawner; // 소행성/장애물 스포너

    [Header("설정")]
    [Tooltip("게임 시작 시 자동으로 Ready 상태로 전환할지 여부")]
    [SerializeField] private bool autoReadyOnStart = true;
    [Tooltip("Ready 상태에서 첫 중심 전환 이벤트로 게임을 시작할지 여부")]
    [SerializeField] private bool startOnFirstCenterToggle = true;
    [Tooltip("초기 생명 수")]
    [SerializeField] private int initialLives = 1;
    [Header("게임오버 조건")]
    [Tooltip("플레이어의 현재 회전 중심이 스폰 영역(원점 중심 원)을 벗어나면 게임오버 처리")]
    [SerializeField] private bool gameOverWhenCenterOut = true;

    // 진행 상태/통계
    private GameState _state = GameState.Init;
    private int _score;
    private int _lives;
    private float _elapsedTime;
    private int _comboCount; // 동일 중심에서 연속 파괴 콤보 수(0부터 시작)

    // 이벤트 훅(UI/스포너/외부에서 구독)
    public event Action<GameState> OnStateChanged;   // 상태 변경 알림
    public event Action<int> OnScoreChanged;         // 점수 변경 알림
    public event Action<int> OnLivesChanged;         // 생명 변경 알림
    public event Action OnGameStarted;               // Playing 진입
    public event Action OnGameOver;                  // GameOver 진입

    // 읽기 전용 프로퍼티
    public GameState State => _state;
    public int Score => _score;
    public int Lives => _lives;
    public float ElapsedTime => _elapsedTime;

    protected override void Awake()
    {
        // 싱글턴 기본 초기화
        base.Awake();

        // 플레이어 참조 자동 바인딩(없을 경우에만)
        if (player == null)
        {
            player = FindFirstObjectByType<PlayerController>();
            if (player == null)
            {
                Debug.LogWarning("[GameManager] PlayerController 참조가 없습니다. 씬에 배치했는지 또는 스크립트에서 연결했는지 확인해 주세요.");
            }
        }

        // Player 중심 전환 이벤트 구독(있을 경우)
        if (player != null)
        {
            player.OnCenterToggled += OnPlayerCenterToggled;
        }

        // 기본 값 초기화
        _lives = Mathf.Max(0, initialLives);
        _score = 0;
        _elapsedTime = 0f;
        _comboCount = 0;
        SetState(GameState.Init);
    }

    private void Start()
    {
        if (autoReadyOnStart)
        {
            ToReady();
        }
    }

    private void Update()
    {
        // 경과 시간은 Playing 상태에서만 누적
        if (_state == GameState.Playing)
        {
            _elapsedTime += Time.deltaTime;
            CheckCenterBoundsAndMaybeGameOver();
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

        if (player != null)
        {
            player.OnCenterToggled -= OnPlayerCenterToggled;
        }
    }

    // 플레이어 중심 전환 이벤트: Ready 상태이고 정책이 허용되면 게임 시작
    private void OnPlayerCenterToggled(bool isSun)
    {
        if (!startOnFirstCenterToggle) return;
        if (_state == GameState.Ready)
        {
            StartGame();
        }
        // 공전 중심이 바뀌면 콤보 초기화
        _comboCount = 0;
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

        // 스포너 초기화(초기 배치 생성 + 스폰 시작)
        if (asteroidSpawner == null)
            asteroidSpawner = FindFirstObjectByType<ObjectSpawner>();
        asteroidSpawner?.Initialize();
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

        // 스폰 중지
        if (asteroidSpawner == null)
            asteroidSpawner = FindFirstObjectByType<ObjectSpawner>();
        asteroidSpawner?.Stop();

        // 결과 표시(UI Manager 연동)
        UIManager.Instance?.ShowGameOver(_score);
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
        int multiplier = _comboCount + 1; // 최초 1배(10점)부터 시작
        int amount = 10 * multiplier;
        AddScore(amount);
        _comboCount++;
        return amount;
    }

    public void LoseLife(int amount = 1)
    {
        if (amount <= 0) return;
        _lives = Mathf.Max(0, _lives - amount);
        OnLivesChanged?.Invoke(_lives);
        if (_lives <= 0)
        {
            EndGame();
        }
    }

    public void GainLife(int amount = 1)
    {
        if (amount <= 0) return;
        _lives += amount;
        OnLivesChanged?.Invoke(_lives);
    }

    // Player 제어 유틸(거리 조정/현재 중심 조회)
    public void SetPlayerDistance(float newDistance)
    {
        if (player == null) return;
        player.Distance = Mathf.Max(0f, newDistance);
    }

    public Transform GetCurrentOrbitCenter()
    {
        return player != null ? player.CurrentCenter : null;
    }

    // 입력: 모바일 터치 Began 또는 PC 마우스 클릭
    private bool IsTap()
    {
        if (Input.touchCount > 0)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                if (Input.GetTouch(i).phase == TouchPhase.Began)
                    return true;
            }
        }
        return Input.GetMouseButtonDown(0);
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

    // 회전 중심이 스폰 영역을 벗어났는지 확인하여 필요 시 게임오버 처리
    private void CheckCenterBoundsAndMaybeGameOver()
    {
        if (!gameOverWhenCenterOut) return;
        var centerTr = GetCurrentOrbitCenter();
        if (centerTr == null) return;

        // 스폰 반경 가져오기(가능하면 스포너에서, 없으면 카메라로 계산)
        if (asteroidSpawner == null)
            asteroidSpawner = FindFirstObjectByType<ObjectSpawner>();

        float baseRadius = 0f;
        if (asteroidSpawner != null)
        {
            baseRadius = asteroidSpawner.GetSpawnRadius();
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
}
