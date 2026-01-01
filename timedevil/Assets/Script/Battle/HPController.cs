using System;
using UnityEngine;

public class HPController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private PlayerDataRuntime playerData;
    [SerializeField] private EnemyRuntime enemyData;

    [Header("Pawns (for hit test)")]
    [SerializeField] private Transform playerPawn;
    [SerializeField] private Transform enemyPawn;

    public Faction CurrentDamageTarget { get; private set; } = Faction.Enemy;

    private HPUIBinder _hpUI;
    public void InjectRefs(PlayerDataRuntime pdr, EnemyRuntime er, HPUIBinder binder = null)
    {
        if (pdr != null) playerData = pdr;
        if (er != null) enemyData = er;
        if (binder != null) _hpUI = binder;

    }
    // 필요 시 개별 주입도 가능하도록
    public void SetEnemyRuntime(EnemyRuntime er) => enemyData = er;
    public void SetPlayerDataRuntime(PlayerDataRuntime pdr) => playerData = pdr;
    void Awake()
    {
        if (!playerData) playerData = FindObjectOfType<PlayerDataRuntime>(true);
        if (!enemyData) enemyData = EnemyRuntime.Instance ?? FindObjectOfType<EnemyRuntime>(true);
        _hpUI = FindObjectOfType<HPUIBinder>(true);

        if (!playerPawn)
        {
            var mc = FindObjectOfType<MoveController>(true);
            if (mc) playerPawn = mc.GetComponentInChildren<Transform>();
        }
        if (!enemyPawn)
        {
            var mc = FindObjectOfType<MoveController>(true);
            if (mc) enemyPawn = mc.GetComponentInChildren<Transform>();
        }
    }

    // ---- ATK / DEF ----
    public int GetAttack(Faction who)
    {
        if (who == Faction.Player)
        {
            // 플레이어 쪽은 필드명이 프로젝트마다 다를 수 있으므로 폴백으로 탐색
            return ReadIntFrom(playerData?.Data, "atk", "attack", "ATK");
        }
        return enemyData != null ? enemyData.attack : 0;
    }

    public int GetDefense(Faction who)
    {
        if (who == Faction.Player)
        {
            return ReadIntFrom(playerData?.Data, "def", "defense", "DEF");
        }
        return enemyData != null ? enemyData.defense : 0;
    }

    // ---- HP ----
    public int GetHP(Faction who)
    {
        if (who == Faction.Player)
            return ReadIntFrom(playerData?.Data, "currentHP");
        return enemyData != null ? enemyData.currentHP : 0;
    }

    public void ApplyDamage(Faction target, int amount)
    {
        amount = Mathf.Max(0, amount);
        CurrentDamageTarget = target;

        // ★ 혹시 주입이 아직 안됐으면 한 번 더 지연해결 시도
        if (enemyData == null) enemyData = EnemyRuntime.Instance ?? FindObjectOfType<EnemyRuntime>(true);
        if (playerData == null) playerData = PlayerDataRuntime.Instance ?? FindObjectOfType<PlayerDataRuntime>(true);
        if (_hpUI == null) _hpUI = FindObjectOfType<HPUIBinder>(true);

        if (target == Faction.Player)
        {
            var pd = playerData?.Data;
            if (pd != null)
            {
                int cur = ReadIntFrom(pd, "currentHP");
                int max = ReadIntFrom(pd, "maxHP");
                cur = Mathf.Clamp(cur - amount, 0, Mathf.Max(1, max));
                WriteIntFieldOrProp(pd, "currentHP", cur);
                Debug.Log($"[HP] Player -{amount} → {cur}");
                _hpUI?.Refresh();
            }
            else
            {
                Debug.LogWarning("[HPController] PlayerDataRuntime.Data is null");
            }
        }
        else
        {
            if (enemyData != null)
            {
                // amount는 최종 데미지이므로 EnemyRuntime.TakeDamage()에 raw 보정
                int raw = amount + Mathf.Max(0, enemyData.defense);
                enemyData.TakeDamage(raw);   // 내부에서 OnChanged 호출 → HPUI 자동 갱신
                Debug.Log($"[HP] Enemy -{amount} → {enemyData.currentHP}");
            }
            else
            {
                Debug.LogWarning("[HPController] EnemyRuntime is null");
            }
        }
    }

    public Vector3 GetWorldPositionOfPawn(Faction who)
    {
        if (who == Faction.Player && playerPawn) return playerPawn.position;
        if (who == Faction.Enemy && enemyPawn) return enemyPawn.position;
        return Vector3.positiveInfinity;
    }

    public void BeginCardHitTest(Faction target)
    {
        CurrentDamageTarget = target;
    }

    // ---------- 리플렉션 보조 ----------
    private int ReadIntFrom(object obj, params string[] names)
    {
        if (obj == null || names == null) return 0;

        var t = obj.GetType();
        const System.Reflection.BindingFlags BF =
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.NonPublic;

        foreach (var name in names)
        {
            var f = t.GetField(name, BF);
            if (f != null && f.FieldType == typeof(int)) return (int)f.GetValue(obj);

            var p = t.GetProperty(name, BF);
            if (p != null && p.PropertyType == typeof(int) && p.CanRead) return (int)p.GetValue(obj);
        }
        return 0;
    }

    private void WriteIntFieldOrProp(object obj, string name, int value)
    {
        if (obj == null) return;

        var t = obj.GetType();
        const System.Reflection.BindingFlags BF =
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.NonPublic;

        var f = t.GetField(name, BF);
        if (f != null && f.FieldType == typeof(int)) { f.SetValue(obj, value); return; }

        var p = t.GetProperty(name, BF);
        if (p != null && p.PropertyType == typeof(int) && p.CanWrite) { p.SetValue(obj, value); return; }

        Debug.LogWarning($"[HPController] '{t.Name}'에 '{name}'(int) 쓰기 실패.");
    }

}
