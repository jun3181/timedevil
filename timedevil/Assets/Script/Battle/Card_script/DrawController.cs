// DrawController.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DrawController : MonoBehaviour
{
    public const string NotEnoughDiscardMessage = "버릴 패가 부족합니다";

    [Header("Optional VFX (Draw only, once)")]
    [SerializeField] private GameObject upDrawParticlePrefab;
    [SerializeField] private float vfxLifetime = 1.2f;

    [Header("Anchors (where to spawn VFX)")]
    [SerializeField] private Transform playerHandAnchor;
    [SerializeField] private Transform enemyHandAnchor;

    [Header("Anime & UI Refs")]
    [SerializeField] private CardAnimeController cardAnime;
    [SerializeField] private BattleMenuController menu;
    [SerializeField] private DescriptionPanelController desc;
    [SerializeField] private HandUI playerHandUI;
    [SerializeField] private EnemyHandUI enemyHandUI;

    public bool CanExecute(DrawCardSO so, Faction self, int selfCardsAlreadyCommitted, out string failMessage)
    {
        failMessage = null;
        if (so == null) return true;

        int selfHand = Mathf.Max(0, GetHandCount(self) - Mathf.Max(0, selfCardsAlreadyCommitted));
        int opponentHand = GetHandCount(OpponentOf(self));
        int selfDeck = GetDeckCount(self) + Mathf.Max(0, selfCardsAlreadyCommitted);
        int opponentDeck = GetDeckCount(OpponentOf(self));

        foreach (var step in BuildSteps(so))
        {
            if (step == null || step.amount <= 0) continue;

            switch (step.effectType)
            {
                case DrawHandEffectType.SelfDiscard:
                    if (selfHand < step.amount)
                    {
                        failMessage = NotEnoughDiscardMessage;
                        return false;
                    }
                    selfHand -= step.amount;
                    selfDeck += step.amount;
                    break;

                case DrawHandEffectType.OpponentDiscard:
                    if (opponentHand < step.amount)
                    {
                        failMessage = NotEnoughDiscardMessage;
                        return false;
                    }
                    opponentHand -= step.amount;
                    opponentDeck += step.amount;
                    break;

                case DrawHandEffectType.SelfDraw:
                {
                    int drawn = Mathf.Min(step.amount, selfDeck);
                    selfDeck -= drawn;
                    selfHand += drawn;
                    break;
                }

                case DrawHandEffectType.OpponentDraw:
                {
                    int drawn = Mathf.Min(step.amount, opponentDeck);
                    opponentDeck -= drawn;
                    opponentHand += drawn;
                    break;
                }
            }
        }

        return true;
    }

    public IEnumerator Execute(DrawCardSO so, Faction self)
    {
        if (so == null) yield break;

        if (!CanExecute(so, self, 0, out string failMessage))
        {
            if (desc) desc.ShowOneShotMessage(failMessage);
            yield break;
        }

        var steps = BuildSteps(so);
        foreach (var step in steps)
        {
            if (step == null || step.amount <= 0) continue;

            switch (step.effectType)
            {
                case DrawHandEffectType.SelfDiscard:
                    yield return ExecuteDiscard(self, step.amount);
                    break;
                case DrawHandEffectType.SelfDraw:
                    yield return ExecuteDraw(self, step.amount);
                    break;
                case DrawHandEffectType.OpponentDiscard:
                    yield return ExecuteDiscard(OpponentOf(self), step.amount);
                    break;
                case DrawHandEffectType.OpponentDraw:
                    yield return ExecuteDraw(OpponentOf(self), step.amount);
                    break;
            }
        }
    }

    private List<DrawHandEffectStep> BuildSteps(DrawCardSO so)
    {
        var steps = new List<DrawHandEffectStep>();
        if (so == null) return steps;

        switch (so.drawMode)
        {
            case DrawMode.UpDraw:
                steps.Add(new DrawHandEffectStep
                {
                    effectType = DrawHandEffectType.SelfDraw,
                    amount = Mathf.Max(0, so.amount)
                });
                break;

            case DrawMode.AntiDraw:
                steps.Add(new DrawHandEffectStep
                {
                    effectType = DrawHandEffectType.OpponentDiscard,
                    amount = Mathf.Max(0, so.amount)
                });
                break;

            case DrawMode.HandRefresh:
                steps.Add(new DrawHandEffectStep
                {
                    effectType = DrawHandEffectType.SelfDiscard,
                    amount = Mathf.Max(0, so.refreshDiscardAmount)
                });
                steps.Add(new DrawHandEffectStep
                {
                    effectType = DrawHandEffectType.SelfDraw,
                    amount = Mathf.Max(0, so.refreshDrawAmount)
                });
                break;

            case DrawMode.HandEffectSequence:
                if (so.handEffectSequence != null)
                {
                    foreach (var step in so.handEffectSequence)
                    {
                        if (step == null) continue;
                        steps.Add(new DrawHandEffectStep
                        {
                            effectType = step.effectType,
                            amount = Mathf.Max(0, step.amount)
                        });
                    }
                }
                break;
        }

        return steps;
    }

    private IEnumerator ExecuteDraw(Faction side, int amount)
    {
        int drawN = Mathf.Max(0, amount);
        if (drawN <= 0) yield break;

        ShowHandForEffect(side, $"{SideLabel(side)} 손패를 {drawN}장 뽑습니다...");
        yield return new WaitForEndOfFrame();

        int actuallyDrawn = 0;
        if (side == Faction.Player)
        {
            var deck = BattleDeckRuntime.Instance;
            if (deck != null) actuallyDrawn = deck.Draw(drawN, ignoreHandCap: true);
            else Debug.LogWarning("[DrawController] BattleDeckRuntime is null (draw).");
        }
        else
        {
            var enemy = EnemyDeckRuntime.Instance;
            if (enemy != null) actuallyDrawn = enemy.Draw(drawN, ignoreHandCap: true);
            else Debug.LogWarning("[DrawController] EnemyDeckRuntime is null (draw).");
        }

        SpawnDrawVfx(side);

        if (actuallyDrawn > 0 && cardAnime != null)
        {
            yield return new WaitForEndOfFrame();
            yield return cardAnime.RevealLastNCards(side, actuallyDrawn);
        }

        RestoreHandAfterEffect(side);
    }

    private IEnumerator ExecuteDiscard(Faction side, int amount)
    {
        int discardN = Mathf.Max(0, amount);
        if (discardN <= 0) yield break;

        ShowHandForEffect(side, $"{SideLabel(side)} 손패에서 무작위 카드를 버립니다...");
        yield return new WaitForEndOfFrame();

        for (int t = 0; t < discardN; t++)
        {
            int countNow = GetHandCount(side);
            if (countNow <= 0) break;

            int idx = Random.Range(0, countNow);
            if (cardAnime != null)
            {
                yield return cardAnime.DiscardOneAtIndex(
                    side,
                    idx,
                    afterAnimDataOp: () => DiscardToBottomAt(side, idx)
                );
            }
            else
            {
                DiscardToBottomAt(side, idx);
                RebuildHandUI(side);
                yield return null;
            }
        }

        RestoreHandAfterEffect(side);
    }

    private void ShowHandForEffect(Faction side, string message)
    {
        if (menu) menu.EnableInput(false);

        if (desc) desc.EnterSpectate(side, message);
        else
        {
            if (side == Faction.Player)
            {
                if (playerHandUI) playerHandUI.ShowCards();
                if (enemyHandUI) enemyHandUI.HideAll();
            }
            else
            {
                if (enemyHandUI) { enemyHandUI.gameObject.SetActive(true); enemyHandUI.ShowAll(); }
                if (playerHandUI) playerHandUI.HideCards();
            }
        }
    }

    private void RestoreHandAfterEffect(Faction side)
    {
        if (desc) desc.ExitSpectate();
        else
        {
            if (side == Faction.Player)
            {
                if (playerHandUI) playerHandUI.HideCards();
                if (enemyHandUI) { enemyHandUI.gameObject.SetActive(true); enemyHandUI.ShowAll(); }
            }
            else
            {
                if (enemyHandUI) enemyHandUI.HideAll();
                if (playerHandUI) playerHandUI.ShowCards();
            }
        }
    }

    private int GetHandCount(Faction side)
    {
        if (side == Faction.Player) return BattleDeckRuntime.Instance ? BattleDeckRuntime.Instance.HandCount : 0;
        return EnemyDeckRuntime.Instance ? EnemyDeckRuntime.Instance.GetHandIds().Count : 0;
    }

    private int GetDeckCount(Faction side)
    {
        if (side == Faction.Player) return BattleDeckRuntime.Instance ? BattleDeckRuntime.Instance.deck.Count : 0;
        return EnemyDeckRuntime.Instance ? EnemyDeckRuntime.Instance.deck.Count : 0;
    }

    private void DiscardToBottomAt(Faction side, int idx)
    {
        if (side == Faction.Player)
        {
            var rt = BattleDeckRuntime.Instance;
            if (rt != null) rt.DiscardToBottom(idx);
        }
        else
        {
            var rt = EnemyDeckRuntime.Instance;
            if (rt != null) rt.DiscardToBottom(idx);
        }
    }

    private void RebuildHandUI(Faction side)
    {
        if (side == Faction.Player) playerHandUI?.RebuildFromHand();
        else enemyHandUI?.RebuildFromHand();
    }

    private void SpawnDrawVfx(Faction side)
    {
        if (upDrawParticlePrefab == null) return;

        Transform anchor = side == Faction.Player ? playerHandAnchor : enemyHandAnchor;
        Vector3 pos = anchor ? anchor.position : Vector3.zero;
        var go = Instantiate(upDrawParticlePrefab, pos, Quaternion.identity);
        if (vfxLifetime > 0f) Destroy(go, vfxLifetime);
    }

    private static Faction OpponentOf(Faction side)
    {
        return side == Faction.Player ? Faction.Enemy : Faction.Player;
    }

    private static string SideLabel(Faction side)
    {
        return side == Faction.Player ? "내" : "상대";
    }

    public void SetAnchors(Transform player, Transform enemy)
    {
        playerHandAnchor = player;
        enemyHandAnchor = enemy;
    }
}
