using UnityEngine;

/// <summary>
/// 태양에서 지구로 향하는 빔 파티클 정렬 담당 컴포넌트.
/// - 로컬 Forward(+Z)를 지구 방향으로 정렬해 방출 방향을 고정
/// - 시뮬레이션 공간은 Local로 고정
/// </summary>
public class SunBeamVfx : MonoBehaviour
{
    [Header("참조")]
    [Tooltip("지구 트랜스폼")]
    [SerializeField] private Transform earth;
    [Tooltip("파티클 시스템(미지정 시 자기 자신에서 탐색)")]
    [SerializeField] private ParticleSystem particle;

    [Header("정렬/시뮬레이션")]
    [Tooltip("로컬 Forward(+Z) 축을 지구 방향으로 정렬하고 Local 시뮬레이션 적용")]
    [SerializeField] private bool info = true; // 인스펙터 표기용(기능 플래그 아님)

    private ParticleSystem.MainModule _main;
    private bool _mainCached;
    

    private void Awake()
    {
        if (particle == null)
        {
            particle = GetComponent<ParticleSystem>();
        }

        if (particle == null)
        {
            Debug.LogWarning("[SunBeamVfx] ParticleSystem 참조가 없습니다. 컴포넌트를 연결해 주세요.");
            return;
        }

        _main = particle.main;
        _mainCached = true;
        // Local 시뮬레이션 공간 고정
        _main.simulationSpace = ParticleSystemSimulationSpace.Local;

        if (earth == null)
        {
            // 기본적으로 Player 자식에 "Earth"가 존재하므로, 동일 부모 내에서 탐색 시도
            var parent = transform.parent;
            if (parent != null)
            {
                var t = parent.Find("Earth");
                if (t != null) earth = t;
            }
        }
    }

    private void LateUpdate()
    {
        if (earth == null || particle == null || !_mainCached) return;

        Vector3 toEarth = earth.position - transform.position;
        float dist = toEarth.magnitude;
        if (dist <= 1e-5f) return;

        // 로컬 Forward(+Z)를 지구 방향으로 정렬
        Vector3 dir = toEarth / Mathf.Max(dist, 1e-5f);
        transform.rotation = Quaternion.LookRotation(dir, Vector3.up);

        // 자동 수명 보정 기능은 제거됨 — 파티클 수명은 파티클 시스템에서 제어
    }

    private void OnValidate()
    {
        if (particle == null) particle = GetComponent<ParticleSystem>();

        // 에디터에서도 Local 시뮬레이션 공간 유지
        if (particle != null)
        {
            var m = particle.main;
            m.simulationSpace = ParticleSystemSimulationSpace.Local;
        }
    }
}
