using UnityEngine;
using UnityEngine.SceneManagement;

public class NextScene : MonoBehaviour
{
    public void LoadBattleScene(GameObject scanObj)
    {
        var sceneName = "battle";
        if (!Application.CanStreamedLevelBeLoaded(sceneName)) return;

        // ▶ Myroom에서 상호작용한 대상의 이름을 전투 컨텍스트로 전달
        BattleContext.EnemyName = scanObj != null ? scanObj.name : "Enemy1";

        SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }
}
