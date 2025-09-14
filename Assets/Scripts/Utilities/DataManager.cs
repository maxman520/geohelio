using System;
using System.IO;
using System.Text;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 데이터 매니저: 최고 점수/음소거/진동 설정을 영속 저장/불러오기하고
/// 런타임 매니저(Audio/Vibration)와 동기화한다.
/// </summary>
public class DataManager : SingletonMonoBehaviour<DataManager>
{
    [Serializable]
    private class GameData
    {
        public int version = 1;
        public int bestScore = 0;
        public bool muted = false;
        public bool vibrationEnabled = true;
        // 광고 관련(버튼 클릭 횟수 집계/쌓인 광고 수/간격/마지막 표시시각)
        public int adRestartClickCount = 0;
        public int adPendingInterstitials = 0;
        public int adMinIntervalSeconds = 0;
        public long adLastShowUnixMs = 0;
    }

    // 현재 설정(읽기 전용 외부 접근)
    public int BestScore { get; private set; }
    public bool Muted { get; private set; }
    public bool VibrationEnabled { get; private set; } = true;
    // 광고 관련 읽기 전용 프로퍼티
    public int AdRestartClickCount { get; private set; }
    public int AdPendingInterstitials { get; private set; }
    public int AdMinIntervalSeconds { get; private set; } = 0; // 옵션(기본 0)
    public long AdLastShowUnixMs { get; set; }

    // 이벤트
    public event Action OnSettingsChanged;          // 음소거/진동 변경 시

    private string _filePath;
    private readonly object _ioLock = new object();

    protected override void Awake()
    {
        base.Awake();
        _filePath = Path.Combine(Application.persistentDataPath, "gh_settings.json");
    }

    // 초기화 시점 보장을 위해 첫 씬 로드 전에 Instance를 강제로 호출 (뒤늦은 생성/로드 방지)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        // 조기 생성 및 로드 시작
        var inst = Instance;
        inst.LoadAsync().Forget();
    }

    /// <summary>
    /// 설정 파일을 비동기로 불러온다. 실패 시 기본값으로 시작한다.
    /// </summary>
    public async UniTask LoadAsync()
    {
        GameData data = null;
        try
        {
            await UniTask.SwitchToThreadPool();
            if (File.Exists(_filePath))
            {
                string json;
                lock (_ioLock)
                {
                    json = File.ReadAllText(_filePath, Encoding.UTF8);
                }
                if (!string.IsNullOrWhiteSpace(json))
                {
                    data = JsonUtility.FromJson<GameData>(json);
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[DataManager] 설정 로드 중 예외: {e.Message}");
        }
        finally
        {
            await UniTask.SwitchToMainThread();
        }

        if (data == null)
        {
            // 기본값으로 시작
            data = new GameData();
        }

        ApplyData(data);
        // 최초 로드 후 이벤트로 매니저, 버튼들이 현재 상태를 재적용할 기회를 제공
        OnSettingsChanged?.Invoke();
    }

    /// <summary>
    /// 현재 상태를 파일에 저장한다.
    /// </summary>
    public async UniTask SaveAsync()
    {
        var data = new GameData
        {
            version = 1,
            bestScore = BestScore,
            muted = Muted,
            vibrationEnabled = VibrationEnabled,
            adRestartClickCount = AdRestartClickCount,
            adPendingInterstitials = AdPendingInterstitials,
            adMinIntervalSeconds = AdMinIntervalSeconds,
            adLastShowUnixMs = AdLastShowUnixMs
        };

        string json = JsonUtility.ToJson(data, prettyPrint: false);

        try
        {
            await UniTask.SwitchToThreadPool();
            lock (_ioLock)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_filePath));
                File.WriteAllText(_filePath, json, Encoding.UTF8);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[DataManager] 설정 저장 중 예외: {e.Message}");
        }
        finally
        {
            await UniTask.SwitchToMainThread();
        }
    }

    private void ApplyData(GameData data)
    {
        BestScore = Mathf.Max(0, data.bestScore);
        Muted = data.muted;
        VibrationEnabled = data.vibrationEnabled;
        // 광고 관련 필드 반영(누락 시 기본값 적용)
        AdRestartClickCount = Mathf.Max(0, data.adRestartClickCount);
        AdPendingInterstitials = Mathf.Max(0, data.adPendingInterstitials);
        AdMinIntervalSeconds = Mathf.Max(0, data.adMinIntervalSeconds);
        AdLastShowUnixMs = Math.Max(0L, data.adLastShowUnixMs);
    }

    /// <summary>
    /// 새로운 점수가 기존 최고 점수보다 높으면 갱신하고 true를 반환한다.
    /// </summary>
    public bool TrySetBestScore(int score)
    {
        if (score > BestScore)
        {
            BestScore = score;
            SaveAsync().Forget();
            return true;
        }
        return false;
    }

    /// <summary>
    /// 음소거 상태 설정(변경 시 저장 및 이벤트 알림).
    /// </summary>
    public void SetMuted(bool value)
    {
        if (Muted == value) return;
        Muted = value;
        SaveAsync().Forget();
        OnSettingsChanged?.Invoke();
    }

    /// <summary>
    /// 진동 사용 여부 설정(변경 시 저장 및 이벤트 알림).
    /// </summary>
    public void SetVibrationEnabled(bool value)
    {
        if (VibrationEnabled == value) return;
        VibrationEnabled = value;
        SaveAsync().Forget();
        OnSettingsChanged?.Invoke();
    }

    private void OnApplicationPause(bool pause)
    {
        // 앱이 백그라운드로 전환될 때 즉시 저장 시도
        if (pause)
        {
            SaveAsync().Forget();
        }
    }

    protected override void OnApplicationQuit()
    {
        base.OnApplicationQuit();
        // 앱 종료 직전에 마지막 저장 시도
        SaveAsync().Forget();
    }

    #region Ads API
    /// <summary>
    /// 재시작/재도전 클릭 누계를 1 증가시키고 즉시 저장한다.
    /// </summary>
    public void IncrementRestartClickCount()
    {
        AdRestartClickCount++;
        SaveAsync().Forget();
    }

    /// <summary>
    /// 표시해야 할 전면 광고를 대기열에 추가한다(상한 1).
    /// </summary>
    public void EnqueuePendingInterstitial()
    {
        if (AdPendingInterstitials < 1) AdPendingInterstitials = 1;
        SaveAsync().Forget();
    }

    /// <summary>
    /// 대기 중인 전면 광고를 1 소모한다.
    /// </summary>
    public void ConsumePendingInterstitial()
    {
        if (AdPendingInterstitials > 0) AdPendingInterstitials--;
        SaveAsync().Forget();
    }
    #endregion Ads API
}
