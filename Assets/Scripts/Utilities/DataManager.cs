using System;
using System.IO;
using System.Text;
using System.Threading;
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
    }

    // 현재 설정(읽기 전용 외부 접근)
    public int BestScore { get; private set; }
    public bool Muted { get; private set; }
    public bool VibrationEnabled { get; private set; } = true;

    // 이벤트
    public event Action OnSettingsChanged;          // 음소거/진동 변경 시

    private string _filePath;
    private readonly object _ioLock = new object();

    protected override void Awake()
    {
        base.Awake();
        _filePath = Path.Combine(Application.persistentDataPath, "gh_settings.json");
    }

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
        try { OnSettingsChanged?.Invoke(); } catch { }
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
            vibrationEnabled = VibrationEnabled
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
        try { OnSettingsChanged?.Invoke(); } catch { }
    }

    /// <summary>
    /// 진동 사용 여부 설정(변경 시 저장 및 이벤트 알림).
    /// </summary>
    public void SetVibrationEnabled(bool value)
    {
        if (VibrationEnabled == value) return;
        VibrationEnabled = value;
        SaveAsync().Forget();
        try { OnSettingsChanged?.Invoke(); } catch { }
    }

    private void OnApplicationPause(bool pause)
    {
        // 앱이 백그라운드로 전환될 때 즉시 저장 시도
        if (pause)
        {
            SaveAsync().Forget();
        }
    }

    private void OnApplicationQuit()
    {
        // 앱 종료 직전에 마지막 저장 시도
        SaveAsync().Forget();
    }
}
