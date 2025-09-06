using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

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
    [Tooltip("모든 씬에서 공통으로 사용할 기본 BGM(선택)")]
    [SerializeField] private AudioClip defaultBgm;
    [Tooltip("기본 페이드 시간(초)")]
    [SerializeField] private float defaultFadeSeconds = 0.5f;

    [Header("SFX 카탈로그")]
    [Tooltip("문자열 키→AudioClip 매핑 카탈로그")]
    [SerializeField] private SfxCatalog sfxCatalog;

    // 내부 상태
    private CancellationTokenSource _bgmFadeCts;
    private bool _muted;

    // 읽기 전용 프로퍼티
    public bool IsMuted => _muted;

    // 음소거 상태 변경 이벤트(true=음소거 켜짐)
    public event Action<bool> OnMuteChanged;

    protected override void Awake()
    {
        base.Awake();
        EnsureAudioSources();
        // 초기 음소거 상태를 시스템에 반영
        SetMasterMute(_muted);
    }

    [Header("자동 재생")]
    [Tooltip("씬 시작 시 기본 BGM을 자동 재생(Main/Game)")]
    [SerializeField] private bool autoPlayDefaultOnStart = true;

    private void Start()
    {
        // DataManager 설정 적용 및 구독
        try
        {
            var dm = DataManager.Instance;
            if (dm != null)
            {
                SetMasterMute(dm.Muted);
                dm.OnSettingsChanged += HandleDataSettingsChanged;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[AudioManager] DataManager 연동 중 예외: {e.Message}");
        }

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

    private void OnDestroy()
    {
        try
        {
            var dm = DataManager.Instance;
            if (dm != null) dm.OnSettingsChanged -= HandleDataSettingsChanged;
        }
        catch { }
    }

    private void HandleDataSettingsChanged()
    {
        try
        {
            var dm = DataManager.Instance;
            if (dm != null)
            {
                SetMasterMute(dm.Muted);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[AudioManager] 설정 반영 중 예외: {e.Message}");
        }
    }

    private void EnsureAudioSources()
    {
        // 동일 GameObject에 BGM/SFX용 오디오 소스 2개를 유지한다.
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

        sfxSource.playOnAwake = false;
        sfxSource.loop = false;
        sfxSource.spatialBlend = 0f;
    }

    /// <summary>
    /// SFX 재생(겹침 허용). clip이 null이면 무시한다.
    /// </summary>
    public void PlaySfx(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null) return;
        if (!isActiveAndEnabled) return;
        if (sfxSource == null) EnsureAudioSources();

        volumeScale = Mathf.Clamp01(volumeScale);
        try
        {
            sfxSource.PlayOneShot(clip, volumeScale);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[AudioManager] SFX 재생 중 예외: {e.Message}");
        }
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

        // 동일 트랙이면 재시작 없이 유지(단순화)
        if (bgmSource.clip == clip && bgmSource.isPlaying)
        {
            return;
        }

        float dur = fadeSeconds >= 0f ? fadeSeconds : Mathf.Max(0f, defaultFadeSeconds);

        // 중복 페이드 취소
        _bgmFadeCts?.Cancel();
        _bgmFadeCts?.Dispose();
        _bgmFadeCts = new CancellationTokenSource();
        var ct = _bgmFadeCts.Token;

        float targetVol = bgmSource.volume;
        float startVol = targetVol;

        // 현재 재생 중이면 페이드 아웃
        if (bgmSource.isPlaying && bgmSource.clip != null && dur > 0f && startVol > 0f)
        {
            await FadeBgmAsync(startVol, 0f, dur, ct);
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
            await FadeBgmAsync(0f, targetVol, dur, ct);
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

        _bgmFadeCts?.Cancel();
        _bgmFadeCts?.Dispose();
        _bgmFadeCts = new CancellationTokenSource();
        var ct = _bgmFadeCts.Token;

        float startVol = bgmSource.volume;
        if (dur > 0f && startVol > 0f)
        {
            await FadeBgmAsync(startVol, 0f, dur, ct);
        }
        bgmSource.Stop();
        bgmSource.clip = null;
    }

    /// <summary>
    /// 전역 음소거 설정. 간단히 AudioListener.pause 와 소스 mute를 함께 갱신한다.
    /// </summary>
    public void SetMasterMute(bool muted)
    {
        _muted = muted;
        AudioListener.pause = muted;
        if (bgmSource != null) bgmSource.mute = muted;
        if (sfxSource != null) sfxSource.mute = muted;
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

    /// <summary>
    /// 문자열 키로 SFX를 재생합니다. 카탈로그가 없거나 키를 찾지 못하면 경고를 출력하고 무시합니다.
    /// </summary>
    public void PlaySfx(string key, float volumeScale = 1f)
    {
        if (string.IsNullOrWhiteSpace(key)) return;
        if (sfxCatalog == null)
        {
            Debug.LogWarning("[AudioManager] SFX 카탈로그가 연결되지 않았습니다. PlaySfx(string) 호출을 무시합니다.");
            return;
        }

        if (!sfxCatalog.TryGetEntry(key, out var entry) || entry == null || entry.Clip == null)
        {
            Debug.LogWarning($"[AudioManager] 카탈로그에서 SFX 키를 찾지 못했습니다: {key}");
            return;
        }

        float baseVol = entry.DefaultVolume <= 0f ? 1f : entry.DefaultVolume;
        float vol = Mathf.Clamp01(volumeScale) * Mathf.Clamp01(baseVol);
        PlaySfx(entry.Clip, vol);
    }

    private async UniTask FadeBgmAsync(float from, float to, float seconds, CancellationToken ct)
    {
        if (bgmSource == null) return;
        seconds = Mathf.Max(0.0001f, seconds);
        float t = 0f;
        while (t < seconds)
        {
            if (ct.IsCancellationRequested) return;
            t += Time.unscaledDeltaTime; // 페이드에 시간 정지 영향 최소화
            float k = Mathf.Clamp01(t / seconds);
            bgmSource.volume = Mathf.Lerp(from, to, k);
            await UniTask.Yield(PlayerLoopTiming.Update, ct);
        }
        bgmSource.volume = to;
    }
}
