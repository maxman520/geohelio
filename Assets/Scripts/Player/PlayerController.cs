using UnityEngine;

/// <summary>
/// 플레이어 컨트롤러: 초기 상태에서 태양이 지구 주위를 공전하도록 제어한다.
/// - Player 오브젝트 하위에 Earth, Sun 트랜스폼이 존재한다고 가정한다.
/// - 지구-태양 간 거리는 인스펙터에서 설정 가능하다.
/// </summary>
public class PlayerController : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private Transform earth;   // 지구 트랜스폼
    [SerializeField] private Transform sun;     // 태양 트랜스폼
    [SerializeField] private Transform beam;    // 지구-태양을 잇는 빔 트랜스폼 (스프라이트가 세로 방향)
    [SerializeField] private SpriteRenderer beamRenderer; // 빔 스프라이트 렌더러 (길이 계산용)

    [Header("설정")]
    [Tooltip("지구-태양 간 거리 (월드 단위)")]
    [SerializeField] private float distance = 3f; // 지구-태양 거리

    [Tooltip("초당 공전 각속도(도/초)")]
    [SerializeField] private float orbitSpeed = 90f; // 공전 속도 (도/초)

    [Tooltip("공전 축 (기본: Z축, 2D 기준)")]
    [SerializeField] private Vector3 orbitAxis = Vector3.forward; // 공전 축

    [Header("빔 설정")]
    [Tooltip("빔 두께(X 스케일). 스프라이트 폭과 곱해짐")]
    [SerializeField] private float beamThickness = 1f; // 빔 두께
    [Tooltip("지구/태양 간 거리에 빔 길이를 정확히 맞출지 여부")]
    [SerializeField] private bool matchBeamToDistance = true; // 빔 길이 자동 맞춤

    private enum OrbitCenter
    {
        Earth,
        Sun
    }

    // 현재 회전 중심 상태 (초기: 지구)
    private OrbitCenter center = OrbitCenter.Earth;

    // 빔 원본 길이(스프라이트 기준, 월드 단위)
    private float beamBaseLength = 1f;

    // 거리 외부에서 접근 필요 시를 위한 공개 프로퍼티 (대문자 시작 규칙 준수)
    public float Distance
    {
        get => distance;
        set => distance = Mathf.Max(0f, value);
    }

    private void Awake()
    {
        // 하위 오브젝트 자동 참조 (필요 시)
        if (earth == null)
        {
            var t = transform.Find("Earth");
            if (t != null) earth = t;
        }

        if (sun == null)
        {
            var t = transform.Find("Sun");
            if (t != null) sun = t;
        }

        if (beam == null)
        {
            var t = transform.Find("Beam");
            if (t != null) beam = t;
        }

        if (beamRenderer == null && beam != null)
        {
            beamRenderer = beam.GetComponent<SpriteRenderer>();
        }

        if (earth == null || sun == null)
        {
            Debug.LogWarning("[PlayerController] Earth 또는 Sun 참조가 설정되지 않았습니다. Player 하위에 'Earth', 'Sun' 트랜스폼을 배치하거나 인스펙터에서 지정하세요.");
        }

        // 빔 기본 길이 캐싱 (스프라이트 피벗이 중앙, 세로 기준)
        if (beamRenderer != null && beamRenderer.sprite != null)
        {
            // 스프라이트의 월드 단위 높이(스케일 1 기준)
            beamBaseLength = beamRenderer.sprite.bounds.size.y;
            if (beamBaseLength <= 0f) beamBaseLength = 1f;
        }
    }

    private void Start()
    {
        // 시작 시 거리를 보정하여 배치 (지구 중심 공전)
        if (earth != null && sun != null)
        {
            Vector3 c = earth.position;
            Vector3 dir = sun.position - c;
            if (dir.sqrMagnitude < 1e-6f) dir = transform.right;
            sun.position = c + dir.normalized * distance;
        }
    }

    private void Update()
    {
        // 탭 입력 처리: 마우스/터치
        if (IsTap())
        {
            ToggleCenter();
        }

        // 공전 처리: 현재 중심에 따라 반대편 오브젝트를 공전시킨다.
        if (earth == null || sun == null) return;

        float angle = orbitSpeed * Time.deltaTime;

        if (center == OrbitCenter.Earth)
        {
            Vector3 rel = sun.position - earth.position;
            rel = Quaternion.AngleAxis(angle, orbitAxis) * rel;
            sun.position = earth.position + rel.normalized * distance; // 거리 유지
        }
        else // OrbitCenter.Sun
        {
            Vector3 rel = earth.position - sun.position;
            rel = Quaternion.AngleAxis(angle, orbitAxis) * rel;
            earth.position = sun.position + rel.normalized * distance; // 거리 유지
        }

        // 빔 위치/회전/스케일 갱신
        UpdateBeam();
    }

    private void OnValidate()
    {
        // 인스펙터 값 변경 시 즉시 갱신 (에디터 전용 상황 포함)
        if (distance < 0f) distance = 0f;

        if (Application.isPlaying) return;

        // 빔 기본 길이 갱신
        if (beamRenderer != null && beamRenderer.sprite != null)
        {
            beamBaseLength = beamRenderer.sprite.bounds.size.y;
            if (beamBaseLength <= 0f) beamBaseLength = 1f;
        }

        if (earth != null && sun != null)
        {
            if (center == OrbitCenter.Earth)
            {
                Vector3 c = earth.position;
                Vector3 dir = sun.position - c;
                if (dir.sqrMagnitude < 1e-6f) dir = Vector3.right;
                sun.position = c + dir.normalized * distance;
            }
            else
            {
                Vector3 c = sun.position;
                Vector3 dir = earth.position - c;
                if (dir.sqrMagnitude < 1e-6f) dir = Vector3.right;
                earth.position = c + dir.normalized * distance;
            }

            // 에디터에서도 빔 미리보기 갱신
            UpdateBeam();
        }
    }

    private bool IsTap()
    {
        // 모바일 터치 시작 또는 에디터/PC 마우스 클릭을 탭으로 간주
        if (Input.touchCount > 0)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                if (Input.GetTouch(i).phase == TouchPhase.Began)
                    return true;
            }
        }
        return Input.GetMouseButtonDown(0);
    }

    private void ToggleCenter()
    {
        if (earth == null || sun == null) return;

        // 전환 시 현재 위치를 유지하고 다음 프레임부터 새 중심으로 공전
        center = (center == OrbitCenter.Earth) ? OrbitCenter.Sun : OrbitCenter.Earth;
        Debug.Log($"[PlayerController] 회전 중심 전환: {(center == OrbitCenter.Earth ? "지구" : "태양")} 기준");
    }

    /// <summary>
    /// 빔을 지구-태양 사이에 정확히 배치하고, 세로 스프라이트의 위/아래가 각각 태양/지구에 오도록 회전 및 스케일을 조정한다.
    /// </summary>
    private void UpdateBeam()
    {
        if (beam == null) return;

        // 거리 및 방향 계산
        Vector3 a = earth.position; // 지구 (아래쪽에 배치)
        Vector3 b = sun.position;   // 태양 (위쪽에 배치)
        Vector3 ab = b - a;
        float len = ab.magnitude;
        if (len <= 1e-6f)
        {
            // 같은 위치일 때는 숨김 처리 혹은 최소 길이 유지
            if (beamRenderer != null) beamRenderer.enabled = false;
            return;
        }

        if (beamRenderer != null && !beamRenderer.enabled) beamRenderer.enabled = true;

        // 중점 배치
        Vector3 mid = a + ab * 0.5f;
        beam.position = mid;

        // 스프라이트의 +Y가 태양 방향을 향하도록 회전
        Vector3 dir = ab / len;
        beam.rotation = Quaternion.FromToRotation(Vector3.up, dir);

        // 스케일 조정: 스프라이트 기본 높이를 len에 맞춘다.
        if (matchBeamToDistance)
        {
            float targetYScale = len / beamBaseLength;
            Vector3 ls = beam.localScale;
            ls.x = beamThickness;
            ls.y = targetYScale;
            beam.localScale = ls;
        }
        else
        {
            // 길이를 강제하지 않는 경우 두께만 유지
            Vector3 ls = beam.localScale;
            ls.x = beamThickness;
            beam.localScale = ls;
        }
    }

    // 디버그(공전 궤도 기즈모)
    [Header("디버그(공전 궤도)")]
    public bool DebugDrawOrbit = true;               // 공전 궤도 원 표시
    public Color OrbitGizmoColor = Color.yellow;     // 공전 궤도 색상
    [Range(12, 256)] public int OrbitGizmoSegments = 64; // 세그먼트 수

    // GC 최소화를 위한 단위 원 캐시
    private Vector3[] orbitUnitCirclePoints;
    private int orbitLastSegments;

    // 현재 회전 중심 Transform을 외부/디버그에서 확인 가능하도록 제공
    public Transform CurrentCenter => center == OrbitCenter.Earth ? earth : sun;
    public bool IsSunCenter => center == OrbitCenter.Sun;

    private void EnsureOrbitUnitCircleCache()
    {
        int seg = Mathf.Clamp(OrbitGizmoSegments, 12, 256);
        if (orbitUnitCirclePoints == null || orbitLastSegments != seg)
        {
            orbitUnitCirclePoints = new Vector3[seg + 1];
            float step = Mathf.PI * 2f / seg;
            for (int i = 0; i <= seg; i++)
            {
                float a = step * i;
                orbitUnitCirclePoints[i] = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f); // XY 평면 단위 원
            }
            orbitLastSegments = seg;
        }
    }

    private void OnDrawGizmos()
    {
        if (!DebugDrawOrbit) return;
        if (earth == null || sun == null) return;

        // 반지름: distance 사용 (사용자 요청)
        float r = Mathf.Max(0f, distance);
        if (r <= 0f) return;

        // 중심: 탭 전환 상태(center)에 따라 지구/태양 중 선택
        Transform cTr = (center == OrbitCenter.Earth) ? earth : sun;
        if (cTr == null) return;

        EnsureOrbitUnitCircleCache();

        // 공전 축에 맞춰 XY 단위 원을 회전시켜 궤도 평면 정합
        Vector3 axis = (orbitAxis.sqrMagnitude > 1e-6f) ? orbitAxis.normalized : Vector3.forward;
        Quaternion rot = Quaternion.FromToRotation(Vector3.forward, axis);

        Gizmos.color = OrbitGizmoColor;
        Vector3 centerPos = cTr.position;

        for (int i = 0; i < orbitLastSegments; i++)
        {
            Vector3 p0 = centerPos + rot * (orbitUnitCirclePoints[i] * r);
            Vector3 p1 = centerPos + rot * (orbitUnitCirclePoints[i + 1] * r);
            Gizmos.DrawLine(p0, p1);
        }
    }
}
