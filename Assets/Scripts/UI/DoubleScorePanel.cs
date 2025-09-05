using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 더블 스코어 패널(UI)
/// - 활성화 버튼과 게이지(루트/바)를 제어한다.
/// - 컨트롤러(DoubleScoreModeController)와 연동하여 버튼 동작 및 남은 시간 바를 갱신한다.
/// </summary>
public class DoubleScorePanel : MonoBehaviour
{
    [Header("참조 (자동 탐색 시도)")]
    [SerializeField] private Button activateButton;    // 더블 스코어 On 버튼
    [SerializeField] private GameObject gaugeRoot;     // DoubleScore_Gauge 루트
    [SerializeField] private Image barFill;            // DoubleScore_Bar(Image, Filled)

    [Header("자동 탐색 설정")]
    [SerializeField] private bool autoFindByName = true;
    [SerializeField] private string gaugeRootName = "DoubleScore_Gauge";
    [SerializeField] private string barObjectName = "DoubleScore_Bar";

    private DoubleScoreModeController _controller;

    public void BindController(DoubleScoreModeController controller)
    {
        _controller = controller;
    }

    private void Awake()
    {
        if (autoFindByName)
        {
            if (gaugeRoot == null)
            {
                var tr = transform.root != null ? transform.root : transform;
                var t = tr.Find(gaugeRootName);
                if (t != null) gaugeRoot = t.gameObject;
            }
            if (barFill == null)
            {
                var tr = transform.root != null ? transform.root : transform;
                var t = tr.Find(barObjectName);
                if (t != null) barFill = t.GetComponent<Image>();
            }
        }

        if (activateButton == null)
        {
            activateButton = GetComponentInChildren<Button>(includeInactive: true);
        }

        if (activateButton != null)
        {
            activateButton.onClick.AddListener(OnClickActivate);
        }

        // 시작 시 게이지는 꺼둔다(씬에서 켜둔 상태라도 런타임에는 On 시에만 표시)
        SetGaugeVisible(false);
        SetFill01(0f);
    }

    private void OnDestroy()
    {
        if (activateButton != null)
        {
            activateButton.onClick.RemoveListener(OnClickActivate);
        }
    }

    private void OnClickActivate()
    {
        if (_controller == null)
        {
            _controller = Object.FindFirstObjectByType<DoubleScoreModeController>();
        }
        if (_controller != null)
        {
            _controller.Activate();
        }
    }

    // 외부(컨트롤러)에서 게이지 표시/숨김
    public void SetGaugeVisible(bool visible)
    {
        if (gaugeRoot != null) gaugeRoot.SetActive(visible);
    }

    // 외부(컨트롤러)에서 0..1 채움 비율 업데이트
    public void SetFill01(float t)
    {
        if (barFill != null)
        {
            barFill.type = Image.Type.Filled;
            barFill.fillMethod = Image.FillMethod.Horizontal;
            barFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            barFill.fillAmount = Mathf.Clamp01(t);
        }
    }

    // 외부에서 버튼 상호작용 상태 제어
    public void SetButtonInteractable(bool interactable)
    {
        if (activateButton != null) activateButton.interactable = interactable;
    }
}

