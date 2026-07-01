using System.Collections.Generic;
using UnityEngine;

public static class InteractableInvoker
{
    public static IInteractable[] GetInteractables(GameObject target)
    {
        if (target == null)
            return System.Array.Empty<IInteractable>();

        IInteractable[] interactables = target.GetComponents<IInteractable>();
        if (interactables.Length == 0)
            interactables = target.GetComponentsInParent<IInteractable>();

        return SortForCooperativeInteraction(interactables);
    }

    public static bool TryInteract(GameObject target)
    {
        IInteractable[] interactables = GetInteractables(target);
        if (interactables.Length == 0)
            return false;

        for (int i = 0; i < interactables.Length; i++)
            interactables[i].Interact();

        return true;
    }

    private static IInteractable[] SortForCooperativeInteraction(IInteractable[] interactables)
    {
        if (interactables == null || interactables.Length <= 1)
            return interactables ?? System.Array.Empty<IInteractable>();

        List<IInteractable> sorted = new(interactables);
        sorted.Sort(CompareInteractionOrder);
        return sorted.ToArray();
    }

    private static int CompareInteractionOrder(IInteractable left, IInteractable right)
    {
        return GetPriority(left).CompareTo(GetPriority(right));
    }

    private static int GetPriority(IInteractable interactable)
    {
        if (interactable is QuestItemInteraction)
            return 0;

        if (interactable is ItemInteraction)
            return 10;

        return 5;
    }
}
