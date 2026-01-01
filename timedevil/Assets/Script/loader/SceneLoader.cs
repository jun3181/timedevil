// Assets/Script/loader/SceneLoader.cs
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public static class SceneLoader
{
    // 돌아올 좌표 저장
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

    // 일반 로드(페이더가 있으면 사용)
    public static void Load(string sceneName, bool useFaderIfExists = true)
    {
        if (useFaderIfExists && SceneFader.instance != null)
            SceneFader.instance.LoadSceneWithFade(sceneName);
        else
            SceneManager.LoadScene(sceneName);
    }

    // 돌아가기(무적시간 옵션)
    public static void GoBackToReturnScene(float graceSeconds = 1.0f, bool useFaderIfExists = true)
    {
        if (string.IsNullOrWhiteSpace(PlayerReturnContext.ReturnSceneName))
        {
            Debug.LogWarning("[SceneLoader] ReturnSceneName이 비어있습니다.");
            return;
        }

        // 트리거 재충돌 방지
        PlayerReturnContext.IsInGracePeriod = graceSeconds > 0f;
        PlayerReturnContext.GraceSecondsPending = Mathf.Max(0f, graceSeconds); // ⬅️ 추가

        SceneLoaderHost.Ensure().StartCoroutine(SceneLoaderHost.Instance.CoClearGrace(graceSeconds));

        Load(PlayerReturnContext.ReturnSceneName, useFaderIfExists);
    }
}

// 내부 코루틴용 호스트
class SceneLoaderHost : MonoBehaviour
{
    public static SceneLoaderHost Instance { get; private set; }
    public static SceneLoaderHost Ensure()
    {
        if (!Instance)
        {
            var go = new GameObject("[SceneLoaderHost]");
            DontDestroyOnLoad(go);
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
