using UnityEngine;

/// <summary>
/// 풀에서 생성된 인스턴스에 원본 프리팹의 키(프리팹 이름)를 부여해,
/// 반환 시 올바른 풀을 역참조할 수 있도록 돕는 태그 컴포넌트.
/// </summary>
[DisallowMultipleComponent]
public class PooledObjectTag : MonoBehaviour
{
    [SerializeField] private string _poolKey;

    public string PoolKey => _poolKey;

    public void SetKey(string key)
    {
        _poolKey = key;
    }
}

