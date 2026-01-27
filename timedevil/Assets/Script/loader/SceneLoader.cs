// Assets/Script/loader/SceneLoader.cs
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public static class SceneLoader
{
    public static void SaveReturnPoint(Transform playerT, Transform enemyT)
    {
        PlayerReturnContext.ReturnSceneName = SceneManager.GetActiveScene().name;
        PlayerReturnContext.HasReturnPosition = playerT != null;
        PlayerReturnContext.ReturnPosition = playerT ? (Vector2)playerT.position : Vector2.zero;

        PlayerReturnContext.MonsterReturnPosition = enemyT ? (Vector2)enemyT.position : Vector2.zero;
        PlayerReturnContext.MonsterNameInScene = enemyT ? enemyT.gameObject.name : "";

        if (enemyT)
        {
            var id = enemyT.GetComponent<EnemyInstanceId>();
            PlayerReturnContext.MonsterInstanceId = id ? id.Id : enemyT.gameObject.name;
        }
        else
        {
            PlayerReturnContext.MonsterInstanceId = "";
        }
    }

    // ✅ SceneFader는 "현재 씬에 있는 것"만 사용한다 (없으면 즉시 로드)
    public static void Load(string sceneName, bool useFaderIfExists = true)
    {
        if (useFaderIfExists)
        {
            var fader = Object.FindObjectOfType<SceneFader>(true);
            if (fader != null)
            {
                fader.LoadSceneWithFadeOut(sceneName);
                return;
            }
        }

        SceneManager.LoadScene(sceneName);
    }

    public static void GoBackToReturnScene(float graceSeconds = 1.0f, bool useFaderIfExists = true)
    {
        if (string.IsNullOrWhiteSpace(PlayerReturnContext.ReturnSceneName))
        {
            Debug.LogWarning("[SceneLoader] ReturnSceneName이 비어있습니다.");
            return;
        }

        PlayerReturnContext.IsInGracePeriod = graceSeconds > 0f;
        PlayerReturnContext.GraceSecondsPending = Mathf.Max(0f, graceSeconds);

        var host = SceneLoaderHost.Ensure();
        host.StartCoroutine(host.CoClearGrace(graceSeconds));

        Load(PlayerReturnContext.ReturnSceneName, useFaderIfExists);
    }
}

class SceneLoaderHost : MonoBehaviour
{
    public static SceneLoaderHost Instance { get; private set; }

    public static SceneLoaderHost Ensure()
    {
        if (!Instance)
        {
            var go = new GameObject("[SceneLoaderHost]");
            Object.DontDestroyOnLoad(go);
            Instance = go.AddComponent<SceneLoaderHost>();
        }
        return Instance;
    }

    public IEnumerator CoClearGrace(float sec)
    {
        if (sec > 0f) yield return new WaitForSeconds(sec);
        PlayerReturnContext.IsInGracePeriod = false;
    }
}
