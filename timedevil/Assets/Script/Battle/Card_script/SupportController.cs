using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SupportController : MonoBehaviour
{
    [SerializeField] private MoveController moveController;
    [SerializeField] private HPController hpController;
    [SerializeField] private CostController costController;

    private readonly List<PositionDebugZone> positionDebugZones = new();
    private readonly List<TurnEffect> turnEffects = new();

    private enum TurnEffectKind { Damage, Heal, AntiCost }

    private class PositionDebugZone
    {
        public string cardId;
        public string cardName;
        public Faction caster;
        public Faction foe;
        public SupportPositionTarget target;
        public string pattern16;
        public bool oneShot;
    }

    private class TurnEffect
    {
        public string cardId;
        public TurnEffectKind kind;
        public Faction target;
        public int amount;
        public int remainingTurns;
    }

    private void Awake()
    {
        if (!moveController) moveController = FindObjectOfType<MoveController>(true);
        if (!hpController) hpController = FindObjectOfType<HPController>(true);
        if (!costController) costController = FindObjectOfType<CostController>(true);
    }

    private void OnEnable()
    {
        if (!moveController) moveController = FindObjectOfType<MoveController>(true);
        if (moveController != null) moveController.OnGridChanged += HandleGridChanged;
    }

    private void OnDisable()
    {
        if (moveController != null) moveController.OnGridChanged -= HandleGridChanged;
    }

    public IEnumerator Execute(SupportCardSO so, Faction self, Faction foe)
    {
        Debug.Log($"[SupportController] Execute: id={so.id}, name={so.displayName}, action={so.action}, self={self}, foe={foe}");

        if (so.enablePositionDebug)
            RegisterPositionDebug(so, self, foe);

        RegisterTurnEffect(so, self, foe);

        // TODO: 버프/디버프 지속/수치 적용
        yield return null;
    }

    public void OnTurnStarted(Faction turnOwner)
    {
        if (!hpController) hpController = FindObjectOfType<HPController>(true);
        if (!costController) costController = FindObjectOfType<CostController>(true);

        for (int i = turnEffects.Count - 1; i >= 0; i--)
        {
            var effect = turnEffects[i];
            if (effect.target != turnOwner) continue;

            switch (effect.kind)
            {
                case TurnEffectKind.Damage:
                    if (hpController != null) hpController.ApplyDamage(effect.target, effect.amount);
                    Debug.Log($"[SupportController] TickDamage: card={effect.cardId}, target={effect.target}, amount={effect.amount}, remainBefore={effect.remainingTurns}");
                    break;
                case TurnEffectKind.Heal:
                    if (hpController != null) hpController.Heal(effect.target, effect.amount);
                    Debug.Log($"[SupportController] TickHeal: card={effect.cardId}, target={effect.target}, amount={effect.amount}, remainBefore={effect.remainingTurns}");
                    break;
                case TurnEffectKind.AntiCost:
                    int reduced = costController != null ? costController.ReduceCurrent(effect.amount) : 0;
                    Debug.Log($"[SupportController] AntiCost: card={effect.cardId}, target={effect.target}, amount={effect.amount}, reduced={reduced}, remainBefore={effect.remainingTurns}");
                    break;
            }

            effect.remainingTurns--;
            if (effect.remainingTurns <= 0)
                turnEffects.RemoveAt(i);
        }
    }

    private void RegisterTurnEffect(SupportCardSO so, Faction self, Faction foe)
    {
        if (so.action == SupportAction.Debuff && so.debuffStat == StatKind.HP && so.debuffAmount > 0 && so.debuffTurn > 0)
        {
            AddTurnEffect(so.id, TurnEffectKind.Damage, foe, so.debuffAmount, so.debuffTurn);
            return;
        }

        if (so.action == SupportAction.Buff && so.buffStat == StatKind.HP && so.buffAmount > 0 && so.buffTurn > 0)
        {
            AddTurnEffect(so.id, TurnEffectKind.Heal, self, so.buffAmount, so.buffTurn);
            return;
        }

        if (so.action == SupportAction.AntiCost && so.antiCostAmount > 0 && so.antiCostTurn > 0)
        {
            AddTurnEffect(so.id, TurnEffectKind.AntiCost, foe, so.antiCostAmount, so.antiCostTurn);
        }
    }

    private void AddTurnEffect(string cardId, TurnEffectKind kind, Faction target, int amount, int turns)
    {
        var effect = new TurnEffect
        {
            cardId = cardId,
            kind = kind,
            target = target,
            amount = Mathf.Max(0, amount),
            remainingTurns = Mathf.Max(0, turns)
        };

        turnEffects.Add(effect);
        Debug.Log($"[SupportController] TurnEffect registered: card={cardId}, kind={kind}, target={target}, amount={effect.amount}, turns={effect.remainingTurns}");
    }

    private void RegisterPositionDebug(SupportCardSO so, Faction self, Faction foe)
    {
        if (moveController == null)
        {
            Debug.LogWarning($"[SupportController] PositionDebug skipped. MoveController is null. card={so.id}");
            return;
        }

        var zone = new PositionDebugZone
        {
            cardId = so.id,
            cardName = string.IsNullOrEmpty(so.displayName) ? so.id : so.displayName,
            caster = self,
            foe = foe,
            target = so.positionTarget,
            pattern16 = string.IsNullOrEmpty(so.positionPattern16) ? "0000000000000000" : so.positionPattern16,
            oneShot = so.positionDebugOneShot
        };

        positionDebugZones.Add(zone);
        Debug.Log($"[SupportController] PositionDebug registered: card={zone.cardId}, target={zone.target}, pattern={zone.pattern16}");

        CheckZone(zone, Faction.Player, moveController.GetGrid(Faction.Player), removeOnTrigger: false);
        CheckZone(zone, Faction.Enemy, moveController.GetGrid(Faction.Enemy), removeOnTrigger: false);
    }

    private void HandleGridChanged(Faction movedFaction, Vector2Int rc)
    {
        for (int i = positionDebugZones.Count - 1; i >= 0; i--)
        {
            var zone = positionDebugZones[i];
            if (CheckZone(zone, movedFaction, rc, removeOnTrigger: true))
                positionDebugZones.RemoveAt(i);
        }
    }

    private bool CheckZone(PositionDebugZone zone, Faction movedFaction, Vector2Int rc, bool removeOnTrigger)
    {
        if (!MatchesTarget(zone, movedFaction)) return false;
        if (!PatternContains(zone.pattern16, rc)) return false;

        Debug.Log($"[SupportController] PositionDebug hit: card={zone.cardId}, target={movedFaction}, rc=({rc.x},{rc.y})");
        return removeOnTrigger && zone.oneShot;
    }

    private static bool MatchesTarget(PositionDebugZone zone, Faction movedFaction)
    {
        return zone.target switch
        {
            SupportPositionTarget.Any => true,
            SupportPositionTarget.Player => movedFaction == Faction.Player,
            SupportPositionTarget.Enemy => movedFaction == Faction.Enemy,
            SupportPositionTarget.Self => movedFaction == zone.caster,
            SupportPositionTarget.Foe => movedFaction == zone.foe,
            _ => false
        };
    }

    private static bool PatternContains(string pattern16, Vector2Int rc)
    {
        if (rc.x < 1 || rc.x > 4 || rc.y < 1 || rc.y > 4) return false;
        int index = (rc.x - 1) * 4 + (rc.y - 1);
        char ch = (pattern16 != null && pattern16.Length > index) ? pattern16[index] : '0';
        return ch == '1';
    }
}
