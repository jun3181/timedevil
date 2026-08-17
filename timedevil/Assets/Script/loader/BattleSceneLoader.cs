using UnityEngine;

/// <summary>
/// 배틀 진입 유틸: 적 ID/복귀 위치를 저장하고 배틀 씬으로 이동.
/// </summary>
public static class BattleSceneLoader
{
    public static string enemyIdToLoad;

    private static ObjectNameRuntime EnsureObjectNameRuntime()
    {
        if (ObjectNameRuntime.Instance != null)
            return ObjectNameRuntime.Instance;

        var existing = Object.FindObjectOfType<ObjectNameRuntime>(true);
        if (existing != null)
            return existing;

        var go = new GameObject("ObjectNameRuntime (Auto)");
        return go.AddComponent<ObjectNameRuntime>();
    }

    public static void Go(string battleSceneName, string enemyIdToLoad, Transform playerT, Transform enemyT)
    {
        // 1) 적 ID 기록 (ObjectNameRuntime 자동 보장)
        var runtime = EnsureObjectNameRuntime();
        if (runtime != null)
        {
            runtime.SetEnemyToLoad(enemyIdToLoad);
        }
        else
        {
            Debug.LogError("[BattleSceneLoader] ObjectNameRuntime 생성/획득에 실패했습니다.");
        }

        // 2) 복귀 정보 저장
        BattleSceneLoader.enemyIdToLoad = enemyIdToLoad;

        // 3) 배틀 씬 이동
        SceneTransitionService.EnterBattle(battleSceneName, enemyIdToLoad, playerT, enemyT, useFaderIfExists: true);
    }
}
