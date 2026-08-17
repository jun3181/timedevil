// Assets/Script/loader/SceneLoader.cs
using UnityEngine;
using UnityEngine.SceneManagement;

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

    /// <summary>
    /// 일반 로드
    /// - useFaderIfExists == true 이고
    /// - 현재 씬에 SceneVisitEffectRunner가 있으면 ExitEffect 후 Load(Single) 처리
    /// - 그 외에는 SceneManager.LoadScene로 즉시 로드
    /// </summary>
    public static void Load(string sceneName, bool useFaderIfExists = true, LoadSceneMode mode = LoadSceneMode.Single)
    {
        SceneTransitionService.LoadDefault(sceneName, useFaderIfExists, mode);
    }

    /// <summary>
    /// 돌아가기(무적시간 옵션)
    /// - graceSeconds는 "돌아간 씬"에서 PlayerReturnManager가 처리하게 Pending으로만 넘김
    /// - useFaderIfExists == true면 Runner 있으면 ExitEffect 후 로드
    /// </summary>
    public static void GoBackToReturnScene(float graceSeconds = 1.0f, bool useFaderIfExists = true)
    {
        SceneTransitionService.ReturnFromBattle(graceSeconds, useFaderIfExists);
    }
}
