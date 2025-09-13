using UnityEngine;
using UnityEngine.Serialization;
using Cysharp.Threading.Tasks;

// UI 전역 매니저: 게임오버 등 UI 요소 노출/숨김 제어
public class UIManager : SingletonMonoBehaviour<UIManager>
{
    [Header("참조")]
    [SerializeField] private GameOverPanel gameOverPanel; // 게임오버 패널 참조
    

    /// <summary>
    /// 게임오버 패널 표시
    /// </summary>
    public void ShowGameOver(int finalScore)
    {
        if (gameOverPanel != null)
            gameOverPanel.Show(finalScore);
    }

    /// <summary>
    /// 게임오버 패널 숨김
    /// </summary>
    public void HideGameOver()
    {
        if (gameOverPanel != null)
            gameOverPanel.Hide();
    }

    // 버튼 핸들러: GameManager 위임
    public void RequestRetry()
    {
        // 먼저 게임오버 패널을 즉시 숨겨, 리로드 대기 중에도 패널이 남아있지 않도록 처리
        HideGameOver();

        // 전면 광고 집계 알림(3회마다 노출 정책) — 재시작 유사 클릭을 단일 지점에서 집계
        if (AdsManager.Instance != null)
        {
            AdsManager.Instance.NotifyRestartLikeClickAsync().Forget();
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.RestartAsync().Forget();
        }
        else
        {
            Debug.LogWarning("[UIManager] GameManager 인스턴스를 찾지 못했습니다. 다시하기 요청을 처리할 수 없습니다.");
        }
    }

}
