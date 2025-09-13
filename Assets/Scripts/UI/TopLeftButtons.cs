using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using UnityEngine.Serialization;

/// <summary>
/// 화면 상단 왼쪽의 3개 버튼(오디오 토글, 진동 토글, 재시작)을 관리하는 스크립트.
/// - 오디오/진동 토글의 로직은 비워 둔다.
/// - 재시작 버튼은 "게임 시작 전" 상태(Ready)로 되돌린다.
/// - 재시작 버튼은 게임이 시작된 뒤(Playing)부터 활성화한다.
/// </summary>
public class TopLeftButtons : MonoBehaviour
{
    [Header("버튼 참조")]
    [SerializeField] private Button audioToggleButton;     // 오디오 토글 버튼
    [SerializeField] private Button vibrationToggleButton; // 스마트폰 진동 토글 버튼
    [SerializeField] private Button restartButton;         // 재시작 버튼

    [Header("오디오 아이콘")]
    [Tooltip("오디오 토글 버튼의 아이콘 이미지(비워두면 버튼의 Image 사용)")]
    [SerializeField] private Image audioIcon;
    [Tooltip("음소거 해제(사운드 ON) 상태의 아이콘 스프라이트")]
    [SerializeField] private Sprite audioOnSprite;
    [Tooltip("음소거(사운드 OFF) 상태의 아이콘 스프라이트")]
    [SerializeField] private Sprite audioOffSprite;

    [Header("진동 아이콘")]
    [Tooltip("진동 토글 버튼의 아이콘 이미지(비워두면 버튼의 Image 사용)")]
    [SerializeField, FormerlySerializedAs("viberationIcon")] private Image vibrationIcon;
    [Tooltip("진동 ON 상태의 아이콘 스프라이트")]
    [SerializeField] private Sprite vibrationOnSprite;
    [Tooltip("진동 OFF 상태의 아이콘 스프라이트")]
    [SerializeField] private Sprite vibrationOffSprite;

    private GameManager _gm;

    private void Awake()
    {
        // 버튼 클릭 리스너 연결
        if (audioToggleButton != null)
            audioToggleButton.onClick.AddListener(OnClickAudioToggle);
        if (vibrationToggleButton != null)
            vibrationToggleButton.onClick.AddListener(OnClickVibrationToggle);
        if (restartButton != null)
            restartButton.onClick.AddListener(OnClickRestart);
    }

    private void OnEnable()
    {
        if (restartButton != null)
        {
            // GameManager 참조 및 상태 변경 이벤트 구독
            _gm = GameManager.Instance != null ? GameManager.Instance : FindFirstObjectByType<GameManager>();

            if (_gm != null)
            {
                _gm.OnStateChanged += HandleStateChanged;
                HandleStateChanged(_gm.State); // 초기 상태 반영
            }
            else
            {
                Debug.LogWarning("[TopLeftButtons] GameManager를 찾지 못했습니다. 재시작 버튼 상태 제어가 비활성화됩니다.");
                SetRestartInteractable(false);
            }
        }
        // DataManager 구독 및 오디오/진동 아이콘 초기 반영
        var dm = DataManager.Instance;
        if (dm != null)
        {
            dm.OnSettingsChanged -= HandleDataSettingsChanged; // 중복 방지
            dm.OnSettingsChanged += HandleDataSettingsChanged;
            RefreshVibrationIcon(dm.VibrationEnabled);
            RefreshAudioIcon(dm.Muted);
        }
        else
        {
            Debug.LogWarning("[TopLeftButtons] DataManager를 찾지 못했습니다. 진동 아이콘 초기화가 제한됩니다.");
        }
    }

    private void OnDisable()
    {
        if (_gm != null)
        {
            _gm.OnStateChanged -= HandleStateChanged;
        }

        var dm = DataManager.Instance;
        if (dm != null)
        {
            dm.OnSettingsChanged -= HandleDataSettingsChanged;
        }
    }

    // 상태 변경 시 재시작 버튼 활성화 조건 갱신
    private void HandleStateChanged(GameManager.GameState state)
    {
        // 요구사항: 재시작 버튼은 게임 시작 후(Playing 상태)에만 활성화
        bool canRestart = state == GameManager.GameState.Playing;
        SetRestartInteractable(canRestart);
    }

    private void SetRestartInteractable(bool interactable)
    {
        if (restartButton != null)
        {
            restartButton.interactable = interactable;
        }
    }
#region Audio
    // 오디오 토글
    private void OnClickAudioToggle()
    {
        // 설정의 단일 진입점: DataManager를 통해 음소거 토글, AudioManager는 설정 변경 이벤트로 반영
        var dm = DataManager.Instance;
        if (dm != null)
        {
            bool next = !dm.Muted;
            dm.SetMuted(next);
        }
        else
        {
            Debug.LogWarning("[TopLeftButtons] DataManager 인스턴스를 찾지 못했습니다. 오디오 토글을 수행할 수 없습니다.");
        }
    }

    private void RefreshAudioIcon(bool muted)
    {
        // 아이콘 참조가 없으면 버튼의 Image로 대체
        if (audioIcon == null && audioToggleButton != null)
        {
            audioIcon = audioToggleButton.image;
        }
        if (audioIcon == null) return;

        // 스프라이트가 모두 설정된 경우 스프라이트 전환, 아니면 색상으로 대체 표현
        if (audioOnSprite != null && audioOffSprite != null)
        {
            audioIcon.sprite = muted ? audioOffSprite : audioOnSprite;
        }
        else
        {
            var c = audioIcon.color;
            c.a = muted ? 0.5f : 1f; // 음소거 시 반투명 처리
            audioIcon.color = c;
        }
    }
#endregion Audio

#region Vibration
    // 진동 토글
    private void OnClickVibrationToggle()
    {
        var dm = DataManager.Instance;
        if (dm == null)
        {
            Debug.LogWarning("[TopLeftButtons] DataManager 인스턴스를 찾지 못했습니다. 진동 토글을 수행할 수 없습니다.");
            return;
        }

        bool next = !dm.VibrationEnabled;
        dm.SetVibrationEnabled(next);

        // 켜짐으로 전환될 때 짧게 피드백
        if (next)
        {
            try
            {
                if (VibrationManager.Instance != null)
                    VibrationManager.Instance.VibrateShort();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[TopLeftButtons] 진동 피드백 중 예외: {e.Message}");
            }
        }
    }

    // 진동 On/Off 변경 시 아이콘 갱신
    private void RefreshVibrationIcon(bool enabled)
    {
        // 아이콘 참조가 없으면 버튼의 Image로 대체
        if (vibrationIcon == null && vibrationToggleButton != null)
        {
            vibrationIcon = vibrationToggleButton.image;
        }
        if (vibrationIcon == null) return;

        // 스프라이트가 모두 설정된 경우 스프라이트 전환, 아니면 색상으로 대체 표현
        if (vibrationOnSprite != null && vibrationOffSprite != null)
        {
            vibrationIcon.sprite = enabled ? vibrationOnSprite : vibrationOffSprite;
        }
        else
        {
            var c = vibrationIcon.color;
            c.a = enabled ? 1f : 0.5f; // OFF 시 반투명 처리
            vibrationIcon.color = c;
        }
    }
    #endregion Vibration

    // 설정 변경 시(음소거/진동) UI 아이콘 갱신
    private void HandleDataSettingsChanged()
    {
        var dm = DataManager.Instance;
        if (dm != null)
        {
            // DataManager 값을 단일 소스로 사용해 아이콘을 갱신한다.
            RefreshVibrationIcon(dm.VibrationEnabled);
            RefreshAudioIcon(dm.Muted);
            return;
        }

        return;
    }

    // 재시작: Ready 상태로 되돌림(즉시 시작하지 않음)
    private void OnClickRestart()
    {
        if (_gm == null)
        {
            _gm = GameManager.Instance != null ? GameManager.Instance : FindFirstObjectByType<GameManager>();
        }
        if (_gm != null)
        {
            // 게임 시작 전 상태(Ready)로만 복귀
            _gm.RestartAsync().Forget();

            // 전면 광고 집계 알림(3회마다 노출 정책)
            if (AdsManager.Instance != null)
            {
                AdsManager.Instance.NotifyRestartLikeClickAsync().Forget();
            }
        }
        else
        {
            Debug.LogWarning("[TopLeftButtons] GameManager 인스턴스가 없어 재시작을 수행할 수 없습니다.");
        }
    }
}
