using System;

public static class SceneArrivalContext
{
    private static SceneArrivalRequest _pending;

    public static bool HasPending => _pending != null;

    public static void SetNext(SceneArrivalRequest request)
    {
        _pending = request;
    }

    public static bool TryPeek(out SceneArrivalRequest request)
    {
        request = _pending;
        return request != null;
    }

    public static bool HasPendingForScene(string sceneName)
    {
        return _pending != null && MatchesScene(_pending.targetSceneName, sceneName);
    }

    public static bool TryPeekForScene(string sceneName, out SceneArrivalRequest request)
    {
        request = null;
        if (_pending == null) return false;
        if (!MatchesScene(_pending.targetSceneName, sceneName)) return false;

        request = _pending;
        return true;
    }

    public static bool TryConsumeForScene(string sceneName, out SceneArrivalRequest request)
    {
        if (!TryPeekForScene(sceneName, out request))
            return false;

        _pending = null;
        return true;
    }

    public static void Clear()
    {
        _pending = null;
    }

    public static string NormalizeSceneName(string sceneName)
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

    private static bool MatchesScene(string expectedSceneName, string actualSceneName)
    {
        string expected = NormalizeSceneName(expectedSceneName);
        string actual = NormalizeSceneName(actualSceneName);
        if (string.IsNullOrWhiteSpace(expected) || string.IsNullOrWhiteSpace(actual))
            return false;

        return string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase);
    }
}
