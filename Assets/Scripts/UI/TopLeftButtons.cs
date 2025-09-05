using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;

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

    private void OnDisable()
    {
        if (_gm != null)
        {
            _gm.OnStateChanged -= HandleStateChanged;
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

    // 오디오 토글: 구현 비움
    private void OnClickAudioToggle()
    {
        // TODO: 오디오 설정 토글 로직은 추후 구현
        // Debug.Log("오디오 토글 클릭");
    }

    // 진동 토글: 구현 비움
    private void OnClickVibrationToggle()
    {
        // TODO: 진동 설정 토글 로직은 추후 구현
        // Debug.Log("진동 토글 클릭");
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
            _gm.RestartToReadyAsync().Forget();
        }
        else
        {
            Debug.LogWarning("[TopLeftButtons] GameManager 인스턴스가 없어 재시작을 수행할 수 없습니다.");
        }
    }
}
