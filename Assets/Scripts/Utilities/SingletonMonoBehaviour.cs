using UnityEngine;

/// <summary>
/// 싱글톤 MonoBehaviour 베이스 클래스.
/// 사용법: public class GameManager : SingletonMonoBehaviour<GameManager> { }
/// </summary>
public abstract class SingletonMonoBehaviour<T> : MonoBehaviour where T : MonoBehaviour
{
    // 현재 인스턴스 참조
    private static T instance;

    // 애플리케이션 종료 중인지 여부 (종료 중에는 새로 생성하지 않기 위함)
    private static bool isQuitting;

    [SerializeField] private bool dontDestroyOnLoad = true; // 씬 전환 시 유지할지 여부

    /// <summary>
    /// 전역 접근자. 필요 시 자동으로 찾아서 없으면 생성합니다.
    /// </summary>
    public static T Instance
    {
        get
        {
            if (isQuitting)
            {
                Debug.LogWarning($"애플리케이션 종료 중이므로 싱글톤 '{typeof(T).Name}'을(를) 생성하지 않습니다.");
                return null;
            }

            if (instance == null)
            {
                instance = FindObjectOfType<T>();
                if (instance == null)
                {
                    // 씬에 존재하지 않으면 새 GameObject를 만들어 추가
                    var go = new GameObject($"[Singleton] {typeof(T).Name}");
                    instance = go.AddComponent<T>();
                }
            }
            return instance;
        }
    }

    /// <summary>
    /// 중복 인스턴스 방지 및 DontDestroyOnLoad 처리
    /// </summary>
    protected virtual void Awake()
    {
        if (instance == null)
        {
            instance = this as T;
            if (dontDestroyOnLoad)
            {
                DontDestroyOnLoad(gameObject);
            }
        }
        else if (instance != this)
        {
            Debug.LogWarning($"싱글톤 중복 인스턴스 감지: {typeof(T).Name}. 기존 인스턴스를 유지하고 현재 객체를 파괴합니다.");
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 애플리케이션 종료 플래그 설정
    /// </summary>
    protected virtual void OnApplicationQuit()
    {
        isQuitting = true;
    }

    /// <summary>
    /// 자신이 현재 인스턴스라면 참조 해제
    /// </summary>
    protected virtual void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
}
