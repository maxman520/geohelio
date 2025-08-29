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
    [FormerlySerializedAs("player")]
    [FormerlySerializedAs("_player")]
    [SerializeField] private PlayerController player; // 플레이어 컨트롤러 참조
    [FormerlySerializedAs("asteroidSpawner")]
    [FormerlySerializedAs("_asteroidSpawner")]
    [SerializeField] private ObjectSpawner asteroidSpawner; // 소행성/장애물 스포너

    [Header("설정")]
    [Tooltip("게임 시작 시 자동으로 Ready 상태로 전환할지 여부")]
    [FormerlySerializedAs("autoReadyOnStart")]
    [FormerlySerializedAs("_autoReadyOnStart")]
    [SerializeField] private bool autoReadyOnStart = true;
    [Tooltip("초기 생명 수")]
    [FormerlySerializedAs("initialLives")]
    [FormerlySerializedAs("_initialLives")]
    [SerializeField] private int initialLives = 1;

    // 진행 상태/통계
    private GameState _state = GameState.Init;
    private int _score;
    private int _lives;
    private float _elapsedTime;

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

        // 기본 값 초기화
        _lives = Mathf.Max(0, initialLives);
        _score = 0;
        _elapsedTime = 0f;
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
        }

        // Ready 상태에서 탭 입력 시 게임 시작
        if (_state == GameState.Ready && IsTap())
        {
            StartGame();
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
        SceneManager.LoadScene("MainScene");
    }

    public void LoadGameScene()
    {
        SceneManager.LoadScene("GameScene");
    }

    public void RestartGameScene()
    {
        var active = SceneManager.GetActiveScene();
        SceneManager.LoadScene(active.name);
    }
}

