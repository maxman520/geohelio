using UnityEngine;

// UI 총괄 매니저: 게임오버 패널 등 UI 요소 제어를 담당
public class UIManager : SingletonMonoBehaviour<UIManager>
{
    [Header("참조")]
    [SerializeField] private GameOverPanel gameOverPanel; // 게임오버 패널 참조

    protected override void Awake()
    {
        base.Awake();
        // 자동 참조 획득
        if (gameOverPanel == null)
        {
            gameOverPanel = FindFirstObjectByType<GameOverPanel>();
        }
    }

    // 게임오버 패널 표시/숨김
    public void ShowGameOver(int finalScore)
    {
        if (gameOverPanel == null)
            gameOverPanel = FindFirstObjectByType<GameOverPanel>();
        gameOverPanel?.Show(finalScore);
    }

    public void HideGameOver()
    {
        if (gameOverPanel == null)
            gameOverPanel = FindFirstObjectByType<GameOverPanel>();
        gameOverPanel?.Hide();
    }

    // 버튼 요청 처리: GameManager 위임
    public void RequestRetry()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RestartGameScene();
        }
        else
        {
            Debug.LogWarning("[UIManager] GameManager 인스턴스를 찾을 수 없습니다. 재시작 요청을 처리할 수 없습니다.");
        }
    }

    public void RequestToMain()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.LoadMainScene();
        }
        else
        {
            Debug.LogWarning("[UIManager] GameManager 인스턴스를 찾을 수 없습니다. 메인 이동 요청을 처리할 수 없습니다.");
        }
    }
}

