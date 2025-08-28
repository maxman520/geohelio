using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 게임 전반 로직을 관리하는 GameManager.
/// - 상태 머신(Init/Ready/Playing/Paused/GameOver)
/// - 점수/타이머/생명 등 기본 게임 진행 흐름 제공
/// - 외부 시스템(스포너/UI 등)이 구독할 수 있는 이벤트 제공
/// - PlayerController 참조 및 거리 설정 유틸 제공
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
    [SerializeField] private PlayerController player; // 플레이어 컨트롤러 참조(자동 탐색 가능)
    [SerializeField] private ObjectSpawner asteroidSpawner; // 소행성/장애물 스포너

    [Header("설정")]
    [Tooltip("게임 시작 시 자동으로 Ready 상태로 전환할지 여부")]
    [SerializeField] private bool autoReadyOnStart = true;
    [Tooltip("초기 생명 수(확장 포인트)")]
    [SerializeField] private int initialLives = 1;

    // 상태/진행 데이터
    private GameState state = GameState.Init;
    private int score;
    private int lives;
    private float elapsedTime;

    // 이벤트: 외부(UI/스포너 등)에서 구독하여 반응
    public event Action<GameState> OnStateChanged;   // 상태 변화 알림
    public event Action<int> OnScoreChanged;         // 점수 변화 알림
    public event Action<int> OnLivesChanged;         // 생명 변화 알림
    public event Action OnGameStarted;               // Playing 진입 시점
    public event Action OnGameOver;                  // GameOver 진입 시점

    // 공개 프로퍼티(외부 조회용)
    public GameState State => state;
    public int Score => score;
    public int Lives => lives;
    public float ElapsedTime => elapsedTime;

    protected override void Awake()
    {
        // 싱글턴 베이스 초기화
        base.Awake();

        // 플레이어 참조 자동 획득(없을 경우에만)
        if (player == null)
        {
            player = FindFirstObjectByType<PlayerController>();
            if (player == null)
            {
                Debug.LogWarning("[GameManager] PlayerController 참조가 없습니다. 씬에 배치하거나 인스펙터에 연결하세요.");
            }
        }

        // 기본 값 초기화
        lives = Mathf.Max(0, initialLives);
        score = 0;
        elapsedTime = 0f;
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
        // 진행 시간은 Playing 상태에서만 카운트
        if (state == GameState.Playing)
        {
            elapsedTime += Time.deltaTime;
        }

        // Ready 상태에서 첫 탭 입력 시 게임 시작
        if (state == GameState.Ready && IsTap())
        {
            StartGame();
        }
    }

    // 상태 전환 유틸
    private void SetState(GameState next)
    {
        if (state == next) return;
        state = next;
        OnStateChanged?.Invoke(state);
        Debug.Log($"[GameManager] 상태 전환: {state}");

        if (state == GameState.Playing)
            OnGameStarted?.Invoke();
        else if (state == GameState.GameOver)
            OnGameOver?.Invoke();
    }

    // 외부 제어 API
    public void ToReady()
    {
        // 점수/타이머 초기화, 필요 시 플레이어/월드 리셋 훅
        elapsedTime = 0f;
        score = 0;
        OnScoreChanged?.Invoke(score);
        SetState(GameState.Ready);
    }

    public void StartGame()
    {
        if (state == GameState.Playing) return;
        if (state == GameState.GameOver) ToReady();
        elapsedTime = 0f;
        SetState(GameState.Playing);

        // 소행성 스폰 시작
        if (asteroidSpawner == null)
            asteroidSpawner = FindFirstObjectByType<ObjectSpawner>();
        asteroidSpawner?.Begin();

        // UI 초기화(게임오버 패널 숨김)
        UIManager.Instance?.HideGameOver();
    }

    public void PauseGame()
    {
        if (state != GameState.Playing) return;
        SetState(GameState.Paused);
        Time.timeScale = 0f;
    }

    public void ResumeGame()
    {
        if (state != GameState.Paused) return;
        Time.timeScale = 1f;
        SetState(GameState.Playing);
    }

    public void EndGame()
    {
        if (state == GameState.GameOver) return;
        SetState(GameState.GameOver);

        // 스폰 정지
        if (asteroidSpawner == null)
            asteroidSpawner = FindFirstObjectByType<ObjectSpawner>();
        asteroidSpawner?.Stop();

        // 결과 표시(UI Manager 경유)
        UIManager.Instance?.ShowGameOver(score);
    }

    // 점수/생명 관리
    public void AddScore(int amount)
    {
        if (amount == 0) return;
        score = Mathf.Max(0, score + amount);
        OnScoreChanged?.Invoke(score);
    }

    public void LoseLife(int amount = 1)
    {
        if (amount <= 0) return;
        lives = Mathf.Max(0, lives - amount);
        OnLivesChanged?.Invoke(lives);
        if (lives <= 0)
        {
            EndGame();
        }
    }

    public void GainLife(int amount = 1)
    {
        if (amount <= 0) return;
        lives += amount;
        OnLivesChanged?.Invoke(lives);
    }

    // 플레이어 관련 유틸(확장 포인트): 필요 시 게임 매니저에서 거리/중심 등을 조정할 수 있게 훅 제공
    public void SetPlayerDistance(float newDistance)
    {
        if (player == null) return;
        player.Distance = Mathf.Max(0f, newDistance);
    }

    public Transform GetCurrentOrbitCenter()
    {
        return player != null ? player.CurrentCenter : null;
    }

    // 입력: 모바일 탭/마우스 클릭(Began)
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

    // 씬 전환 유틸: MainScene/GameScene 로드 및 재시작
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
