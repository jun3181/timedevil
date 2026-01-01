// BattleBootstrapEnsurePlayerData.cs (새 스크립트, 배틀 씬에 빈 오브젝트에 붙이기)
using UnityEngine;

public class BattleBootstrapEnsurePlayerData : MonoBehaviour
{
    void Awake()
    {
        if (PlayerDataRuntime.Instance == null)
        {
            var go = new GameObject("PlayerDataRuntime (Auto)");
            go.AddComponent<PlayerDataRuntime>();  // Awake에서 DontDestroyOnLoad + 로드/기본값 세팅
            Debug.Log("[BattleBootstrap] Auto-created PlayerDataRuntime in battle scene.");
        }
    }
}
