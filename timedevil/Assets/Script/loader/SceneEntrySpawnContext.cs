using System;

public static class SceneEntrySpawnContext
{
    private static string _targetSceneName;
    private static string _spawnKey;

    public static void SetNext(string targetSceneName, string spawnKey)
    {
        _targetSceneName = Normalize(targetSceneName);
        _spawnKey = string.IsNullOrWhiteSpace(spawnKey) ? null : spawnKey.Trim();
    }

    public static bool Matches(string sceneName, string spawnKey)
    {
        if (string.IsNullOrWhiteSpace(_targetSceneName)) return false;
        if (string.IsNullOrWhiteSpace(_spawnKey)) return false;
        if (string.IsNullOrWhiteSpace(sceneName)) return false;
        if (string.IsNullOrWhiteSpace(spawnKey)) return false;

        return string.Equals(_targetSceneName, Normalize(sceneName), StringComparison.OrdinalIgnoreCase) &&
               string.Equals(_spawnKey, spawnKey.Trim(), StringComparison.Ordinal);
    }

    public static bool TryConsume(string sceneName, string spawnKey)
    {
        if (!Matches(sceneName, spawnKey))
            return false;

        Clear();
        return true;
    }

    public static void Clear()
    {
        _targetSceneName = null;
        _spawnKey = null;
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
