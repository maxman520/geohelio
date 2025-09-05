using UnityEngine;

/// <summary>
/// 게임오버 경계(스폰 반경)를 LineRenderer로 런타임에 시각화한다.
/// - 원점(0,0)을 중심으로 ObjectSpawner.GetSpawnRadius() 또는 카메라 기반 반경을 사용해 원을 그림.
/// - 카메라 해상도/비율 변화에 따라 반지름이 달라지면 자동으로 재생성.
/// </summary>
[ExecuteAlways]
public class GameOverBoundaryVisualizer : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private ObjectSpawner spawner;       // 스폰 반경 제공자(없으면 자동 탐색)
    [SerializeField] private LineRenderer lineRenderer;   // 경계 라인을 그릴 LineRenderer

    [Header("라인 설정")]
    [SerializeField] private int segments = 128;          // 원 세그먼트 수(품질 조절)
    [SerializeField] private float lineWidth = 0.05f;     // 라인 두께
    [SerializeField] private bool useWorldSpace = true;   // 월드 좌표 사용 권장
    [SerializeField] private Color lineColor = new Color(1f, 0.25f, 0.25f, 0.85f); // 살짝 투명한 레드
    [SerializeField] private bool visibleOnlyDuringPlay = false; // 플레이 중에만 표시

    [Header("갱신 설정")]
    [SerializeField] private bool rebuildEveryFrame = false; // 매 프레임 강제 재생성 여부
    [SerializeField] private float radiusChangeThreshold = 0.001f; // 반지름 변화 감지 임계값

    // 캐시
    private float _lastRadius;
    private int _lastSegments;

    private void Reset()
    {
        TryAutoBind(true);
        EnsureLineRenderer();
        ApplyDefaults();
    }

    private void Awake()
    {
        TryAutoBind(false);
        EnsureLineRenderer();
        ApplyDefaults();
    }

    private void OnEnable()
    {
        ForceRebuild();
    }

    private void OnValidate()
    {
        // 에디터에서 파라미터 변경 시 즉시 갱신
        ForceRebuild();
    }

    private void Update()
    {
        // 표시 조건 처리
        if (lineRenderer != null)
            lineRenderer.enabled = !(visibleOnlyDuringPlay && !Application.isPlaying);

        if (lineRenderer == null || !lineRenderer.enabled) return;

        float r = GetBoundaryRadius();
        bool need = rebuildEveryFrame || Mathf.Abs(r - _lastRadius) > radiusChangeThreshold || _lastSegments != Mathf.Clamp(segments, 12, 2048);
        if (need)
        {
            RebuildCircle(Vector3.zero, r);
        }
    }

    /// <summary>
    /// 강제로 원형 경계를 재구성한다.
    /// </summary>
    public void ForceRebuild()
    {
        TryAutoBind(false);
        EnsureLineRenderer();
        ApplyDefaults();
        if (lineRenderer == null) return;
        lineRenderer.enabled = !(visibleOnlyDuringPlay && !Application.isPlaying);
        if (!lineRenderer.enabled) return;
        RebuildCircle(Vector3.zero, GetBoundaryRadius());
    }

    private void TryAutoBind(bool verbose)
    {
        if (spawner == null)
        {
            spawner = FindFirstObjectByType<ObjectSpawner>();
            if (verbose && spawner == null)
                Debug.LogWarning("[GameOverBoundaryVisualizer] ObjectSpawner를 찾지 못했습니다. 카메라 기반 반경을 사용합니다.");
        }
        if (lineRenderer == null)
        {
            lineRenderer = GetComponent<LineRenderer>();
        }
    }

    private void EnsureLineRenderer()
    {
        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
            Debug.Log("[GameOverBoundaryVisualizer] LineRenderer가 없어 자동 추가했습니다.");
        }
    }

    private void ApplyDefaults()
    {
        if (lineRenderer == null) return;
        lineRenderer.loop = true;
        lineRenderer.useWorldSpace = useWorldSpace;
        lineRenderer.widthMultiplier = lineWidth;
        lineRenderer.textureMode = LineTextureMode.Stretch;
        lineRenderer.startColor = lineColor;
        lineRenderer.endColor = lineColor;
    }

    private float GetBoundaryRadius()
    {
        // 런타임: 스포너의 런타임 반경(GetSpawnRadiusWorld) 경로와 일치시킴
        if (Application.isPlaying)
        {
            if (spawner != null)
                return Mathf.Max(0f, spawner.GetSpawnRadius());

            // 스포너가 없을 때는 카메라 기반 계산
            var camRun = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
            if (camRun != null && camRun.orthographic)
                return Mathf.Max(0f, camRun.orthographicSize * camRun.aspect);
            return 6f; // 폴백
        }

        // 에디터: Gizmo 로직과 같게 카메라 기반으로 우선 계산
        var cam = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
        if (cam != null && cam.orthographic)
            return Mathf.Max(0f, cam.orthographicSize * cam.aspect);

        // 메인 카메라가 원근이거나 없으면, 모든 카메라 중 직교 카메라 우선 선택
        try
        {
            var all = Camera.allCameras;
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null && all[i].orthographic)
                    return Mathf.Max(0f, all[i].orthographicSize * all[i].aspect);
            }
        }
        catch { /* 편집기 환경에 따라 접근 실패 가능 — 폴백 사용 */ }

        // 그래도 실패하면 스포너 값(설정값) 사용, 마지막 폴백은 상수
        if (spawner != null)
            return Mathf.Max(0f, spawner.GetSpawnRadius());
        return 6f;
    }

    private void RebuildCircle(Vector3 center, float radius)
    {
        if (lineRenderer == null) return;
        int seg = Mathf.Clamp(segments, 12, 2048);
        if (_lastSegments != seg)
        {
            lineRenderer.positionCount = seg;
            _lastSegments = seg;
        }

        float step = Mathf.PI * 2f / seg;
        for (int i = 0; i < seg; i++)
        {
            float a = step * i;
            Vector3 p = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * radius;
            lineRenderer.SetPosition(i, center + p);
        }
        _lastRadius = radius;
    }
}
