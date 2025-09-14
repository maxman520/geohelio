using UnityEngine;

/// <summary>
/// 진동 매니저: 플랫폼별 진동 API를 래핑하고, DataManager 설정과 동기화한다.
/// </summary>
public class VibrationManager : SingletonMonoBehaviour<VibrationManager>
{
    [Header("설정")]
    [SerializeField] private bool enabledByDefault = true; // 데이터가 없을 때 기본값

    private const int ShortMs = 30;
    private const int MediumMs = 60;
    private const int HeavyMs = 100;
    public void VibrateShort() => VibrateMs(ShortMs);
    public void VibrateMedium() => VibrateMs(MediumMs);
    public void VibrateHeavy()  => VibrateMs(HeavyMs);

    public bool IsEnabled { get; private set; } = true;

#if UNITY_ANDROID && !UNITY_EDITOR
    private AndroidJavaObject _vibrator;
    private int _apiLevel;
#endif

    protected override void Awake()
    {
        base.Awake();
        IsEnabled = enabledByDefault;
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            // 진동 관련 참조 초기화
            var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            _vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");

            var version = new AndroidJavaClass("android.os.Build$VERSION");
            _apiLevel = version.GetStatic<int>("SDK_INT");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[VibrationManager] 안드로이드 진동 초기화 중 예외: {e.Message}");
        }
#endif
    }

    private void Start()
    {
        // DataManager 값 적용
        var dm = DataManager.Instance;
        if (dm != null)
        {
            IsEnabled = dm.VibrationEnabled;
            dm.OnSettingsChanged += HandleSettingsChanged;
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        var dm = DataManager.Instance;
        if (dm != null)
        {
            dm.OnSettingsChanged -= HandleSettingsChanged;
        }
    }

    private void HandleSettingsChanged()
    {
        var dm = DataManager.Instance;
        if (dm != null)
        {
            IsEnabled = dm.VibrationEnabled;
        }
    }

    private void VibrateMs(long ms)
    {
        if (!CanVibrate()) return;
#if UNITY_ANDROID && !UNITY_EDITOR // Android
        try
        {
            if (_vibrator == null)
            {
                Debug.LogWarning("[VibrationManager] Vibrator 객체가 없습니다.");
                return;
            }
            if (_apiLevel >= 26)
            {
                var vibrationEffectClass = new AndroidJavaClass("android.os.VibrationEffect");
                var effect = vibrationEffectClass.CallStatic<AndroidJavaObject>("createOneShot", ms, 255);
                _vibrator.Call("vibrate", effect);
            }
            else
            {
                _vibrator.Call("vibrate", ms);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[VibrationManager] 진동 중 예외: {e.Message}");
        }
#elif UNITY_IOS && !UNITY_EDITOR // IOS
        Handheld.Vibrate();
#else
        // 기타: no-op
#endif
    }

    private bool CanVibrate()
    {
        if (!IsEnabled) return false;
        if (!SystemInfo.supportsVibration) return false;
        return true;
    }

}

