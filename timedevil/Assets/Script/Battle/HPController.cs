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

    [Header("Player Defeat")]
    [SerializeField] private bool loadMyroomOnPlayerZeroHp = true;
    [SerializeField] private string myroomSceneName = "Myroom";

    [Header("Enemy Defeat")]
    [SerializeField] private bool returnToPreviousSceneOnEnemyZeroHp = true;
    [SerializeField, Min(0f)] private float enemyDefeatReturnGraceSeconds = 1f;

    public Faction CurrentDamageTarget { get; private set; } = Faction.Enemy;

    private HPUIBinder _hpUI;
    private bool playerDefeatLoadStarted;
    private bool enemyDefeatReturnStarted;
    private bool battleResultPipeStarted;
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
            if (mc) playerPawn = mc.GetPawn(Faction.Player);
        }
        if (!enemyPawn)
        {
            var mc = FindObjectOfType<MoveController>(true);
            if (mc) enemyPawn = mc.GetPawn(Faction.Enemy);
        }
    }

    // ---- ATK / DEF ----
    public int GetAttack(Faction who)
    {
        int value;
        if (who == Faction.Player)
        {
            // 플레이어 쪽은 필드명이 프로젝트마다 다를 수 있으므로 폴백으로 탐색
            value = ReadIntFrom(playerData?.Data, "atk", "attack", "ATK");
        }
        else
        {
            value = enemyData != null ? enemyData.attack : 0;
        }

        var support = SupportController.Instance ?? FindObjectOfType<SupportController>(true);
        if (support != null)
            value += support.GetAttackModifier(who);

        return Mathf.Max(0, value);
    }

    public int GetDefense(Faction who)
    {
        int value;
        if (who == Faction.Player)
        {
            value = ReadIntFrom(playerData?.Data, "def", "defense", "DEF");
        }
        else
        {
            value = enemyData != null ? enemyData.defense : 0;
        }

        var support = SupportController.Instance ?? FindObjectOfType<SupportController>(true);
        if (support != null)
            value += support.GetDefenseModifier(who);

        return Mathf.Max(0, value);
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

        var support = SupportController.Instance ?? FindObjectOfType<SupportController>(true);
        if (amount > 0 && support != null && support.IsInvincible(target))
        {
            Debug.Log($"[HP] {target} is invincible. Damage {amount} ignored.");
            _hpUI?.Refresh();
            return;
        }

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
                if (cur <= 0)
                    HandlePlayerDefeat();
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
                if (enemyData.IsDead)
                    HandleEnemyDefeat();
            }
            else
            {
                Debug.LogWarning("[HPController] EnemyRuntime is null");
            }
        }
    }

    public int PayHP(Faction target, int amount, bool allowDefeat = false)
    {
        amount = Mathf.Max(0, amount);
        if (amount <= 0) return 0;

        if (enemyData == null) enemyData = EnemyRuntime.Instance ?? FindObjectOfType<EnemyRuntime>(true);
        if (playerData == null) playerData = PlayerDataRuntime.Instance ?? FindObjectOfType<PlayerDataRuntime>(true);
        if (_hpUI == null) _hpUI = FindObjectOfType<HPUIBinder>(true);

        if (target == Faction.Player)
        {
            var pd = playerData?.Data;
            if (pd == null)
            {
                Debug.LogWarning("[HPController] PlayerDataRuntime.Data is null");
                return 0;
            }

            int cur = ReadIntFrom(pd, "currentHP");
            int max = Mathf.Max(1, ReadIntFrom(pd, "maxHP"));
            int min = allowDefeat ? 0 : 1;
            int next = Mathf.Clamp(cur - amount, min, max);
            int paid = Mathf.Max(0, cur - next);
            WriteIntFieldOrProp(pd, "currentHP", next);
            Debug.Log($"[HP] Player paid {paid} HP -> {next}");
            _hpUI?.Refresh();
            if (next <= 0)
                HandlePlayerDefeat();
            return paid;
        }

        if (enemyData == null)
        {
            Debug.LogWarning("[HPController] EnemyRuntime is null");
            return 0;
        }

        int enemyCur = enemyData.currentHP;
        int enemyMin = allowDefeat ? 0 : 1;
        int enemyNext = Mathf.Clamp(enemyCur - amount, enemyMin, Mathf.Max(1, enemyData.maxHP));
        int enemyPaid = Mathf.Max(0, enemyCur - enemyNext);
        enemyData.currentHP = enemyNext;
        Debug.Log($"[HP] Enemy paid {enemyPaid} HP -> {enemyData.currentHP}");
        _hpUI?.Refresh();
        if (enemyData.IsDead)
            HandleEnemyDefeat();
        return enemyPaid;
    }

    public void Heal(Faction target, int amount)
    {
        amount = Mathf.Max(0, amount);
        if (amount <= 0) return;

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
                cur = Mathf.Clamp(cur + amount, 0, Mathf.Max(1, max));
                WriteIntFieldOrProp(pd, "currentHP", cur);
                Debug.Log($"[HP] Player +{amount} -> {cur}");
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
                enemyData.Heal(amount);
                Debug.Log($"[HP] Enemy +{amount} -> {enemyData.currentHP}");
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

    private void HandlePlayerDefeat()
    {
        if (!loadMyroomOnPlayerZeroHp || playerDefeatLoadStarted) return;
        playerDefeatLoadStarted = true;

        if (string.IsNullOrWhiteSpace(myroomSceneName))
        {
            Debug.LogWarning("[HPController] Player defeat scene name is empty.");
            return;
        }

        var menu = FindObjectOfType<BattleMenuController>(true);
        if (menu != null) menu.EnableInput(false);

        var turnManager = TurnManager.Instance ?? FindObjectOfType<TurnManager>(true);
        if (turnManager != null)
            turnManager.QueueBattleResult(BattleResultKind.Defeat);
        else
            ExecuteBattleResultPipe(BattleResultKind.Defeat);
    }

    public void ExecuteBattleResultPipe(BattleResultKind result)
    {
        if (battleResultPipeStarted)
            return;

        battleResultPipeStarted = true;

        if (result == BattleResultKind.Victory)
        {
            ExecuteVictoryPipe();
            return;
        }

        if (result == BattleResultKind.Defeat)
            ExecuteDefeatPipe();
    }

    public void BeginCardHitTest(Faction target)
    {
        CurrentDamageTarget = target;
    }

    private void HandleEnemyDefeat()
    {
        if (!returnToPreviousSceneOnEnemyZeroHp || enemyDefeatReturnStarted)
            return;

        enemyDefeatReturnStarted = true;
        var menu = FindObjectOfType<BattleMenuController>(true);
        if (menu != null) menu.EnableInput(false);

        var turnManager = TurnManager.Instance ?? FindObjectOfType<TurnManager>(true);
        if (turnManager != null)
            turnManager.QueueBattleResult(BattleResultKind.Victory);
        else
            ExecuteBattleResultPipe(BattleResultKind.Victory);
    }

    private void ExecuteVictoryPipe()
    {
        if (!returnToPreviousSceneOnEnemyZeroHp)
            return;

        BattleVictoryReturnContext.QueueArmedVictory();
        BattleEncounterState.ConsumePendingVictory();
        SceneTransitionService.ReturnFromBattle(enemyDefeatReturnGraceSeconds);
    }

    private void ExecuteDefeatPipe()
    {
        if (!loadMyroomOnPlayerZeroHp)
            return;

        if (string.IsNullOrWhiteSpace(myroomSceneName))
        {
            Debug.LogWarning("[HPController] Player defeat scene name is empty.");
            return;
        }

        BattleEncounterState.ClearPending();
        BattleVictoryReturnContext.ClearAll();
        PlayerReturnContext.ClearReturnCore();

        Debug.Log($"[HPController] Player HP reached 0. Loading '{myroomSceneName}' at Spawn_Room2_LoadGame_PlayerDead.");

        SceneTransitionService.EnterMyroom(MyroomEntryPoint.Spawn_Room2_LoadGame_PlayerDead, myroomSceneName, useFaderIfExists: true);
    }

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
