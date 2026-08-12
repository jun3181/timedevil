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
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("[SceneLoader] sceneName이 비어있습니다.");
            return;
        }

        // Additive는 기본적으로 "현재 씬 나가기 연출" 개념이 애매하니 바로 로드
        if (mode != LoadSceneMode.Single)
        {
            SceneManager.LoadScene(sceneName, mode);
            return;
        }

        if (useFaderIfExists && SceneVisitEffectRunner.Current != null)
        {
            SceneVisitEffectRunner.Current.LoadSceneWithExitEffect(sceneName);
            return;
        }

        // fallback: 효과 없이 즉시 로드
        SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }

    /// <summary>
    /// 돌아가기(무적시간 옵션)
    /// - graceSeconds는 "돌아간 씬"에서 PlayerReturnManager가 처리하게 Pending으로만 넘김
    /// - useFaderIfExists == true면 Runner 있으면 ExitEffect 후 로드
    /// </summary>
    public static void GoBackToReturnScene(float graceSeconds = 1.0f, bool useFaderIfExists = true)
    {
        if (string.IsNullOrWhiteSpace(PlayerReturnContext.ReturnSceneName))
        {
            Debug.LogWarning("[SceneLoader] ReturnSceneName이 비어있습니다.");
            return;
        }

        PlayerReturnContext.GraceSecondsPending = Mathf.Max(0f, graceSeconds);
        // 로드 직후 한 프레임 내 트리거 재발동 방지용 선제 플래그
        PlayerReturnContext.IsInGracePeriod = PlayerReturnContext.GraceSecondsPending > 0f;
        BattleEncounterState.ClearPending();

        Load(PlayerReturnContext.ReturnSceneName, useFaderIfExists, LoadSceneMode.Single);
    }
}
