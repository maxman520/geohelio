using System;
using Cysharp.Threading.Tasks;
using GoogleMobileAds.Api;
using UnityEngine;

/// <summary>
/// 광고 관리 매니저.
/// - GMA 초기화 및 인터스티셜 사전 로드/표시
/// - 재시작/다시하기 클릭 집계에 따라 3회마다 광고 노출
/// - 광고 중 오디오 음소거 및 종료 시 원복
/// - 모든 비동기 동작은 UniTask 기반
/// </summary>

public class AdsManager : SingletonMonoBehaviour<AdsManager>
{
    private InterstitialAd _interstitial;
    private bool _isShowing;
    private bool _preloaded;
    private bool _snapshotMuted; // 광고 시작 전 음소거 상태 스냅샷

    protected override void Awake()
    {
        base.Awake();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        // 조기 생성 및 초기화 시도
        var inst = Instance;
        inst.InitializeAsync().Forget();
    }

    /// <summary>
    /// GMA SDK 초기화 후 인터스티셜 사전 로드.
    /// </summary>
    public async UniTask InitializeAsync()
    {
        try
        {
            MobileAds.Initialize(_ => { });
            await PreloadInterstitialAsync();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[AdsManager] 초기화 중 예외: {e.Message}");
        }
    }

    /// <summary>
    /// 인터스티셜 광고를 사전 로드한다.
    /// 이미 로드된 경우 중복 로드를 방지한다.
    /// </summary>
    public async UniTask PreloadInterstitialAsync()
    {
        if (_preloaded && _interstitial != null) return;

        string unitId = GetInterstitialUnitId();
        if (string.IsNullOrEmpty(unitId))
        {
            Debug.LogWarning("[AdsManager] 유효한 전면 광고 단위 ID가 없습니다.");
            return;
        }

        var tcs = new UniTaskCompletionSource<bool>();

        try
        {
            var request = new AdRequest();
            InterstitialAd.Load(unitId, request, (ad, err) =>
            {
                // 기존 콜백을 재사용하여 상태 설정/훅/로그를 일원화한다.
                InterstitialLoadCallback(ad, err);
                // 성공 여부를 결과로 전달한다.
                tcs.TrySetResult(err == null && ad != null);
            });
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[AdsManager] 인터스티셜 로드 중 예외: {e.Message}");
            _interstitial = null;
            _preloaded = false;
            tcs.TrySetResult(false);
        }

        // 콜백이 도착할 때까지 실제로 대기한다.
        await tcs.Task;
    }
    /// <summary>
    /// 전면 광고 사전 로드 콜백 함수
    /// </summary>
    public void InterstitialLoadCallback(InterstitialAd ad, LoadAdError err)
    {
        if (err != null || ad == null)
        {
            Debug.LogWarning($"[AdsManager] 인터스티셜 로드 실패: {err}");
            _interstitial = null;
            _preloaded = false;
            return;
        }
        _interstitial = ad;
        _preloaded = true;
        HookInterstitialCallbacks(ad);
        Debug.Log("[AdsManager] 인터스티셜 로드 완료");

    }

    /// <summary>
    /// Retry/Restart 버튼에서 호출. 버튼들의 클릭 집계를 알리고 필요 시 광고를 예약/표시한다.
    /// </summary>
    public async UniTask NotifyRestartLikeClickAsync()
    {
        var dm = DataManager.Instance;
        if (dm == null) return;

        dm.IncrementRestartClickCount(); // 즉시 저장

        if (dm.AdRestartClickCount % 3 == 0) // 클릭 3번 마다 광고 표시
        {
            dm.EnqueuePendingInterstitial();
            await TryShowInterstitialAsync(); // 대기중인 광고가 있다면 표시
        }
    }

    /// <summary>
    /// 표시 대기 중인 전면 광고가 있으면 가능한 즉시 표시한다.
    /// 로드가 안 되어 있으면 짧게 대기 후 포기.
    /// </summary>
    public async UniTask TryShowInterstitialAsync()
    {
        var dm = DataManager.Instance;
        if (dm == null) return;
        if (dm.AdPendingInterstitials <= 0) return;

        // 최소 간격 정책(옵션)
        if (dm.AdMinIntervalSeconds > 0 && dm.AdLastShowUnixMs > 0)
        {
            long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            long gapMs = now - dm.AdLastShowUnixMs;
            if (gapMs < dm.AdMinIntervalSeconds * 1000L)
            {
                // 간격 미충족: 다음 기회로
                return;
            }
        }

        // 프리로드가 안 되어 있으면 짧게 대기 후 재확인하고 포기
        if (!_preloaded || _interstitial == null)
        {
            await UniTask.Delay(300); // 짧게 대기
            if (!_preloaded || _interstitial == null) return;
        }

        ShowInterstitialInternal();
    }

    private void ShowInterstitialInternal()
    {
        if (_isShowing) return;
        if (_interstitial == null) return;
        _isShowing = true;

        // 오디오 스냅샷 저장 및 음소거 적용
        var am = AudioManager.Instance;
        if (am != null)
        {
            _snapshotMuted = am.IsMuted;
            am.SetMasterMute(true);
        }

        try
        {
            _interstitial.Show();
            Debug.Log("[AdsManager] 전면 광고 표시 시작");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[AdsManager] 전면 광고 표시 중 예외: {e.Message}");
            // 실패 시 상태 정리 및 쌓인 광고 수 유지
            TryRestoreAudio();
            _isShowing = false;
            _preloaded = false;
            _interstitial = null;
            PreloadInterstitialAsync().Forget();
        }
    }

    private void HookInterstitialCallbacks(InterstitialAd ad)
    {
        // 전면 광고 열림
        ad.OnAdFullScreenContentOpened += () =>
        {
            Debug.Log("[AdsManager] 전면 광고 전체 화면 열림");
        };


        // 전면 광고 닫힘
        ad.OnAdFullScreenContentClosed += () =>
        {
            try
            {
                // 오디오 복구 및 쌓인 광고 수 소비
                TryRestoreAudio();
                var dm = DataManager.Instance;
                if (dm != null)
                {
                    // 마지막 광고 표시 시각 갱신
                    dm.AdLastShowUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    dm.ConsumePendingInterstitial();
                }
            }
            finally
            {
                _isShowing = false;
                _preloaded = false;
                _interstitial = null;
                PreloadInterstitialAsync().Forget();
            }
        };

        // 전면 광고 표시 실패
        ad.OnAdFullScreenContentFailed += err =>
        {
            Debug.LogWarning($"[AdsManager] 전면 광고 표시 실패: {err}");
            TryRestoreAudio();
            _isShowing = false;
            _preloaded = false;
            _interstitial = null;
            // 실패 시 쌓인 광고 수 유지 → 다음 기회에 재시도.
            // 전면 광고 프리 로드.
            PreloadInterstitialAsync().Forget();
        };
    }

    // 오디오 복구
    private void TryRestoreAudio()
    {
        var am = AudioManager.Instance;
        if (am != null && !_snapshotMuted)
        {
            am.SetMasterMute(false);
        }
    }

    // 전면 광고 Unit ID 반환 함수
    private string GetInterstitialUnitId()
    {
#if UNITY_ANDROID
        // Android 테스트 ID
        return "ca-app-pub-3940256099942544/1033173712";
#elif UNITY_IOS
        // iOS 테스트 ID
        return "ca-app-pub-3940256099942544/4411468910";
#else
        return string.Empty;
#endif
    }

}
