// EnemyBootstrapper.cs
using UnityEngine;

public class EnemyBootstrapper : MonoBehaviour
{
    [Header("Database & Selection")]
    [SerializeField] private EnemyDatabaseSO database;
    [SerializeField] private string enemyIdOverride = ""; // 비워두면 SelectedEnemyRuntime 사용

    [Header("Runtime Target")]
    [SerializeField] private EnemyRuntime runtimePrefab; // 씬에 없다면 생성용(선택)
    private EnemyRuntime runtime;

    void Awake()
    {
        runtime = EnemyRuntime.Instance;
        if (!runtime)
        {
            runtime = FindObjectOfType<EnemyRuntime>();
            if (!runtime && runtimePrefab)
                runtime = Instantiate(runtimePrefab);
        }
        if (!runtime)
        {
            var go = new GameObject("EnemyRuntime");
            runtime = go.AddComponent<EnemyRuntime>();
        }
    }

    void Start()
    {
        if (!database)
        {
            Debug.LogError("[EnemyBootstrapper] EnemyDatabaseSO is missing.");
            return;
        }

        // 1) 어떤 적을 쓸지 결정
        string chosenId = !string.IsNullOrWhiteSpace(enemyIdOverride)
            ? enemyIdOverride
            : (SelectedEnemyRuntime.Instance ? SelectedEnemyRuntime.Instance.enemyName : null);

        if (string.IsNullOrWhiteSpace(chosenId))
        {
            Debug.LogWarning("[EnemyBootstrapper] No enemy id given. Fallback to first in DB.");
            if (database.enemies.Count > 0) chosenId = database.enemies[0].enemyId;
        }

        var so = database.GetById(chosenId);
        if (!so)
        {
            Debug.LogError($"[EnemyBootstrapper] Enemy id '{chosenId}' not found in DB.");
            return;
        }

        // 2) 런타임 초기화
        runtime.InitializeFromSO(so);

        // 3) HP UI와 연결(있으면)
        var hp = FindObjectOfType<HPUIBinder>();
        if (hp)
        {
            // Player 바인딩(있으면)
            var pdr = FindObjectOfType<PlayerDataRuntime>();
            if (pdr) hp.BindPlayer(pdr.Data);

            // EnemyRuntime 바인딩
            hp.BindEnemyRuntime(runtime);
            hp.Refresh();
        }
    }
}
