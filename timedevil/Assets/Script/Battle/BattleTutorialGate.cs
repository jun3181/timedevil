using System;
using UnityEngine;

public enum BattleTutorialAdvanceMode
{
    PressE,
    WaitAction
}

public enum BattleTutorialAction
{
    None,
    Continue,
    MenuNavigate,
    CardPanelInteract,
    ItemPanelInteract,
    StatePanelInteract,
    EndPanelInteract,
    RunPanelInteract,
    CardSelect,
    CardSelectionMove,
    CardUse,
    CardDiscard,
    CardCancel,
    TurnEnd,
    StateTargetMove,
    StateHandInspect,
    StateHandCardMove,
    StateCancel,
    ItemCancel,
    PlayerEvade
}

public static class BattleTutorialGate
{
    public static event Action<BattleTutorialAction> OnActionReported;

    public static bool IsActive { get; private set; }
    public static BattleTutorialAdvanceMode Mode { get; private set; }
    public static BattleTutorialAction RequiredAction { get; private set; }
    public static int LastInputConsumedFrame { get; private set; } = -1;
    public static BattleTutorialAction LastInputConsumedAction { get; private set; } = BattleTutorialAction.None;
    public static bool WasInputConsumedThisFrame => LastInputConsumedFrame == Time.frameCount;

    private static bool allowMenuNavigation;
    private static bool allowCardSelectionNavigation;
    private static bool allowStateNavigation;
    private static bool allowCancel;

    public static void OpenPressE()
    {
        IsActive = true;
        Mode = BattleTutorialAdvanceMode.PressE;
        RequiredAction = BattleTutorialAction.Continue;
        allowMenuNavigation = false;
        allowCardSelectionNavigation = false;
        allowStateNavigation = false;
        allowCancel = false;
    }

    public static void OpenWaitAction(
        BattleTutorialAction requiredAction,
        bool allowMenuNav,
        bool allowCardNav,
        bool allowStateNav,
        bool allowCancelInput)
    {
        IsActive = true;
        Mode = BattleTutorialAdvanceMode.WaitAction;
        RequiredAction = requiredAction;
        allowMenuNavigation = allowMenuNav;
        allowCardSelectionNavigation = allowCardNav;
        allowStateNavigation = allowStateNav;
        allowCancel = allowCancelInput;
    }

    public static void Close()
    {
        IsActive = false;
        Mode = BattleTutorialAdvanceMode.PressE;
        RequiredAction = BattleTutorialAction.None;
        allowMenuNavigation = false;
        allowCardSelectionNavigation = false;
        allowStateNavigation = false;
        allowCancel = false;
    }

    public static void MarkInputConsumedThisFrame(BattleTutorialAction action = BattleTutorialAction.None)
    {
        LastInputConsumedFrame = Time.frameCount;
        LastInputConsumedAction = action;
    }

    public static bool Allows(BattleTutorialAction action)
    {
        if (WasInputConsumedThisFrame && action != LastInputConsumedAction)
            return false;

        if (!IsActive)
            return true;

        if (action == BattleTutorialAction.None)
            return false;

        if (Mode == BattleTutorialAdvanceMode.PressE)
            return action == BattleTutorialAction.Continue;

        if (action == RequiredAction)
            return true;

        if (allowMenuNavigation && action == BattleTutorialAction.MenuNavigate)
            return true;

        if (allowCardSelectionNavigation && action == BattleTutorialAction.CardSelectionMove)
            return true;

        if (allowStateNavigation && (action == BattleTutorialAction.StateTargetMove || action == BattleTutorialAction.StateHandCardMove))
            return true;

        if (allowCancel && (action == BattleTutorialAction.CardCancel
            || action == BattleTutorialAction.StateCancel
            || action == BattleTutorialAction.ItemCancel))
            return true;

        return RequiredAction switch
        {
            BattleTutorialAction.CardSelect => action == BattleTutorialAction.CardPanelInteract,
            BattleTutorialAction.CardUse => action == BattleTutorialAction.CardPanelInteract
                || action == BattleTutorialAction.CardSelect
                || (allowCardSelectionNavigation && action == BattleTutorialAction.CardSelectionMove),
            BattleTutorialAction.TurnEnd => action == BattleTutorialAction.EndPanelInteract
                || action == BattleTutorialAction.CardDiscard
                || (allowCardSelectionNavigation && action == BattleTutorialAction.CardSelectionMove),
            BattleTutorialAction.StateHandInspect => action == BattleTutorialAction.StatePanelInteract
                || (allowStateNavigation && action == BattleTutorialAction.StateTargetMove),
            _ => false,
        };
    }

    public static void Report(BattleTutorialAction action)
    {
        if (!IsActive)
            return;

        OnActionReported?.Invoke(action);
    }
}
