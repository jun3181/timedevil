// Assets/Script/Battle/BattleBootstrap.cs
using UnityEngine;

public class BattleBootstrap : MonoBehaviour
{
    [Header("UI Binder")]
    [SerializeField] private HPUIBinder hpUIBinder;

    [Header("Enemy (SO-first)")]
    [SerializeField] private EnemyDatabaseSO enemyDatabase;
    [SerializeField] private string enemyIdOverride = "";     // 인스펙터에서 강제 지정 시 사용
    [SerializeField] private EnemyRuntime enemyRuntimePrefab; // 없으면 런타임 자동 생성

    private EnemyRuntime enemyRt;
    private bool initialized;

    void Awake()
    {
        // PlayerDataRuntime 보장
        if (PlayerDataRuntime.Instance == null)
        {
            var go = new GameObject("PlayerDataRuntime (Auto)");
            go.AddComponent<PlayerDataRuntime>();
            Debug.Log("[BattleBootstrap] Auto-created PlayerDataRuntime in battle scene.");
        }

        // Battle 씬을 직접 실행해도 cards.json 기본 생성/로드가 되도록 보장
        if (CardStateRuntime.Instance == null)
        {
            var go = new GameObject("CardStateRuntime (Auto)");
            go.AddComponent<CardStateRuntime>();
            Debug.Log("[BattleBootstrap] Auto-created CardStateRuntime in battle scene.");
        }

        // EnemyRuntime 확보
        enemyRt = EnemyRuntime.Instance ?? FindObjectOfType<EnemyRuntime>(true);
        if (enemyRt == null)
        {
            enemyRt = enemyRuntimePrefab != null
                ? Instantiate(enemyRuntimePrefab)
                : new GameObject("EnemyRuntime").AddComponent<EnemyRuntime>();
        }

        InitializeBattleRuntime();
    }


    void Start()
    {
        InitializeBattleRuntime();
    }

    private void InitializeBattleRuntime()
    {
        if (enemyDatabase == null)
        {
            Debug.LogError("[BattleBootstrap] EnemyDatabaseSO is missing.");
            return;
        }

        if (initialized) return;
        initialized = true;

        // 1) 사용할 적 ID 결정
        string enemyId = BattleSceneLoader.enemyIdToLoad;
        if (string.IsNullOrWhiteSpace(enemyId))
            enemyId = !string.IsNullOrWhiteSpace(enemyIdOverride) ? enemyIdOverride : "Enemy1";

        Debug.Log($"[BattleBootstrap] resolved enemyId='{enemyId}'");

        // 2) DB에서 SO 검색
        var so = enemyDatabase.GetById(enemyId);
        if (so == null)
        {
            Debug.LogError($"[BattleBootstrap] Enemy id '{enemyId}' not found in DB.");
            return;
        }

        // 3) EnemyRuntime 초기화
        enemyRt.InitializeFromSO(so);

        // 4) HP UI 바인딩
        var pdr = PlayerDataRuntime.Instance ?? FindObjectOfType<PlayerDataRuntime>(true);
        if (hpUIBinder != null)
        {
            if (pdr != null) hpUIBinder.BindPlayer(pdr.Data);
            hpUIBinder.BindEnemyRuntime(enemyRt);
            hpUIBinder.Refresh();
        }

        // 5) ★★★ 전투 씬의 모든 HPController에 참조 주입
        var allHpControllers = FindObjectsOfType<HPController>(true); // 비활성 포함
        foreach (var hp in allHpControllers)
        {
            hp.InjectRefs(pdr, enemyRt, hpUIBinder); // ← HPController에 이 메서드가 있어야 함
        }

        // 6) 안전하게 한 번 더 UI 갱신
        hpUIBinder?.Refresh();

        Debug.Log($"[BattleBootstrap] Initialized from EnemySO: {so.displayName} ({so.enemyId})");
    }
}
