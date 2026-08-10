using UnityEngine;

public static class HandPositionUtility
{
    public static Vector2 ToSeparatedRootLocal(RectTransform target, Vector2 desiredAnchoredPosition, bool enabled, string rootName)
    {
        if (!enabled || !target || string.IsNullOrEmpty(rootName))
            return desiredAnchoredPosition;

        RectTransform root = FindNamedAncestor(target.transform, rootName);
        if (!root || root == target)
            return desiredAnchoredPosition;

        Vector2 ancestorOffset = Vector2.zero;
        Transform cursor = target.transform.parent;
        while (cursor != null)
        {
            if (cursor is RectTransform rect)
                ancestorOffset += rect.anchoredPosition;

            if (cursor == root.transform)
                break;

            cursor = cursor.parent;
        }

        return desiredAnchoredPosition - ancestorOffset;
    }

    public static RectTransform FindNamedAncestor(Transform target, string rootName)
    {
        if (!target || string.IsNullOrEmpty(rootName))
            return null;

        Transform cursor = target;
        while (cursor != null)
        {
            if (IsNamedRoot(cursor, rootName) && cursor is RectTransform rect)
                return rect;

            cursor = cursor.parent;
        }

        return null;
    }

    public static bool IsNamedRoot(Transform target, string rootName)
    {
        return target
            && !string.IsNullOrEmpty(rootName)
            && string.Equals(target.name.Trim(), rootName.Trim(), System.StringComparison.OrdinalIgnoreCase);
    }
}
