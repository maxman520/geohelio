using System;
using UnityEngine;

/// <summary>
/// 플레이어 컨트롤러: 초기 상태에서 지구·태양 거리를 유지하며
/// 탭 입력으로 회전 중심을 지구/태양으로 전환해 공전시킨다.
/// 빔은 두 천체를 잇고 길이/두께를 설정에 따라 갱신한다.
/// </summary>
public class PlayerController : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private Transform earth;   // 지구 트랜스폼
    [SerializeField] private Transform sun;     // 태양 트랜스폼
    [SerializeField] private Transform beam;    // 지구-태양을 잇는 빔 트랜스폼(스프라이트 +Y가 길이 방향)
    [SerializeField] private SpriteRenderer beamRenderer; // 빔 스프라이트 렌더러(길이 계산용)

    [Header("설정")]
    [Tooltip("지구-태양 거리(보정 포함)")]
    [SerializeField] private float distance = 1.5f; // 지구-태양 거리(기본값 1.5)

    [Tooltip("공전 속도(도/초)")]
    [SerializeField] private float orbitSpeed = 90f; // 공전 속도(도/초)

    [Tooltip("공전 축(기본: Z축, 2D 평면)")]
    [SerializeField] private Vector3 orbitAxis = Vector3.forward; // 공전 축

    [Header("빔 설정")]
    [Tooltip("빔 두께(X 스케일). 스프라이트 길이와는 별개로 두께만 조정")]
    [SerializeField] private float beamThickness = 1f; // 빔 두께

    [Tooltip("지구-태양 거리 변화에 맞춰 빔 길이를 자동으로 맞춤")]
    [SerializeField] private bool matchBeamToDistance = true; // 빔 길이 자동 맞춤

    private enum OrbitCenter
    {
        Earth,
        Sun
    }

    // 현재 회전 중심 상태(초기값: 지구)
    private OrbitCenter _center = OrbitCenter.Earth;

    // 이벤트: 중심 전환 시 1회 발행(true=태양 기준, false=지구 기준)
    public event Action<bool> OnCenterToggled;

    // 빔 기본 길이(스프라이트 원본 길이, 보정용)
    private float _beamBaseLength = 1f;

    // 외부 제어용 거리 프로퍼티(보정 포함)
    public float Distance
    {
        get => distance;
        set => distance = Mathf.Max(0f, value);
    }

    private void Awake()
    {
        // 자식에서 기본 트랜스폼 자동 탐색(없을 경우에만)
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
            Debug.LogWarning("[PlayerController] Earth 또는 Sun 참조가 설정되지 않았습니다. Player 자식에 'Earth', 'Sun' 오브젝트를 배치했는지 또는 스크립트에서 지정했는지 확인해 주세요.");
        }

        // 빔 기본 길이 캐시(스프라이트의 +Y 방향 길이)
        if (beamRenderer != null && beamRenderer.sprite != null)
        {
            _beamBaseLength = beamRenderer.sprite.bounds.size.y;
            if (_beamBaseLength <= 0f) _beamBaseLength = 1f;
        }
    }

    private void Start()
    {
        // 시작 시 거리 유지하도록 초기 배치 보정(지구 중심 회전 기준)
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
        // 탭 입력 처리: 중심 전환 (게임오버 시 클릭 감지 중단)
        if (IsTap())
        {
            var gm = GameManager.Instance;
            if (gm != null && gm.State == GameManager.GameState.GameOver)
            {
                // 게임오버 상태에서는 입력을 무시
            }
            else
            {
                // 탭 → 중심 전환 후 이벤트 발행
                ToggleCenter();
                try
                {
                    OnCenterToggled?.Invoke(IsSunCenter);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[PlayerController] 중심 전환 이벤트 처리 중 예외: {e.Message}");
                }
            }
        }

        // 공전 처리: 현재 중심을 기준으로 반대편 천체 이동
        if (earth == null || sun == null) return;

        float angle = orbitSpeed * Time.deltaTime;

        if (_center == OrbitCenter.Earth)
        {
            Vector3 rel = sun.position - earth.position;
            rel = Quaternion.AngleAxis(angle, orbitAxis) * rel;
            sun.position = earth.position + rel.normalized * distance;
        }
        else // OrbitCenter.Sun
        {
            Vector3 rel = earth.position - sun.position;
            rel = Quaternion.AngleAxis(angle, orbitAxis) * rel;
            earth.position = sun.position + rel.normalized * distance;
        }

        UpdateBeam();
    }

    private void OnValidate()
    {
        // 인스펙터 보정(음수 거리 방지)
        if (distance < 0f) distance = 0f;

        if (Application.isPlaying) return;

        // 에디터에서 빔 길이 캐시 갱신
        if (beamRenderer != null && beamRenderer.sprite != null)
        {
            _beamBaseLength = beamRenderer.sprite.bounds.size.y;
            if (_beamBaseLength <= 0f) _beamBaseLength = 1f;
        }

        // 현재 중심 기준으로 반대편 천체를 거리만큼 배치
        if (earth != null && sun != null)
        {
            if (_center == OrbitCenter.Earth)
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

            UpdateBeam();
        }
    }

    private bool IsTap()
    {
        // 모바일 터치 Began 또는 PC 마우스 클릭으로 판정
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

        // 전환: 현재 기준을 반대로 바꿔 다음 프레임부터 해당 중심으로 공전
        _center = (_center == OrbitCenter.Earth) ? OrbitCenter.Sun : OrbitCenter.Earth;
        Debug.Log($"[PlayerController] 회전 중심 전환: {(_center == OrbitCenter.Earth ? "지구" : "태양")} 기준");
    }

    /// <summary>
    /// 빔을 지구-태양 사이에 맞추고, 스프라이트의 원본 길이를 기준으로
    /// 스케일을 조정해 거리 변화에 따라 자연스럽게 길이를 갱신한다.
    /// </summary>
    private void UpdateBeam()
    {
        if (beam == null || earth == null || sun == null) return;

        // 거리 및 중간 지점 계산
        Vector3 a = earth.position;
        Vector3 b = sun.position;
        Vector3 ab = b - a;
        float len = ab.magnitude;
        if (len <= 1e-6f)
        {
            if (beamRenderer != null) beamRenderer.enabled = false;
            return;
        }

        if (beamRenderer != null && !beamRenderer.enabled) beamRenderer.enabled = true;

        // 위치/회전 설정
        Vector3 mid = a + ab * 0.5f;
        beam.position = mid;

        Vector3 dir = ab / len;
        beam.rotation = Quaternion.FromToRotation(Vector3.up, dir);

        // 스케일 조정
        if (matchBeamToDistance)
        {
            float targetYScale = len / _beamBaseLength;
            Vector3 ls = beam.localScale;
            ls.x = beamThickness;
            ls.y = targetYScale;
            beam.localScale = ls;
        }
        else
        {
            Vector3 ls = beam.localScale;
            ls.x = beamThickness;
            beam.localScale = ls;
        }
    }

    // 디버그/궤도 그리기
    [Header("디버그/궤도 그리기")]
    public bool DebugDrawOrbit = true;               // 궤도 라인 표시
    public Color OrbitGizmoColor = Color.yellow;     // 궤도 라인 색상
    [Range(12, 256)] public int OrbitGizmoSegments = 64; // 라인 세그먼트 수

    // GC 절감을 위한 캐시
    private Vector3[] _orbitUnitCirclePoints;
    private int _orbitLastSegments;

    // 현재 회전 중심 Transform 및 상태 질의
    public Transform CurrentCenter => _center == OrbitCenter.Earth ? earth : sun;
    public bool IsSunCenter => _center == OrbitCenter.Sun;

    /// <summary>
    /// 공전 중심을 지구로 고정하고, 지구의 월드 좌표를 (0,0)으로 이동시킨다.
    /// 태양은 현재 지구→태양 방향(없으면 +X)을 기준으로 distance만큼 배치한다.
    /// 소프트 리스타트 시 초기 상태를 강제하기 위해 사용한다.
    /// </summary>
    public void ResetOrbitToEarthAtOrigin()
    {
        if (earth == null || sun == null) return;

        // 중심을 지구로 전환
        _center = OrbitCenter.Earth;

        // 지구를 원점으로 이동(Z는 유지)
        Vector3 earthPos = earth.position;
        float z = earthPos.z;
        earth.position = new Vector3(0f, 0f, z);

        // 방향 결정: 기존 지구→태양 방향 유지, 너무 짧으면 +X 사용
        Vector3 dir = sun.position - earth.position;
        if (dir.sqrMagnitude < 1e-6f)
        {
            dir = Vector3.right;
        }

        // 태양을 distance만큼 배치
        sun.position = earth.position + dir.normalized * Mathf.Max(0f, distance);

        // 빔 갱신
        UpdateBeam();

        Debug.Log("[PlayerController] 소프트 리스타트 초기화: 중심=지구, 지구를 (0,0)에 배치");
    }

    private void EnsureOrbitUnitCircleCache()
    {
        int seg = Mathf.Clamp(OrbitGizmoSegments, 12, 256);
        if (_orbitUnitCirclePoints == null || _orbitLastSegments != seg)
        {
            _orbitUnitCirclePoints = new Vector3[seg + 1];
            float step = Mathf.PI * 2f / seg;
            for (int i = 0; i <= seg; i++)
            {
                float a = step * i;
                _orbitUnitCirclePoints[i] = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f); // XY 평면 단위 원
            }
            _orbitLastSegments = seg;
        }
    }

    private void OnDrawGizmos()
    {
        if (!DebugDrawOrbit) return;
        if (earth == null || sun == null) return;

        float r = Mathf.Max(0f, distance);
        if (r <= 0f) return;

        Transform cTr = (_center == OrbitCenter.Earth) ? earth : sun;
        if (cTr == null) return;

        EnsureOrbitUnitCircleCache();

        // 공전 축에 맞춘 회전 생성
        Vector3 axis = (orbitAxis.sqrMagnitude > 1e-6f) ? orbitAxis.normalized : Vector3.forward;
        Quaternion rot = Quaternion.FromToRotation(Vector3.forward, axis);

        Gizmos.color = OrbitGizmoColor;
        Vector3 centerPos = cTr.position;

        for (int i = 0; i < _orbitLastSegments; i++)
        {
            Vector3 p0 = centerPos + rot * (_orbitUnitCirclePoints[i] * r);
            Vector3 p1 = centerPos + rot * (_orbitUnitCirclePoints[i + 1] * r);
            Gizmos.DrawLine(p0, p1);
        }
    }
}
