using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 진동 매니저: 플랫폼별 진동 API를 래핑하고, DataManager 설정과 동기화한다.
/// </summary>
public class VibrationManager : SingletonMonoBehaviour<VibrationManager>
{
    [Header("설정")]
    [SerializeField] private bool enabledByDefault = true; // 데이터가 없을 때 기본값

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

    private void OnDestroy()
    {
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

    public void VibrateShort()
    {
        VibrateMs(30);
    }

    public void VibrateMedium()
    {
        VibrateMs(60);
    }

    public void VibrateHeavy()
    {
        VibrateMs(100);
    }

    public void VibratePattern(int[] timingsMs, int[] amplitudes, int repeat = -1)
    {
        if (!CanVibrate()) return;
#if UNITY_ANDROID && !UNITY_EDITOR
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
                var effect = vibrationEffectClass.CallStatic<AndroidJavaObject>(
                    "createWaveform", ToLongArray(timingsMs), amplitudes, repeat);
                _vibrator.Call("vibrate", effect);
            }
            else
            {
                // 구버전 폴백: 간단히 첫 타이밍만 사용
                long first = (timingsMs != null && timingsMs.Length > 0) ? timingsMs[0] : 50;
                _vibrator.Call("vibrate", first);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[VibrationManager] 패턴 진동 중 예외: {e.Message}");
        }
#elif UNITY_IOS && !UNITY_EDITOR
        // iOS 기본 진동만 지원(고급 햅틱은 네이티브 연계 필요)
        Handheld.Vibrate();
#else
        // 에디터/기타: no-op
#endif
    }

    public async UniTask VibrateRepeatedAsync(int intervalMs, CancellationToken ct)
    {
        intervalMs = Mathf.Max(10, intervalMs);
        while (!ct.IsCancellationRequested)
        {
            VibrateShort();
            try
            {
                await UniTask.Delay(TimeSpan.FromMilliseconds(intervalMs), cancellationToken: ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private void VibrateMs(long ms)
    {
        if (!CanVibrate()) return;
#if UNITY_ANDROID && !UNITY_EDITOR
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
#elif UNITY_IOS && !UNITY_EDITOR
        Handheld.Vibrate();
#else
        // 에디터/기타: no-op
#endif
    }

    private bool CanVibrate()
    {
        if (!IsEnabled) return false;
        if (!SystemInfo.supportsVibration) return false;
        return true;
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private static long[] ToLongArray(int[] src)
    {
        if (src == null) return null;
        var dst = new long[src.Length];
        for (int i = 0; i < src.Length; i++) dst[i] = src[i];
        return dst;
    }
#endif
}

