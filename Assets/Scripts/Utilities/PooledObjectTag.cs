using UnityEngine;

/// <summary>
/// 풀에서 생성된 인스턴스에 원본 프리팹 참조를 부여해,
/// 반환 시 올바른 풀을 역참조할 수 있도록 돕는 태그 컴포넌트.
/// </summary>
[DisallowMultipleComponent]
public class PooledObjectTag : MonoBehaviour
{
    [SerializeField] private GameObject _sourcePrefab; // 원본 프리팹 참조

    public GameObject SourcePrefab => _sourcePrefab;

    public void SetPrefab(GameObject prefab)
    {
        _sourcePrefab = prefab;
    }
}
