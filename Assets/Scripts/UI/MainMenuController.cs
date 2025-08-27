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
        [SerializeField] private string targetSceneName = "GameScene"; // 전환 대상 씬 이름
        [SerializeField] private bool showLog = true; // 디버그 로그 출력 여부

        private bool loading;

        /// <summary>
        /// 시작 버튼 OnClick에 연결할 메서드.
        /// UniTask를 사용해 비동기 씬 로드를 수행한다.
        /// </summary>
        public async void OnClickStart()
        {
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
        /// 랭킹 버튼 OnClick에 연결할 메서드. (추후 구현)
        /// </summary>
        public void OnClickRanking()
        {
            Debug.Log("랭킹 화면은 추후에 구현됩니다.");
        }
    }
}

