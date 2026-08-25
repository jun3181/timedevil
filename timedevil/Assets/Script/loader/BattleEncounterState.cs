using System;
using System.Collections.Generic;

public static class BattleEncounterState
{
    private static readonly HashSet<string> Consumed = new(StringComparer.Ordinal);

    private static bool _hasPending;
    private static string _pendingSceneName;
    private static string _pendingEncounterKey;

    public static void SetPending(string sceneName, string encounterKey)
    {
        string key = BuildKey(sceneName, encounterKey);
        if (string.IsNullOrWhiteSpace(key))
        {
            ClearPending();
            return;
        }

        _hasPending = true;
        _pendingSceneName = Normalize(sceneName);
        _pendingEncounterKey = encounterKey.Trim();
    }

    public static void ConsumePendingVictory()
    {
        if (!_hasPending)
            return;

        string key = BuildKey(_pendingSceneName, _pendingEncounterKey);
        if (!string.IsNullOrWhiteSpace(key))
            Consumed.Add(key);

        ClearPending();
    }

    public static bool IsConsumed(string sceneName, string encounterKey)
    {
        string key = BuildKey(sceneName, encounterKey);
        return !string.IsNullOrWhiteSpace(key) && Consumed.Contains(key);
    }

    public static void ClearPending()
    {
        _hasPending = false;
        _pendingSceneName = null;
        _pendingEncounterKey = null;
    }

    public static void ClearAll()
    {
        ClearPending();
        Consumed.Clear();
    }

    private static string BuildKey(string sceneName, string encounterKey)
    {
        string scene = Normalize(sceneName);
        string encounter = string.IsNullOrWhiteSpace(encounterKey) ? string.Empty : encounterKey.Trim();
        if (string.IsNullOrWhiteSpace(scene) || string.IsNullOrWhiteSpace(encounter))
            return string.Empty;

        return scene + "::" + encounter;
    }

    private static string Normalize(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName)) return string.Empty;

        string normalized = sceneName.Trim().Replace('\\', '/');
        int slashIndex = normalized.LastIndexOf('/');
        if (slashIndex >= 0 && slashIndex + 1 < normalized.Length)
            normalized = normalized.Substring(slashIndex + 1);

        const string unityExtension = ".unity";
        if (normalized.EndsWith(unityExtension, StringComparison.OrdinalIgnoreCase))
            normalized = normalized.Substring(0, normalized.Length - unityExtension.Length);

        return normalized;
    }
}
