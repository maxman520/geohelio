using UnityEngine;
using UnityEngine.SceneManagement;
using Cysharp.Threading.Tasks;

namespace GeoHelio.UI
{
    /// <summary>
    /// 메인 메뉴의 버튼 동작을 관리하는 컨트롤러.
    /// - 시작 버튼: 지정된 씬으로 전환
    /// - 랭킹 버튼: 추후 구현 예정
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private string targetSceneName = GameConstants.Scenes.Game; // 전환 대상 씬 이름
        [SerializeField] private bool showLog = true; // 디버그 로그 출력 여부
        [SerializeField] private GuidePanel guidePanel; // 게임 방법 패널 참조

        private bool loading;

        // 버튼 클릭 SFX는 AudioManager.OnClickBtn()을 직접 사용하세요.

        /// <summary>
        /// 시작 버튼 OnClick에 연결할 메서드.
        /// UniTask를 사용해 비동기 씬 로드를 수행한다.
        /// </summary>
        public async void OnClickStart()
        {
            AudioManager.Instance.PlaySfx("OnClickBtn");
            if (loading)
            {
                if (showLog)
                {
                    Debug.Log("이미 로딩 중입니다.");
                }
                return;
            }

            if (string.IsNullOrEmpty(targetSceneName))
            {
                Debug.LogError("전환할 씬 이름이 설정되지 않았습니다.");
                return;
            }

            loading = true;

            try
            {
                if (showLog)
                {
                    Debug.Log($"'{targetSceneName}' 씬 로드를 시작합니다.");
                }

                var op = SceneManager.LoadSceneAsync(targetSceneName, LoadSceneMode.Single);
                await UniTask.WaitUntil(() => op.isDone);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"씬 전환 중 오류가 발생했습니다: {e.Message}");
            }
            finally
            {
                loading = false;
            }
        }

        /// <summary>
        /// 가이드 버튼 OnClick에 연결할 메서드. (추후 구현)
        /// </summary>
        public void OnClickGuide()
        {
            AudioManager.Instance.PlaySfx("OnClickBtn");
            if (guidePanel == null)
            {
                Debug.LogWarning("GuidePanel 참조가 비어있습니다. 인스펙터에서 연결하세요.");
                return;
            }
            // 계획서에 따라 Show 호출(비동기)
            guidePanel.Show().Forget();
        }
    }
}
