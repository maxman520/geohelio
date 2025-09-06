using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 게임오버 창 제어 스크립트: 점수 표시와 다시하기 버튼 처리(TextMeshPro 사용)
public class GameOverPanel : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private GameObject root;           // 패널 루트(비활성/활성)
    [SerializeField] private TMP_Text scoreText;        // 최종 점수 표시(TextMeshPro)
    [SerializeField] private TMP_Text highScoreText;    // 최고 점수 표시(TextMeshPro)
    [SerializeField] private Button retryButton;        // 다시하기 버튼

    private void Awake()
    {
        // 루트 미할당 시 자신을 루트로 사용하여 활성/비활성을 안정적으로 제어
        if (root == null) root = gameObject;
        if (retryButton != null)
        {
            retryButton.onClick.AddListener(OnClickRetry);
        }
    }

    // 점수와 함께 패널 표시
    public void Show(int finalScore)
    {
        if (root != null) root.SetActive(true);
        if (scoreText != null)
        {
            // 최종 점수는 천 단위 구분 기호가 붙도록 표시(예: 1,024)
            string formatted = finalScore.ToString("N0");
            scoreText.text = formatted;
        }

        // 최고 점수도 즉시 반영
        var dm = DataManager.Instance;
        if (dm != null)
        {
            SetHighScoreText(dm.BestScore);
        }
    }

    // 패널 숨김
    public void Hide()
    {
        if (root != null) root.SetActive(false);
    }

    // 다시하기 버튼 클릭 처리: UIManager에 재시작 요청
    private void OnClickRetry()
    {
        AudioManager.Instance.PlaySfx("OnClickBtn");
        var ui = UIManager.Instance;
        if (ui != null) ui.RequestRetry();
    }

    // 최고 점수 텍스트를 천 단위 구분 기호와 함께 갱신
    private void SetHighScoreText(int value)
    {
        if (highScoreText == null) return;
        highScoreText.text = value.ToString("N0");
    }
}
