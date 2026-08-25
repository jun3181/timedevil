using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SupportController : MonoBehaviour
{
    public static SupportController Instance { get; private set; }

    private const string NotEnoughDiscardMessage = "버릴 패가 부족합니다";
    private const string NotEnoughHpMessage = "체력이 부족합니다";

    [SerializeField] private MoveController moveController;
    [SerializeField] private HPController hpController;
    [SerializeField] private CostController costController;

    private readonly int[] nextFreeCardCounts = new int[2];
    private readonly List<TurnHpEffect> turnHpEffects = new();
    private readonly List<StatModifier> statModifiers = new();
    private readonly List<TrapInstance> traps = new();
    private readonly List<GuardEffect> guards = new();

    private class TurnHpEffect
    {
        public string cardId;
        public Faction target;
        public int amount;
        public int remainingTurns;
    }

    private class StatModifier
    {
        public string cardId;
        public Faction target;
        public SupportStatEffectType statType;
        public int amount;
        public int remainingTurns;
    }

    private class TrapInstance
    {
        public string cardId;
        public Faction panelFaction;
        public SupportGridMask gridMask;
        public int damage;
        public int remainingTurnStarts;
        public bool removeAfterTrigger;
    }

    private class GuardEffect
    {
        public string cardId;
        public Faction target;
        public int remainingTurns;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        ResolveRefs();
    }

    private void OnEnable()
    {
        ResolveRefs();
        if (moveController != null)
        {
            moveController.OnGridChanged -= HandleGridChanged;
            moveController.OnGridChanged += HandleGridChanged;
        }
    }

    private void OnDisable()
    {
        if (moveController != null)
            moveController.OnGridChanged -= HandleGridChanged;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void ResolveRefs()
    {
        if (!moveController) moveController = FindObjectOfType<MoveController>(true);
        if (!hpController) hpController = FindObjectOfType<HPController>(true);
        if (!costController) costController = FindObjectOfType<CostController>(true);
    }

    public bool CanExecute(SupportCardSO so, Faction self, int selfCardsAlreadyCommitted, out string failMessage)
    {
        failMessage = null;
        if (so == null || so.effects == null) return true;

        int simulatedHand = Mathf.Max(0, GetHandCount(self) - Mathf.Max(0, selfCardsAlreadyCommitted));
        int simulatedHp = Mathf.Max(0, GetCurrentHP(self));

        foreach (SupportEffect effect in so.effects)
        {
            if (effect == null || effect.category != SupportEffectCategory.Cost)
                continue;

            switch (effect.costType)
            {
                case SupportCostEffectType.GainCostByDiscardHand:
                {
                    int need = Mathf.Max(1, effect.discardHandCount);
                    if (simulatedHand < need)
                    {
                        failMessage = NotEnoughDiscardMessage;
                        return false;
                    }
                    simulatedHand -= need;
                    break;
                }

                case SupportCostEffectType.GainCostByPayingHpPercent:
                {
                    int payment = CalculateHpPayment(simulatedHp, effect.hpCostPercent);
                    if (payment <= 0) break;

                    if (!effect.hpPaymentCanDefeat && simulatedHp - payment <= 0)
                    {
                        failMessage = NotEnoughHpMessage;
                        return false;
                    }

                    simulatedHp = Mathf.Max(0, simulatedHp - payment);
                    break;
                }
            }
        }

        return true;
    }

    public IEnumerator Execute(SupportCardSO so, Faction self, Faction foe)
    {
        if (so == null || so.effects == null)
            yield break;

        ResolveRefs();

        Debug.Log($"[SupportController] Execute: id={so.id}, name={so.displayName}, self={self}, foe={foe}, effects={so.effects.Count}");

        foreach (SupportEffect effect in so.effects)
        {
            if (effect == null) continue;

            switch (effect.category)
            {
                case SupportEffectCategory.Cost:
                    ExecuteCostEffect(effect, self);
                    break;
                case SupportEffectCategory.HP:
                    ExecuteHpEffect(so.id, effect, self, foe);
                    break;
                case SupportEffectCategory.Stat:
                    ExecuteStatEffect(so.id, effect, self, foe);
                    break;
                case SupportEffectCategory.Trap:
                    ExecuteTrapEffect(so.id, effect, self, foe);
                    break;
                case SupportEffectCategory.Guard:
                    ExecuteGuardEffect(so.id, effect, self, foe);
                    break;
            }

            yield return null;
        }
    }

    public void OnTurnStarted(Faction turnOwner)
    {
        ResolveRefs();
        TickTurnHpEffects(turnOwner);
        TickStatModifiers(turnOwner);
        TickGuards(turnOwner);
        TickTraps(turnOwner);
    }

    public bool HasNextCardFree(Faction owner)
    {
        return nextFreeCardCounts[ToIndex(owner)] > 0;
    }

    public bool TryConsumeNextCardFree(Faction owner)
    {
        int index = ToIndex(owner);
        if (nextFreeCardCounts[index] <= 0)
            return false;

        nextFreeCardCounts[index]--;
        Debug.Log($"[SupportController] NextCardFree consumed: owner={owner}, remaining={nextFreeCardCounts[index]}");
        return true;
    }

    public int GetAttackModifier(Faction target)
    {
        int total = 0;
        for (int i = 0; i < statModifiers.Count; i++)
        {
            StatModifier modifier = statModifiers[i];
            if (modifier.target == target && modifier.statType == SupportStatEffectType.AttackChange)
                total += modifier.amount;
        }
        return total;
    }

    public int GetDefenseModifier(Faction target)
    {
        int total = 0;
        for (int i = 0; i < statModifiers.Count; i++)
        {
            StatModifier modifier = statModifiers[i];
            if (modifier.target == target && modifier.statType == SupportStatEffectType.DefenseChange)
                total += modifier.amount;
        }
        return total;
    }

    public bool IsInvincible(Faction target)
    {
        for (int i = 0; i < guards.Count; i++)
        {
            GuardEffect guard = guards[i];
            if (guard.target == target && guard.remainingTurns > 0)
                return true;
        }
        return false;
    }

    private void ExecuteCostEffect(SupportEffect effect, Faction self)
    {
        switch (effect.costType)
        {
            case SupportCostEffectType.NextCardFree:
                AddNextFreeCards(self, effect.freeCardCount);
                break;

            case SupportCostEffectType.GainCostByPayingHpPercent:
            {
                int payment = CalculateHpPayment(GetCurrentHP(self), effect.hpCostPercent);
                int paid = hpController != null ? hpController.PayHP(self, payment, effect.hpPaymentCanDefeat) : 0;
                if (paid > 0 && costController != null)
                    costController.GainCurrent(effect.costGainAmount, effect.allowCostOverMax);
                break;
            }

            case SupportCostEffectType.GainCostByDiscardHand:
            {
                int need = Mathf.Max(1, effect.discardHandCount);
                int discarded = DiscardRandomFromHand(self, need);
                if (discarded >= need && costController != null)
                    costController.GainCurrent(effect.costGainAmount, effect.allowCostOverMax);
                break;
            }
        }
    }

    private void ExecuteHpEffect(string cardId, SupportEffect effect, Faction self, Faction foe)
    {
        foreach (Faction target in ResolveTargets(effect.target, self, foe))
        {
            if (effect.hpType == SupportHpEffectType.InstantChange)
            {
                ApplyHpChange(target, effect.hpAmount);
                continue;
            }

            turnHpEffects.Add(new TurnHpEffect
            {
                cardId = cardId,
                target = target,
                amount = effect.hpAmount,
                remainingTurns = Mathf.Max(1, effect.hpTurns)
            });
        }
    }

    private void ExecuteStatEffect(string cardId, SupportEffect effect, Faction self, Faction foe)
    {
        foreach (Faction target in ResolveTargets(effect.target, self, foe))
        {
            statModifiers.Add(new StatModifier
            {
                cardId = cardId,
                target = target,
                statType = effect.statType,
                amount = effect.statAmount,
                remainingTurns = Mathf.Max(1, effect.statTurns)
            });
        }
    }

    private void ExecuteTrapEffect(string cardId, SupportEffect effect, Faction self, Faction foe)
    {
        if (effect.trapPlacements == null || moveController == null)
            return;

        for (int i = 0; i < effect.trapPlacements.Count; i++)
        {
            SupportTrapPlacement placement = effect.trapPlacements[i];
            if (placement == null || placement.gridMask == null || placement.gridMask.IsEmpty())
                continue;

            Faction panelFaction = ResolvePanelFaction(placement.panelSide, self, foe);
            TrapInstance trap = new TrapInstance
            {
                cardId = cardId,
                panelFaction = panelFaction,
                gridMask = placement.gridMask,
                damage = Mathf.Max(0, effect.trapDamage),
                remainingTurnStarts = Mathf.Max(1, effect.trapDurationTurns) + 1,
                removeAfterTrigger = effect.removeAfterTrigger
            };

            traps.Add(trap);
            Debug.Log($"[SupportController] Trap armed: card={cardId}, panel={panelFaction}, pattern={placement.gridMask.ToPattern16()}");

            if (effect.triggerImmediatelyIfOccupied &&
                TryTriggerTrap(trap, panelFaction, moveController.GetGrid(panelFaction)) &&
                trap.removeAfterTrigger)
            {
                traps.Remove(trap);
            }
        }
    }

    private void ExecuteGuardEffect(string cardId, SupportEffect effect, Faction self, Faction foe)
    {
        foreach (Faction target in ResolveTargets(effect.target, self, foe))
        {
            guards.Add(new GuardEffect
            {
                cardId = cardId,
                target = target,
                remainingTurns = Mathf.Max(1, effect.guardTurns)
            });

            Debug.Log($"[SupportController] Guard armed: card={cardId}, target={target}, turns={effect.guardTurns}");
        }
    }

    private void TickTurnHpEffects(Faction turnOwner)
    {
        for (int i = turnHpEffects.Count - 1; i >= 0; i--)
        {
            TurnHpEffect effect = turnHpEffects[i];
            if (effect.target != turnOwner) continue;

            ApplyHpChange(effect.target, effect.amount);
            effect.remainingTurns--;
            Debug.Log($"[SupportController] TurnHP tick: card={effect.cardId}, target={effect.target}, amount={effect.amount}, remain={effect.remainingTurns}");

            if (effect.remainingTurns <= 0)
                turnHpEffects.RemoveAt(i);
        }
    }

    private void TickStatModifiers(Faction turnOwner)
    {
        for (int i = statModifiers.Count - 1; i >= 0; i--)
        {
            StatModifier modifier = statModifiers[i];
            if (modifier.target != turnOwner) continue;

            modifier.remainingTurns--;
            if (modifier.remainingTurns <= 0)
            {
                Debug.Log($"[SupportController] Stat expired: card={modifier.cardId}, target={modifier.target}, stat={modifier.statType}, amount={modifier.amount}");
                statModifiers.RemoveAt(i);
            }
        }
    }

    private void TickGuards(Faction turnOwner)
    {
        for (int i = guards.Count - 1; i >= 0; i--)
        {
            GuardEffect guard = guards[i];
            if (guard.target != turnOwner) continue;

            guard.remainingTurns--;
            if (guard.remainingTurns <= 0)
            {
                Debug.Log($"[SupportController] Guard expired: card={guard.cardId}, target={guard.target}");
                guards.RemoveAt(i);
            }
        }
    }

    private void TickTraps(Faction turnOwner)
    {
        for (int i = traps.Count - 1; i >= 0; i--)
        {
            TrapInstance trap = traps[i];
            if (trap.panelFaction != turnOwner) continue;

            trap.remainingTurnStarts--;
            if (trap.remainingTurnStarts <= 0)
            {
                Debug.Log($"[SupportController] Trap expired: card={trap.cardId}, panel={trap.panelFaction}");
                traps.RemoveAt(i);
            }
        }
    }

    private void HandleGridChanged(Faction movedFaction, Vector2Int rc)
    {
        for (int i = traps.Count - 1; i >= 0; i--)
        {
            TrapInstance trap = traps[i];
            if (trap.panelFaction != movedFaction) continue;

            if (TryTriggerTrap(trap, movedFaction, rc) && trap.removeAfterTrigger)
                traps.RemoveAt(i);
        }
    }

    private bool TryTriggerTrap(TrapInstance trap, Faction target, Vector2Int rc)
    {
        if (trap == null || trap.gridMask == null || !trap.gridMask.Contains(rc))
            return false;

        if (trap.damage > 0 && hpController != null)
            hpController.ApplyDamage(target, trap.damage);

        Debug.Log($"[SupportController] Trap triggered: card={trap.cardId}, target={target}, rc=({rc.x},{rc.y}), damage={trap.damage}");
        return true;
    }

    private void ApplyHpChange(Faction target, int amount)
    {
        if (hpController == null || amount == 0) return;

        if (amount > 0) hpController.Heal(target, amount);
        else hpController.ApplyDamage(target, -amount);
    }

    private void AddNextFreeCards(Faction owner, int count)
    {
        int index = ToIndex(owner);
        nextFreeCardCounts[index] += Mathf.Max(1, count);
        Debug.Log($"[SupportController] NextCardFree armed: owner={owner}, count={nextFreeCardCounts[index]}");
    }

    private int DiscardRandomFromHand(Faction side, int count)
    {
        int need = Mathf.Max(1, count);
        int discarded = 0;

        for (int i = 0; i < need; i++)
        {
            int handCount = GetHandCount(side);
            if (handCount <= 0) break;

            int index = Random.Range(0, handCount);
            if (side == Faction.Player)
            {
                if (BattleDeckRuntime.Instance != null && BattleDeckRuntime.Instance.DiscardToBottom(index))
                    discarded++;
            }
            else
            {
                if (EnemyDeckRuntime.Instance != null && EnemyDeckRuntime.Instance.DiscardToBottom(index))
                    discarded++;
            }
        }

        Debug.Log($"[SupportController] DiscardForCost: side={side}, discarded={discarded}/{need}");
        return discarded;
    }

    private int GetHandCount(Faction side)
    {
        if (side == Faction.Player)
            return BattleDeckRuntime.Instance != null ? BattleDeckRuntime.Instance.HandCount : 0;

        return EnemyDeckRuntime.Instance != null ? EnemyDeckRuntime.Instance.GetHandIds().Count : 0;
    }

    private int GetCurrentHP(Faction side)
    {
        if (hpController != null)
            return hpController.GetHP(side);

        if (side == Faction.Player)
            return PlayerDataRuntime.Instance != null && PlayerDataRuntime.Instance.Data != null
                ? PlayerDataRuntime.Instance.Data.currentHP
                : 0;

        return EnemyRuntime.Instance != null ? EnemyRuntime.Instance.currentHP : 0;
    }

    private static int CalculateHpPayment(int currentHp, float percent)
    {
        if (currentHp <= 0 || percent <= 0f)
            return 0;

        return Mathf.Max(1, Mathf.CeilToInt(currentHp * Mathf.Clamp(percent, 0f, 100f) / 100f));
    }

    private static IEnumerable<Faction> ResolveTargets(SupportTarget target, Faction self, Faction foe)
    {
        if (target == SupportTarget.Self || target == SupportTarget.Both)
            yield return self;
        if (target == SupportTarget.Opponent || target == SupportTarget.Both)
            yield return foe;
    }

    private static Faction ResolvePanelFaction(SupportPanelSide side, Faction self, Faction foe)
    {
        return side == SupportPanelSide.SelfPanel ? self : foe;
    }

    private static int ToIndex(Faction faction)
    {
        return faction == Faction.Player ? 0 : 1;
    }
}
