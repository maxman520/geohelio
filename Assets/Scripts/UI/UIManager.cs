using UnityEngine;
using UnityEngine.Serialization;
using Cysharp.Threading.Tasks;

// UI 전역 매니저: 게임오버 등 UI 요소 노출/숨김 제어
public class UIManager : SingletonMonoBehaviour<UIManager>
{
    [Header("참조")]
    [FormerlySerializedAs("gameOverPanel")]
    [SerializeField] private GameOverPanel gameOverPanel; // 게임오버 패널 참조

    protected override void Awake()
    {
        base.Awake();
    }
    

    // 게임오버 패널 표시/숨김
    public void ShowGameOver(int finalScore)
    {
        if (gameOverPanel == null)
        {
            Debug.LogWarning("[UIManager] GameOverPanel 참조가 없어 표시할 수 없습니다. 씬 배치/참조를 확인해 주세요.");
            return;
        }
        gameOverPanel.Show(finalScore);
    }

    public void HideGameOver()
    {
        gameOverPanel?.Hide();
    }

    // 버튼 핸들러: GameManager 위임
    public void RequestRetry()
    {
        // 먼저 게임오버 패널을 즉시 숨겨, 리로드 대기 중에도 패널이 남아있지 않도록 처리
        HideGameOver();

        if (GameManager.Instance != null)
        {
            // 씬 리로드 대신 소프트 리스타트 경로로 진입
            GameManager.Instance.SoftRestartAsync().Forget();
        }
        else
        {
            Debug.LogWarning("[UIManager] GameManager 인스턴스를 찾지 못했습니다. 다시하기 요청을 처리할 수 없습니다.");
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
            Debug.LogWarning("[UIManager] GameManager 인스턴스를 찾지 못했습니다. 메인 이동 요청을 처리할 수 없습니다.");
        }
    }

}