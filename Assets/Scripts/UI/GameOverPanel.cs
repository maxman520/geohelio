using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 게임오버 창 제어 스크립트: 점수 표시와 다시하기 버튼만 처리(TextMeshPro 사용)
public class GameOverPanel : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private GameObject root;           // 패널 루트(비활성/활성)
    [SerializeField] private TMP_Text scoreText;        // 최종 점수 표시(TextMeshPro)
    [SerializeField] private Button retryButton;        // 다시하기 버튼

    private void Awake()
    {
        if (retryButton != null)
        {
            retryButton.onClick.AddListener(OnClickRetry);
        }
        Hide();
    }

    // 점수와 함께 패널 표시
    public void Show(int finalScore)
    {
        if (root != null) root.SetActive(true);
        if (scoreText != null) scoreText.text = $"점수: {finalScore}";
    }

    // 패널 숨김
    public void Hide()
    {
        if (root != null) root.SetActive(false);
    }

    // 다시하기 버튼 클릭 처리: UIManager에 재시작 요청
    private void OnClickRetry()
    {
        var ui = UIManager.Instance;
        if (ui != null) ui.RequestRetry();
    }
}

