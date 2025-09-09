using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace GeoHelio.UI
{
    /// <summary>
    /// 메인 메뉴의 게임 방법(Guide) 패널 제어 스크립트.
    /// - 전환 연출 없이 단순 활성/비활성로 페이지 전환
    /// - Prev/Next/Close 버튼 핸들러 제공
    /// - 비동기 메서드는 UniTask 사용
    /// </summary>
    public class GuidePanel : MonoBehaviour
    {
        [Header("필수 참조")]
        [SerializeField] private GameObject backgroundDim; // 배경 딤(모달 차단)
        [SerializeField] private GameObject page1; // 1페이지 오브젝트
        [SerializeField] private GameObject page2; // 2페이지 오브젝트
        [SerializeField] private GameObject page3; // 3페이지 오브젝트

        [Header("버튼")]
        [SerializeField] private Button prevButton; // 이전 버튼
        [SerializeField] private Button nextButton; // 다음 버튼
        [SerializeField] private Button closeButton; // 닫기 버튼

        [Header("페이지 인디케이터(선택)")]
        [SerializeField] private Image[] pageDots; // 현재 페이지 표시(선택)

        private int _pageIndex; // 0~2
        private bool _isOpen;

        /// <summary>
        /// 패널을 표시한다. 첫 페이지(0번)로 설정한다.
        /// </summary>
        public async UniTask Show()
        {
            // Debug.Log("가이드 패널을 표시합니다.");
            gameObject.SetActive(true);
            if (backgroundDim != null) backgroundDim.SetActive(true);
            _isOpen = true;
            _pageIndex = 0;
            SetPage(_pageIndex);
            UpdateButtons();
            UpdateDots();
            await UniTask.Yield();
        }

        /// <summary>
        /// 패널을 숨긴다.
        /// </summary>
        public async UniTask Hide()
        {
            // Debug.Log("가이드 패널을 숨깁니다.");
            _isOpen = false;
            if (backgroundDim != null) backgroundDim.SetActive(false);
            gameObject.SetActive(false);
            await UniTask.Yield();
        }

        /// <summary>
        /// 다음 페이지로 이동한다.
        /// </summary>
        public void OnClickNext()
        {
            AudioManager.Instance.PlaySfx("OnClickBtn");
            if (!_isOpen) return;
            if (_pageIndex >= 2) return;
            _pageIndex++;
            SetPage(_pageIndex);
            UpdateButtons();
            UpdateDots();
        }

        /// <summary>
        /// 이전 페이지로 이동한다.
        /// </summary>
        public void OnClickPrev()
        {
            AudioManager.Instance.PlaySfx("OnClickBtn");
            if (!_isOpen) return;
            if (_pageIndex <= 0) return;
            _pageIndex--;
            SetPage(_pageIndex);
            UpdateButtons();
            UpdateDots();
        }

        /// <summary>
        /// 패널을 닫는다.
        /// </summary>
        public void OnClickClose()
        {
            AudioManager.Instance.PlaySfx("OnClickBtn");
            Hide().Forget();
        }

        /// <summary>
        /// 지정 페이지를 활성화하고 나머지는 비활성화한다.
        /// </summary>
        private void SetPage(int pageIndex)
        {
            if (page1 != null) page1.SetActive(pageIndex == 0);
            if (page2 != null) page2.SetActive(pageIndex == 1);
            if (page3 != null) page3.SetActive(pageIndex == 2);
        }

        /// <summary>
        /// 페이지 경계에 따라 Prev/Next 버튼의 상호작용 가능 여부를 갱신한다.
        /// </summary>
        private void UpdateButtons()
        {
            if (prevButton != null) prevButton.interactable = _pageIndex > 0;
            if (nextButton != null) nextButton.interactable = _pageIndex < 2;
        }

        /// <summary>
        /// 페이지 인디케이터(Dot)를 갱신한다.
        /// </summary>
        private void UpdateDots()
        {
            if (pageDots == null || pageDots.Length == 0) return;
            for (int i = 0; i < pageDots.Length; i++)
            {
                if (pageDots[i] == null) continue;
                // 페이지에 해당하는 Dots만 알파를 1로, 나머지는 0.35
                pageDots[i].color = (i == _pageIndex) ? Color.white : new Color(1f, 1f, 1f, 0.35f);

            }
        }
    }
}

