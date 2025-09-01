using System.Globalization;
using UnityEngine;
using TMPro;

/// <summary>
/// 점수 패널: GameManager의 점수 변경 이벤트를 구독하여 실시간으로 점수를 표시한다.
/// </summary>
public class ScorePanel : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private TMP_Text scoreText; // 점수 표시용 TextMeshPro
    private string format = "{0}"; // 점수 문자열 포맷(예: "{0}" 또는 "점수: {0}")

    private void OnEnable()
    {
        // 초기 표시 및 이벤트 구독
        var gm = GameManager.Instance;
        if (gm != null)
        {
            gm.OnScoreChanged += HandleScoreChanged;
            HandleScoreChanged(gm.Score);
        }
        else
        {
            Debug.LogWarning("[ScorePanel] GameManager 인스턴스를 찾지 못했습니다. 점수 표시가 갱신되지 않을 수 있습니다.");
        }
    }

    private void OnDisable()
    {
        var gm = GameManager.Instance;
        if (gm != null)
        {
            gm.OnScoreChanged -= HandleScoreChanged;
        }
    }

    // 점수 변경 이벤트 처리
    private void HandleScoreChanged(int score)
    {
        if (scoreText != null)
        {
            // 천 단위 구분 기호 적용(예: 1,024)
            string formatted = score.ToString("N0", CultureInfo.InvariantCulture);
            scoreText.text = string.Format(format, formatted);
        }
    }
}
