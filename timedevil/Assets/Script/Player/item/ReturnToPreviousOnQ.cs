using UnityEngine;
using UnityEngine.SceneManagement;

public class ReturnToPreviousOnQ : MonoBehaviour
{
    [Header("Return options")]
    [SerializeField] private float graceSeconds = 0.5f;      // 필요시 무적/유예에 사용
    [SerializeField] private bool useFaderIfExists = true;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            if (string.IsNullOrWhiteSpace(PlayerReturnContext.ReturnSceneName))
            {
                Debug.LogWarning("[ReturnToPreviousOnQ] ReturnSceneName이 비어있습니다. 복귀할 씬이 없습니다.");
                return;
            }

            // RunController와 동일한 경로 사용
            if (typeof(SceneLoader).GetMethod("GoBackToReturnScene") != null)
            {
                SceneLoader.GoBackToReturnScene(graceSeconds, useFaderIfExists);
            }
            else
            {
                // 폴백: SceneLoader가 없다면 직접 로드
                var target = PlayerReturnContext.ReturnSceneName;
                if (SceneFader.instance)
                    SceneFader.instance.LoadSceneWithFade(target);
                else
                    SceneManager.LoadScene(target);
            }
        }
    }
}
