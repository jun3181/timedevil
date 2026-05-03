// HPUIBinder.cs (교체)
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// 전투 UI에 HP를 "현재/최대" 형태로 표시.
/// 우선 EnemyRuntime가 있으면 그 값을 사용, 없으면 기존 컴포넌트 리플렉션으로 폴백.
public class HPUIBinder : MonoBehaviour
{
    [Header("Player UI")]
    [SerializeField] private TMP_Text playerHpText;
    [SerializeField] private Slider playerHpBar;

    [Header("Enemy UI")]
    [SerializeField] private TMP_Text enemyHpText;
    [SerializeField] private Slider enemyHpBar;

    [Header("Sources")]
    [SerializeField] private PlayerData playerData;     // Start에서 PlayerDataRuntime로 보충 가능
    [SerializeField] private EnemyRuntime enemyRuntime; // 새 경로(우선 사용)

    // 폴백: 예전 Enemy 컴포넌트(Enemy1 등)
    private MonoBehaviour enemyComp;
    private FieldInfo enemyCurHpField;
    private FieldInfo enemyMaxHpField;

    // ---- Public API ----
    public void BindPlayer(PlayerData data)
    {
        playerData = data;
        Refresh();
    }

    public void BindEnemyRuntime(EnemyRuntime rt)
    {
        if (enemyRuntime != null) enemyRuntime.OnChanged -= Refresh;
        enemyRuntime = rt;
        if (enemyRuntime != null) enemyRuntime.OnChanged += Refresh;
        Refresh();
    }

    // 옛 방식(폴백) 유지
    public void BindEnemy(MonoBehaviour enemy)
    {
        enemyComp = enemy;
        CacheEnemyHPFields(enemyComp);
        Refresh();
    }

    public void Refresh()
    {
        // Player
        if (playerData != null)
        {
            int cur = Mathf.Max(0, playerData.currentHP);
            int max = Mathf.Max(1, playerData.maxHP);

            if (playerHpText != null)
                playerHpText.text = $"HP : {cur} / {max}";
            UpdateBar(playerHpBar, cur, max);
        }

        // Enemy: Runtime 우선
        if (enemyRuntime != null)
        {
            int cur = Mathf.Max(0, enemyRuntime.currentHP);
            int max = Mathf.Max(1, enemyRuntime.maxHP);

            if (enemyHpText != null)
                enemyHpText.text = $"HP : {cur} / {max}";
            UpdateBar(enemyHpBar, cur, max);
        }
        else if (enemyComp != null && enemyCurHpField != null && enemyMaxHpField != null)
        {
            int cur = Mathf.Max(0, (int)enemyCurHpField.GetValue(enemyComp));
            int max = Mathf.Max(1, (int)enemyMaxHpField.GetValue(enemyComp));

            if (enemyHpText != null)
                enemyHpText.text = $"HP : {cur} / {max}";
            UpdateBar(enemyHpBar, cur, max);
        }
    }

    // ---- Unity ----
    private void Start()
    {
        if (playerData == null)
        {
            var runtime = FindObjectOfType<PlayerDataRuntime>();
            if (runtime != null) playerData = runtime.Data;
        }

        if (enemyRuntime == null)
            enemyRuntime = EnemyRuntime.Instance;

        if (enemyRuntime != null)
            enemyRuntime.OnChanged += Refresh;

        Refresh();
    }

    private void OnDestroy()
    {
        if (enemyRuntime != null)
            enemyRuntime.OnChanged -= Refresh;
    }

    // ---- Helpers (fallback) ----
    private void CacheEnemyHPFields(MonoBehaviour comp)
    {
        enemyCurHpField = null;
        enemyMaxHpField = null;
        if (comp == null) return;

        var t = comp.GetType();
        enemyCurHpField = t.GetField("currentHP", BindingFlags.Public | BindingFlags.Instance);
        enemyMaxHpField = t.GetField("maxHP", BindingFlags.Public | BindingFlags.Instance);

        if (enemyCurHpField == null || enemyMaxHpField == null)
            Debug.LogWarning($"[HPUIBinder] Enemy '{t.Name}'에서 currentHP / maxHP 필드를 찾지 못했습니다.");
    }

    private static void UpdateBar(Slider bar, int cur, int max)
    {
        if (bar == null) return;
        bar.minValue = 0f;
        bar.maxValue = Mathf.Max(1, max);
        bar.value = Mathf.Clamp(cur, 0, max);
    }
}
