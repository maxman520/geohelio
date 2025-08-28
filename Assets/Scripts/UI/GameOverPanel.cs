using UnityEngine;
using UnityEngine.UI;

// 게임오버 창 제어 스크립트: 점수 표시/다시하기/메인으로 버튼 훅
public class GameOverPanel : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private GameObject root; // 패널 루트(비활성/활성)
    [SerializeField] private Text scoreText;  // 최종 점수 표시(UI Text)
    [SerializeField] private Button retryButton; // 다시하기 버튼
    [SerializeField] private Button toMainButton; // 메인으로 버튼(선택)

    private void Awake()
    {
        if (retryButton != null)
        {
            retryButton.onClick.AddListener(OnClickRetry);
        }
        if (toMainButton != null)
        {
            toMainButton.onClick.AddListener(OnClickToMain);
        }
        Hide();
    }

    public void Show(int finalScore)
    {
        if (root != null) root.SetActive(true);
        if (scoreText != null) scoreText.text = $"점수: {finalScore}";
    }

    public void Hide()
    {
        if (root != null) root.SetActive(false);
    }

    private void OnClickRetry()
    {
        var ui = UIManager.Instance;
        if (ui != null) ui.RequestRetry();
    }

    private void OnClickToMain()
    {
        var ui = UIManager.Instance;
        if (ui != null) ui.RequestToMain();
    }
}
