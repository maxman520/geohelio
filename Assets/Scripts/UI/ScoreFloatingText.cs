using System.Globalization;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using System.Threading;
/// <summary>
/// 점수 플로팅 텍스트: 활성화 시 지정된 텍스트를 위로 띄우며 알파를 페이드아웃한다.
/// 스스로 애니메이션만 담당하며, 반환(풀 Release)은 외부(ObjectSpawner)에서 수행한다.
/// </summary>
public class ScoreFloatingText : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private TMP_Text label;

    [Header("애니메이션 설정")]
    [Tooltip("표시 지속 시간(초)")]
    [SerializeField] private float duration = 0.7f;
    [Tooltip("상승 거리(월드 단위)")]
    [SerializeField] private float upDistance = 1.0f;
    [Tooltip("시작 스케일")]
    [SerializeField] private float startScale = 0.9f;
    [Tooltip("펀치 피크 스케일(초반부)")]
    [SerializeField] private float peakScale = 1.05f;

    // 실행 중 애니메이션 취소용(Disable/Destroy 연동)
    private CancellationTokenSource _runCts;

    private void Awake()
    {
        if (label == null)
        {
            label = GetComponentInChildren<TMP_Text>();
        }
    }

    public void SetAmount(int amount)
    {
        if (label == null) return;
        string formatted = amount.ToString("N0", CultureInfo.InvariantCulture);
        label.text = $"+{formatted}";
        var c = label.color; c.a = 1f; label.color = c;
    }

    private void OnDisable()
    {
        // 풀로 반환 시 즉시 애니메이션 중지
        _runCts?.Cancel();
    }

    private void OnDestroy()
    {
        _runCts?.Cancel();
        _runCts?.Dispose();
        _runCts = null;
    }

    public async UniTask PlayAsync(Vector3 startWorldPos)
    {
        float dur = Mathf.Max(0.1f, duration);
        float t = 0f;
        Vector3 startPos = startWorldPos;
        Vector3 endPos = startPos + new Vector3(0f, upDistance, 0f);

        // 단순 CTS 사용(Disable/Destroy에서 수동 취소/정리)
        _runCts?.Cancel();
        _runCts?.Dispose();
        _runCts = new CancellationTokenSource();
        var ct = _runCts.Token;

        try
        {
            if (this == null) return; // 파괴된 경우 방어
            transform.position = startPos;
            transform.localScale = Vector3.one * startScale;

            while (t < dur)
            {
                if (this == null) return; // 파괴된 경우 방어
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / dur);

                // 위치 보간(상승)
                transform.position = Vector3.Lerp(startPos, endPos, u);

                // 스케일 펀치(전반부만)
                float s = (u < 0.3f) ? Mathf.Lerp(startScale, peakScale, u / 0.3f) : 1f;
                transform.localScale = Vector3.one * s;

                // 알파 페이드
                if (label != null)
                {
                    var c = label.color;
                    c.a = 1f - u;
                    label.color = c;
                }

                // 파괴 시 예외 대신 정상 종료되도록 취소 예외를 흡수
                await UniTask.Yield(PlayerLoopTiming.Update, ct).SuppressCancellationThrow();
                if (ct.IsCancellationRequested) return;
            }
        }
        catch (System.OperationCanceledException)
        {
            // 플레이 모드 중지/오브젝트 파괴 시 자연 종료
        }
        finally
        {
            _runCts?.Dispose();
            _runCts = null;
        }
    }
}
