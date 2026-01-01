using UnityEngine;

/// 배틀씬 진입을 위한 전용 유틸: 적 ID를 넘기고, 복귀 좌표 저장 후 배틀씬으로 이동
public static class BattleSceneLoader
{
    public static void Go(string battleSceneName, string enemyIdToLoad, Transform playerT, Transform enemyT)
    {
        // 1) 적 ID 전달 (ObjectNameRuntime 싱글톤 사용)
        if (ObjectNameRuntime.Instance != null)
        {
            ObjectNameRuntime.Instance.SetEnemyToLoad(enemyIdToLoad);
        }
        else
        {
            Debug.LogError("[BattleSceneLoader] ObjectNameRuntime.Instance가 없습니다. Boot 씬에 배치하세요.");
        }

        // 2) 복귀 정보를 저장
        SceneLoader.SaveReturnPoint(playerT, enemyT);

        if (enemyT && WorldNPCStateService.Instance != null)
            WorldNPCStateService.Instance.SaveSnapshot(enemyT.gameObject);

        // 3) 배틀씬으로 이동
        SceneLoader.Load(battleSceneName, useFaderIfExists: true);
    }
}
