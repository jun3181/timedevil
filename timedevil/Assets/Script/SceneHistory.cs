using UnityEngine;

/// <summary>
/// 단순 이전 씬 기록용 헬퍼
/// - Card 씬 진입 전에 SetLastScene() 호출해서 기록
/// - Card 씬에서 SceneHistory.LastSceneName으로 돌아가기
/// </summary>
public static class SceneHistory
{
    /// <summary>마지막으로 들어온 씬 이름</summary>
    public static string LastSceneName { get; private set; }

    /// <summary>현재 씬 이름 저장</summary>
    public static void SetLastScene(string sceneName)
    {
        if (!string.IsNullOrEmpty(sceneName))
        {
            LastSceneName = sceneName;
            Debug.Log($"[SceneHistory] 이전 씬 기록됨: {LastSceneName}");
        }
    }

    /// <summary>저장된 씬 기록 초기화</summary>
    public static void Clear()
    {
        LastSceneName = null;
        Debug.Log("[SceneHistory] 씬 기록 초기화");
    }
}
