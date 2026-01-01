// BattleNextScene.cs
using UnityEngine;
using UnityEngine.SceneManagement;

public class BattleNextScene : MonoBehaviour
{
    [SerializeField] private string battleSceneName = "battle";

    // 버튼의 OnClick에 바로 연결해서 문자열 인자를 넘길 수 있음 (예: "Enemy1")
    public void GoBattleWithEnemy(string enemyName)
    {
        // 컨텍스트 없으면 즉석 생성
        if (SceneLoadContext.Instance == null)
        {
            var go = new GameObject("SceneLoadContext");
            go.AddComponent<SceneLoadContext>();
        }

        SceneLoadContext.Instance.pendingEnemyName = string.IsNullOrEmpty(enemyName) ? "Enemy1" : enemyName;

        // 씬 전환
        Time.timeScale = 1f;
        SceneManager.LoadScene(battleSceneName, LoadSceneMode.Single);
    }
}
