using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 오디오 매니저: 간단한 SFX 재생과 BGM 재생/페이드를 제공하는 싱글턴.
/// - SFX: PlayOneShot 기반 겹침 재생
/// - BGM: 단일 AudioSource 루프 + 페이드 인/아웃(UniTask)
/// - 전역 음소거 토글 제공
/// </summary>
public class AudioManager : SingletonMonoBehaviour<AudioManager>
{
    [Header("오디오 소스")]
    [SerializeField] private AudioSource bgmSource; // BGM 전용 소스
    [SerializeField] private AudioSource sfxSource; // SFX 전용 소스

    [Header("기본 설정")]
    [Tooltip("모든 씬에서 공통으로 사용할 기본 BGM")]
    [SerializeField] private AudioClip defaultBgm;
    [Tooltip("기본 페이드 시간(초)")]
    [SerializeField] private float defaultFadeSeconds = 0.5f;

    [Header("SFX 카탈로그")]
    [Tooltip("문자열 키→AudioClip 매핑 카탈로그")]
    [SerializeField] private SfxCatalog sfxCatalog;

    [Header("SFX 채널 풀")]
    [Tooltip("초기 SFX 채널(오디오 소스) 개수")]
    [SerializeField] private int initialSfxChannels = 8;
    [Tooltip("최대 SFX 채널(오디오 소스) 개수 상한")]
    [SerializeField] private int maxSfxChannels = 32;

    [Header("자동 재생")]
    [Tooltip("씬 시작 시 기본 BGM을 자동 재생(Main/Game)")]
    [SerializeField] private bool autoPlayDefaultOnStart = true;

    // 내부 상태
    private CancellationTokenSource _bgmFadeCts;
    private bool _muted;

    // 읽기 전용 프로퍼티
    public bool IsMuted => _muted;

    // 음소거 상태 변경 이벤트(true=음소거 켜짐)
    public event Action<bool> OnMuteChanged;

    private readonly List<SfxChannel> _channels = new List<SfxChannel>(32);

    protected override void Awake()
    {
        base.Awake();
        EnsureAudioSources();
        EnsureSfxChannelPool();
    }

    private void Start()
    {
        // DataManager 설정 적용 및 구독
        var dm = DataManager.Instance;
        if (dm != null)
        {
            SetMasterMute(dm.Muted);
            dm.OnSettingsChanged += HandleDataSettingsChanged;
        }

        // 시작 시 BGM 자동 재생 체크
        if (!autoPlayDefaultOnStart) return;
        if (bgmSource != null && bgmSource.isPlaying) return;

        try
        {
            if (defaultBgm != null)
            {
                PlayBgmAsync(defaultBgm, true).Forget();
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[AudioManager] 자동 BGM 재생 중 예외: {e.Message}");
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        // DataManager 구독 해제
        var dm = DataManager.Instance;
        if (dm != null)
            dm.OnSettingsChanged -= HandleDataSettingsChanged;

        // 채널 정리
        for (int i = 0; i < _channels.Count; i++)
        {
            try { _channels[i]?.Dispose(); } catch { }
        }
        _channels.Clear();
    }

    // DataManager 설정 변경 구독 메소드
    private void HandleDataSettingsChanged()
    {
        var dm = DataManager.Instance;
        if (dm != null)
        {
            SetMasterMute(dm.Muted);
        }
    }

    private void EnsureAudioSources()
    {
        // BGM/SFX용 오디오 소스 2개를 유지한다.
        if (bgmSource == null)
        {
            bgmSource = GetComponent<AudioSource>();
            if (bgmSource == null)
            {
                bgmSource = gameObject.AddComponent<AudioSource>();
            }
        }
        // BGM 소스 기본값
        bgmSource.playOnAwake = false;
        bgmSource.loop = true;
        bgmSource.spatialBlend = 0f;

        if (sfxSource == null)
        {
            // 자신에 두 번째 AudioSource를 둔다.
            // 첫 번째가 bgmSource라면, 새로 추가해 sfxSource로 사용
            var all = GetComponents<AudioSource>();
            if (all != null && all.Length >= 2)
            {
                // 이미 2개 이상 있다면, bgmSource가 아닌 첫 소스를 선택
                for (int i = 0; i < all.Length; i++)
                {
                    if (all[i] != null && all[i] != bgmSource)
                    {
                        sfxSource = all[i];
                        break;
                    }
                }
            }
            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
            }
        }

        // SFX 소스 기본값
        sfxSource.playOnAwake = false;
        sfxSource.loop = false;
        sfxSource.spatialBlend = 0f;
    }

    #region 단순 재생/정지/Mute 등 API
    /// <summary>
    /// 카탈로그에서 키로 클립과 기본 볼륨을 조회한다.
    /// </summary>
    public bool TryGetSfxClip(string key, out AudioClip clip, out float defaultVolume)
    {
        clip = null;
        defaultVolume = 1f;

        if (string.IsNullOrWhiteSpace(key)) return false;
        if (sfxCatalog == null) return false;

        if (!sfxCatalog.TryGetEntry(key, out var entry) || entry == null || entry.Clip == null) return false;

        clip = entry.Clip;
        defaultVolume = entry.DefaultVolume <= 0f ? 1f : entry.DefaultVolume;
        return true;
    }

    /// <summary>
    /// 문자열 키로 SFX를 재생한다. 카탈로그가 없거나 키를 찾지 못하면 경고를 출력하고 무시한다.
    /// </summary>
    public void PlaySfx(string key, float volumeScale = 1f)
    {
        if (string.IsNullOrWhiteSpace(key)) return;
        if (_muted) return; // 음소거 시 SFX 재생 가드
        if (sfxCatalog == null)
        {
            Debug.LogWarning("[AudioManager] SFX 카탈로그가 연결되지 않았습니다. PlaySfx(string) 호출을 무시합니다.");
            return;
        }

        if (!TryGetSfxClip(key, out var clip, out var baseVol))
        {
            Debug.LogWarning($"[AudioManager] 카탈로그에서 SFX 키를 찾지 못했습니다: {key}");
            return;
        }

        float vol = Mathf.Clamp01(volumeScale) * Mathf.Clamp01(baseVol);
        sfxSource.PlayOneShot(clip, vol);
    }

    /// <summary>
    /// BGM 재생(페이드 아웃 후 클립 교체 → 페이드 인). 같은 클립이면 무시한다.
    /// </summary>
    public async UniTask PlayBgmAsync(AudioClip clip, bool loop = true, float fadeSeconds = -1f)
    {
        if (clip == null)
        {
            Debug.LogWarning("[AudioManager] PlayBgmAsync에 null 클립이 전달되었습니다.");
            return;
        }

        if (bgmSource == null) EnsureAudioSources();

        // 동일 트랙이면 재시작 없이 유지
        if (bgmSource.clip == clip && bgmSource.isPlaying) return;

        float dur = fadeSeconds >= 0f ? fadeSeconds : Mathf.Max(0f, defaultFadeSeconds);

        // 중복 페이드 취소 후 새 CTS 생성
        var ct = RenewCts(ref _bgmFadeCts);

        float targetVol = bgmSource.volume;
        float startVol = targetVol;

        // 현재 재생 중이면 페이드 아웃
        if (bgmSource.isPlaying && bgmSource.clip != null && dur > 0f && startVol > 0f)
        {
            await FadeVolumeAsync(bgmSource, startVol, 0f, dur, ct);
        }

        // 교체 및 시작
        bgmSource.Stop();
        bgmSource.clip = clip;
        bgmSource.loop = loop;
        bgmSource.volume = 0f;
        bgmSource.Play();

        // 페이드 인
        if (dur > 0f && targetVol > 0f)
        {
            await FadeVolumeAsync(bgmSource, 0f, targetVol, dur, ct);
        }
        else
        {
            bgmSource.volume = targetVol;
        }
    }

    /// <summary>
    /// BGM 정지(페이드 아웃 후 Stop).
    /// </summary>
    public async UniTask StopBgmAsync(float fadeSeconds = -1f)
    {
        if (bgmSource == null || !bgmSource.isPlaying) return;

        float dur = fadeSeconds >= 0f ? fadeSeconds : Mathf.Max(0f, defaultFadeSeconds);

        var ct = RenewCts(ref _bgmFadeCts);

        float startVol = bgmSource.volume;
        if (dur > 0f && startVol > 0f)
        {
            await FadeVolumeAsync(bgmSource, startVol, 0f, dur, ct);
        }
        bgmSource.Stop();
        bgmSource.clip = null;
    }

    /// <summary>
    /// 공용 볼륨 페이드 헬퍼: 지정 AudioSource의 볼륨을 from→to로 seconds 동안 선형 보간한다.
    /// </summary>
    private static async UniTask FadeVolumeAsync(AudioSource src, float from, float to, float seconds, CancellationToken ct)
    {
        if (src == null) return;
        seconds = Mathf.Max(0.0001f, seconds);
        float t = 0f;
        while (t < seconds)
        {
            if (ct.IsCancellationRequested) return;
            t += Time.unscaledDeltaTime; // 페이드에 시간 정지 영향 최소화
            float k = Mathf.Clamp01(t / seconds);
            src.volume = Mathf.Lerp(from, to, k);
            await UniTask.Yield(PlayerLoopTiming.Update, ct);
        }
        src.volume = to;
    }

    /// <summary>
    /// 전역 음소거 설정. 모든 소스/채널에 일괄 적용한다.
    /// </summary>
    public void SetMasterMute(bool muted)
    {
        _muted = muted;
        ApplyMuteToAll();
        Debug.Log($"[AudioManager] 전역 음소거: {(muted ? "켜짐" : "꺼짐")} ");
        try
        {
            OnMuteChanged?.Invoke(_muted);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[AudioManager] OnMuteChanged 이벤트 알림 중 예외: {e.Message}");
        }
    }

    public void ToggleMasterMute()
    {
        SetMasterMute(!_muted);
    }

    public void SetBgmVolume(float value)
    {
        if (bgmSource == null) EnsureAudioSources();
        bgmSource.volume = Mathf.Clamp01(value);
    }

    public void SetSfxVolume(float value)
    {
        if (sfxSource == null) EnsureAudioSources();
        sfxSource.volume = Mathf.Clamp01(value);
    }
    #endregion 단순 재생/정지/Mute 등 API

    #region 핸들/채널 기반 API

    /// <summary>
    /// 단발 SFX를 전용 채널에서 재생하고 핸들을 반환한다.
    /// </summary>
    public SfxHandle PlayOneShotHandle(string key, float volume = 1f)
    {
        if (_muted) return null;
        if (!TryGetSfxClip(key, out var clip, out var baseVol)) return null;
        float vol = Mathf.Clamp01(volume) * Mathf.Clamp01(baseVol);
        var ch = AcquireChannel(owner: null);
        if (ch == null) return null;
        ch.Play(clip, looping: false, volume: vol);
        // 자동 해제 감시
        ch.WatchAndReleaseAsync(this).Forget();
        return new SfxHandle(ch);
    }

    /// <summary>
    /// Owner에 결합된 단발 SFX 재생.
    /// </summary>
    public SfxHandle PlayAttached(string key, Transform owner, float volume = 1f)
    {
        if (_muted) return null;
        if (owner == null) return PlayOneShotHandle(key, volume);
        if (!TryGetSfxClip(key, out var clip, out var baseVol)) return null;
        float vol = Mathf.Clamp01(volume) * Mathf.Clamp01(baseVol);
        var ch = AcquireChannel(owner);
        if (ch == null) return null;
        ch.Play(clip, looping: false, volume: vol);
        ch.WatchAndReleaseAsync(this).Forget();
        return new SfxHandle(ch);
    }

    /// <summary>
    /// Owner에 결합된 루프 SFX 재생.
    /// </summary>
    public SfxHandle PlayLoopAttached(string key, Transform owner, float volume = 1f)
    {
        if (_muted) return null;
        if (!TryGetSfxClip(key, out var clip, out var baseVol)) return null;
        float vol = Mathf.Clamp01(volume) * Mathf.Clamp01(baseVol);
        var ch = AcquireChannel(owner);
        if (ch == null) return null;
        ch.Play(clip, looping: true, volume: vol);
        // Owner 소멸 감시(루프는 재생 끝이 없으므로 핸들이나 Owner 이벤트로 정지해야 함)
        ch.WatchOwnerAndAutoStopAsync(this).Forget();
        return new SfxHandle(ch);
    }

    /// <summary>
    /// 특정 핸들의 재생을 중지한다(선택적 페이드 아웃).
    /// </summary>
    public void Stop(SfxHandle handle, float fadeOutSeconds = 0.03f)
    {
        if (handle == null) return;
        var ch = handle.Channel;
        ch?.StopAsync(fadeOutSeconds).Forget();
    }

    /// <summary>
    /// 특정 Owner에 연결된 모든 채널을 중지한다.
    /// </summary>
    public void StopAllForOwner(Transform owner, float fadeOutSeconds = 0.03f)
    {
        if (owner == null) return;
        for (int i = 0; i < _channels.Count; i++)
        {
            var ch = _channels[i];
            if (ch == null) continue;
            if (ch.Owner == owner && ch.InUse)
            {
                ch.StopAsync(fadeOutSeconds).Forget();
            }
        }
    }
    #endregion 핸들/채널 기반 API

    #region SFX 채널/핸들 시스템

    /// <summary>
    /// SFX 재생에 대한 경량 핸들. 내부 채널을 간접 제어하며 상태 조회와 중지를 제공한다.
    /// </summary>
    public sealed class SfxHandle
    {
        internal readonly SfxChannel Channel;

        /// <summary>
        /// 내부용 생성자. AudioManager와 채널을 바인딩한다.
        /// </summary>
        /// <param name="ch">연결할 재생 채널</param>
        internal SfxHandle(SfxChannel ch)
        {
            Channel = ch;
        }

        /// <summary>
        /// 현재 핸들이 가리키는 채널이 재생 중인지 여부.
        /// </summary>
        public bool IsPlaying => Channel != null && Channel.InUse && Channel.Source != null && Channel.Source.isPlaying;

        /// <summary>
        /// 핸들이 가리키는 재생을 중지한다.
        /// </summary>
        /// <param name="fadeOutSeconds">선택적 페이드 아웃 시간(초)</param>
        public void Stop(float fadeOutSeconds = 0.03f)
        {
            Channel?.StopAsync(fadeOutSeconds).Forget();
        }
    }

    /// <summary>
    /// SFX 전용 재생 채널. 단발/루프 재생을 담당하며 풀에서 획득/반납되어 재사용된다.
    /// </summary>
    internal sealed class SfxChannel
    {
        /// <summary>
        /// 실제 사운드를 출력하는 Unity 오디오 소스.
        /// </summary>
        public readonly AudioSource Source;

        /// <summary>
        /// 채널 사용 중 여부. 재생 중이거나 예약/감시 작업이 진행 중일 때 true.
        /// </summary>
        public bool InUse { get; private set; }

        /// <summary>
        /// 현재 재생이 루프인지 여부.
        /// </summary>
        public bool Looping { get; private set; }

        /// <summary>
        /// 채널이 추적하는 채널 자신을 소유한 Transform.
        /// </summary>
        public Transform Owner { get; set; }

        /// <summary>
        /// 마지막 사용(상태 갱신/재생/정지) 시각(Time.unscaledTime 기준).
        /// </summary>
        public float LastUseTime { get; private set; }
        private float _baseVolume = 1f;
        private CancellationTokenSource _cts;

        /// <summary>
        /// 새 채널을 생성한다. 기본적으로 비사용 상태로 초기화된다.
        /// </summary>
        public SfxChannel(AudioSource src)
        {
            Source = src;
            InUse = false;
            Looping = false;
            Owner = null;
            LastUseTime = Time.unscaledTime;
        }

        /// <summary>
        /// 재생을 중지하고 내부 상태를 초기값으로 되돌린다.
        /// 풀 반납 전 호출되며, 오디오 소스 설정을 기본값으로 복구한다.
        /// </summary>
        public void ResetState()
        {
            // 재생 중지 및 상태 초기화
            try { Source.Stop(); } catch { }
            Source.clip = null;
            Source.loop = false;
            Source.volume = 1f;
            InUse = false;
            Looping = false;
            Owner = null;
            LastUseTime = Time.unscaledTime;
            AudioManager.CancelAndClearCts(ref _cts);
        }

        /// <summary>
        /// 지정한 클립을 재생한다.
        /// </summary>
        public void Play(AudioClip clip, bool looping, float volume)
        {
            if (clip == null) return;
            InUse = true;
            Looping = looping;
            Source.loop = looping;
            Source.clip = clip;
            _baseVolume = Mathf.Clamp01(volume);
            Source.volume = _baseVolume;
            LastUseTime = Time.unscaledTime;
            try { Source.Play(); } catch { }
        }

        /// <summary>
        /// 페이드 아웃 후 재생을 중지한다. 페이드 시간이 0 이하이면 즉시 정지한다.
        /// </summary>
        public async UniTaskVoid StopAsync(float fadeOutSeconds)
        {
            if (!InUse) { ForceStopImmediate(); return; }
            var ct = AudioManager.RenewCts(ref _cts);
            float dur = Mathf.Max(0f, fadeOutSeconds);
            if (dur > 0f && Source != null)
            {
                float from = Source.volume;
                await AudioManager.FadeVolumeAsync(Source, from, 0f, dur, ct);
            }
            ForceStopImmediate();
        }

        /// <summary>
        /// 즉시 재생을 중지하고 내부 상태 일부를 초기화한다(볼륨은 기본값으로 복구).
        /// </summary>
        public void ForceStopImmediate()
        {
            try { Source.Stop(); } catch { }
            InUse = false;
            Looping = false;
            Source.clip = null;
            Source.loop = false;
            Source.volume = _baseVolume;
            AudioManager.CancelAndClearCts(ref _cts);
            LastUseTime = Time.unscaledTime;
        }

        /// <summary>
        /// 단발 재생을 감시하여 자연 종료 시 채널을 풀로 반납한다.
        /// </summary>
        /// <param name="am">반납 호출에 사용할 AudioManager</param>
        public async UniTaskVoid WatchAndReleaseAsync(AudioManager am)
        {
            // 단발 감시: 재생 종료 시 채널 반환
            var ct = AudioManager.RenewCts(ref _cts);
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    if (Source == null) break;
                    if (!Source.isPlaying)
                    {
                        break;
                    }
                    await UniTask.Yield(PlayerLoopTiming.Update, ct);
                }
            }
            catch { }
            finally
            {
                ResetState();
            }
        }

        /// <summary>
        /// 루프 재생을 소유자 Transform의 생존과 활성 상태를 감시하여, 소유자가 사라지거나 비활성화되면 자동 정지 후 풀로 반납한다.
        /// </summary>
        /// <param name="am">반납 호출에 사용할 AudioManager</param>
        public async UniTaskVoid WatchOwnerAndAutoStopAsync(AudioManager am)
        {
            // 루프 감시: Owner가 사라지면 자동 정지
            var ct = AudioManager.RenewCts(ref _cts);
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    if (Owner == null) break;
                    if (!Owner.gameObject.activeInHierarchy) break;
                    await UniTask.Yield(PlayerLoopTiming.Update, ct);
                }
            }
            catch { }
            finally
            {
                ResetState();
            }
        }

        /// <summary>
        /// 채널이 보유한 리소스를 정리한다. 내부 AudioSource를 파괴한다.
        /// </summary>
        public void Dispose()
        {
            AudioManager.CancelAndClearCts(ref _cts);
            if (Source != null)
            {
                try { UnityEngine.Object.Destroy(Source); } catch { }
            }
        }
    }

    // SFX 채널 풀을 준비
    private void EnsureSfxChannelPool()
    {
        initialSfxChannels = Mathf.Clamp(initialSfxChannels, 0, Mathf.Max(0, maxSfxChannels));
        maxSfxChannels = Mathf.Max(0, maxSfxChannels);
        // 이미 존재한다면 크기만 상향 조절
        for (int i = _channels.Count; i < initialSfxChannels; i++)
        {
            _channels.Add(CreateChannel());
        }
        // 음소거 상태 반영
        ApplyMuteToAll();
    }

    private SfxChannel CreateChannel()
    {
        var src = gameObject.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.loop = false;
        src.spatialBlend = 0f;
        src.mute = _muted;
        return new SfxChannel(src);
    }

    private void ApplyMuteToAll()
    {
        // 소스 및 채널에 일괄 적용
        if (bgmSource != null) bgmSource.mute = _muted;
        if (sfxSource != null) sfxSource.mute = _muted;
        for (int i = 0; i < _channels.Count; i++)
        {
            var ch = _channels[i];
            if (ch == null) continue;
            ch.Source.mute = _muted;
        }
    }

    // CTS를 취소/폐기하고 새로 생성하여 토큰을 반환한다.
    private static CancellationToken RenewCts(ref CancellationTokenSource cts)
    {
        cts?.Cancel();
        cts?.Dispose();
        cts = new CancellationTokenSource();
        return cts.Token;
    }

    // CTS를 취소/폐기하고 null로 설정한다.
    private static void CancelAndClearCts(ref CancellationTokenSource cts)
    {
        cts?.Cancel();
        cts?.Dispose();
        cts = null;
    }

    private SfxChannel AcquireChannel(Transform owner)
    {
        // 유휴 채널 검색
        for (int i = 0; i < _channels.Count; i++)
        {
            var ch = _channels[i];
            if (ch != null && !ch.InUse)
            {
                ch.ResetState();
                ch.Owner = owner;
                return ch;
            }
        }
        // 생성 가능하면 생성
        if (_channels.Count < maxSfxChannels)
        {
            var ch = CreateChannel();
            ch.Owner = owner;
            _channels.Add(ch);
            return ch;
        }
        // 상한 초과 시 가장 오래된 채널 선별 정지 후 재사용
        SfxChannel candidate = null;
        float oldest = float.MaxValue;
        for (int i = 0; i < _channels.Count; i++)
        {
            var ch = _channels[i];
            if (ch == null) continue;
            if (ch.LastUseTime < oldest)
            {
                oldest = ch.LastUseTime;
                candidate = ch;
            }
        }
        if (candidate != null)
        {
            candidate.ResetState();
            candidate.Owner = owner;
            return candidate;
        }
        return null;
    }

    # endregion SFX 채널/핸들 시스템
}
