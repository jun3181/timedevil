using System;
using UnityEngine;

public sealed class BattleVictoryRouteRequest
{
    public string targetSceneName;
    public string routeKey;
    public string routerTransformPath;
    public string enemyId;
    public string sourceObjectName;

    public BattleVictoryRouteRequest Clone()
    {
        return new BattleVictoryRouteRequest
        {
            targetSceneName = targetSceneName,
            routeKey = routeKey,
            routerTransformPath = routerTransformPath,
            enemyId = enemyId,
            sourceObjectName = sourceObjectName
        };
    }
}

public static class BattleVictoryReturnContext
{
    private static BattleVictoryRouteRequest _armed;
    private static BattleVictoryRouteRequest _pending;

    public static bool HasArmed => _armed != null;
    public static bool HasPending => _pending != null;

    public static void Arm(
        string targetSceneName,
        string routeKey,
        string routerTransformPath,
        string enemyId,
        string sourceObjectName)
    {
        ClearArmed();

        if (string.IsNullOrWhiteSpace(targetSceneName) ||
            string.IsNullOrWhiteSpace(routeKey) ||
            string.IsNullOrWhiteSpace(routerTransformPath))
            return;

        _armed = new BattleVictoryRouteRequest
        {
            targetSceneName = SceneArrivalContext.NormalizeSceneName(targetSceneName),
            routeKey = routeKey.Trim(),
            routerTransformPath = routerTransformPath.Trim(),
            enemyId = string.IsNullOrWhiteSpace(enemyId) ? null : enemyId.Trim(),
            sourceObjectName = string.IsNullOrWhiteSpace(sourceObjectName) ? null : sourceObjectName.Trim()
        };
    }

    public static bool QueueArmedVictory()
    {
        if (_armed == null)
            return false;

        _pending = _armed.Clone();
        _armed = null;
        return true;
    }

    public static bool TryPeekForScene(string sceneName, out BattleVictoryRouteRequest request)
    {
        request = null;
        if (_pending == null)
            return false;

        if (!MatchesScene(_pending.targetSceneName, sceneName))
            return false;

        request = _pending.Clone();
        return true;
    }

    public static bool TryConsumeForScene(string sceneName, out BattleVictoryRouteRequest request)
    {
        if (!TryPeekForScene(sceneName, out request))
            return false;

        _pending = null;
        return true;
    }

    public static void ClearArmed()
    {
        _armed = null;
    }

    public static void ClearPending()
    {
        _pending = null;
    }

    public static void ClearAll()
    {
        ClearArmed();
        ClearPending();
    }

    public static string GetTransformPath(Transform transform)
    {
        if (transform == null)
            return null;

        var stack = new System.Collections.Generic.Stack<string>();
        Transform current = transform;
        while (current != null)
        {
            stack.Push(current.name);
            current = current.parent;
        }

        return string.Join("/", stack.ToArray());
    }

    private static bool MatchesScene(string expectedSceneName, string actualSceneName)
    {
        string expected = SceneArrivalContext.NormalizeSceneName(expectedSceneName);
        string actual = SceneArrivalContext.NormalizeSceneName(actualSceneName);

        if (string.IsNullOrWhiteSpace(expected) || string.IsNullOrWhiteSpace(actual))
            return false;

        return string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase);
    }
}
