using System;
using System.Collections.Generic;

public static class StorySignalContext
{
    private static readonly Dictionary<string, HashSet<string>> SignalsByScene = new();

    public static void SetNext(string targetSceneName, string signalKey)
    {
        if (string.IsNullOrWhiteSpace(targetSceneName)) return;
        if (string.IsNullOrWhiteSpace(signalKey)) return;

        if (!SignalsByScene.TryGetValue(targetSceneName, out var signals))
        {
            signals = new HashSet<string>(StringComparer.Ordinal);
            SignalsByScene[targetSceneName] = signals;
        }

        signals.Add(signalKey);
    }

    public static bool Has(string activeSceneName, string signalKey)
    {
        if (string.IsNullOrWhiteSpace(activeSceneName)) return false;
        if (string.IsNullOrWhiteSpace(signalKey)) return false;

        return SignalsByScene.TryGetValue(activeSceneName, out var signals) &&
               signals.Contains(signalKey);
    }

    public static bool TryConsume(string activeSceneName, string signalKey)
    {
        if (!Has(activeSceneName, signalKey)) return false;

        var signals = SignalsByScene[activeSceneName];
        signals.Remove(signalKey);

        if (signals.Count == 0)
            SignalsByScene.Remove(activeSceneName);

        return true;
    }

    public static void Clear(string targetSceneName = null, string signalKey = null)
    {
        if (string.IsNullOrWhiteSpace(targetSceneName))
        {
            SignalsByScene.Clear();
            return;
        }

        if (!SignalsByScene.TryGetValue(targetSceneName, out var signals))
            return;

        if (string.IsNullOrWhiteSpace(signalKey))
        {
            SignalsByScene.Remove(targetSceneName);
            return;
        }

        signals.Remove(signalKey);
        if (signals.Count == 0)
            SignalsByScene.Remove(targetSceneName);
    }
}
