using UnityEngine;

[DefaultExecutionOrder(-500)] // EnemyBootstrapper보다 먼저 실행

public class BattleEnemyLoader : MonoBehaviour
{
    [SerializeField] private bool alsoWriteToSelectedEnemy = true;

    private void Awake()
    {
        var id = ObjectNameRuntime.Instance ? ObjectNameRuntime.Instance.EnemyIDToLoad : null;
        Debug.Log($"[BattleEnemyLoader] ObjectNameRuntime id='{id}'");

        if (!string.IsNullOrEmpty(id) && alsoWriteToSelectedEnemy)
        {
            var sel = SelectedEnemyRuntime.Instance;
            if (!sel)
            {
                var go = new GameObject("SelectedEnemyRuntime");
                sel = go.AddComponent<SelectedEnemyRuntime>();
            }
            sel.SetEnemyName(id);
            Debug.Log($"[BattleEnemyLoader] SelectedEnemyRuntime <- '{id}'");
        }
    }
}

