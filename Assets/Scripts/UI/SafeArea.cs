using UnityEngine;

/// <summary>
/// Safe Area 적용 컴포넌트: 디바이스의 안전 영역에 맞춰 RectTransform 앵커를 보정한다.
/// </summary>
public class SafeArea : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private RectTransform rectTransform; // SafeArea가 적용될 RectTransform(비우면 자동 할당)

    // 내부 캐시(프라이빗 필드는 _camelCase 규칙 적용)
    private Rect _safeArea;
    private Vector2 _minAnchor;
    private Vector2 _maxAnchor;

    // 외부에서 RectTransform을 읽기 전용으로 접근할 수 있도록 프로퍼티 제공
    public RectTransform Rect => rectTransform != null ? rectTransform : (rectTransform = GetComponent<RectTransform>());

    private void Awake()
    {
        // 컴포넌트 자동 할당 보완
        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
            if (rectTransform == null)
            {
                Debug.LogWarning("[SafeArea] RectTransform을 찾지 못했습니다. 동일 오브젝트에 RectTransform이 있는지 확인해 주세요.");
                return;
            }
        }

        ApplySafeArea();
    }

    private void ApplySafeArea()
    {
        // Safe Area를 받아서 min/max 앵커를 비율로 계산한 뒤 적용한다.
        _safeArea = Screen.safeArea;
        _minAnchor = _safeArea.position;
        _maxAnchor = _minAnchor + _safeArea.size;

        // 픽셀 → 0..1 비율 변환
        _minAnchor.x /= Screen.width;
        _minAnchor.y /= Screen.height;
        _maxAnchor.x /= Screen.width;
        _maxAnchor.y /= Screen.height;

        rectTransform.anchorMin = _minAnchor;
        rectTransform.anchorMax = _maxAnchor;
    }

    private void OnValidate()
    {
        // 에디터에서 참조 누락 시 자동 보완
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();
    }
}
