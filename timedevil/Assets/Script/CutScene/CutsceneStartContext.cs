// Assets/Script/CutScene/CutsceneStartContext.cs
using System;

public static class CutsceneStartContext
{
    private static bool _hasPending;
    private static string _targetSceneName;
    private static string _startKey;

    public static void SetNext(string targetSceneName, string startKey)
    {
        _targetSceneName = targetSceneName;
        _startKey = startKey;
        _hasPending =
            !string.IsNullOrWhiteSpace(_targetSceneName) &&
            !string.IsNullOrWhiteSpace(_startKey);
    }

    public static bool TryConsume(string activeSceneName, out string startKey)
    {
        startKey = null;

        if (!_hasPending) return false;
        if (string.IsNullOrWhiteSpace(activeSceneName)) return false;
        if (!string.Equals(_targetSceneName, activeSceneName, StringComparison.Ordinal)) return false;

        startKey = _startKey;
        Clear();
        return true;
    }

    public static void Clear()
    {
        _hasPending = false;
        _targetSceneName = null;
        _startKey = null;
    }
}

public static class SceneStartCameraContext
{
    private static bool _hasPending;
    private static string _targetSceneName;
    private static float _orthoSize;

    public static void SetNext(string targetSceneName, float orthoSize)
    {
        _targetSceneName = targetSceneName;
        _orthoSize = orthoSize;
        _hasPending = !string.IsNullOrWhiteSpace(_targetSceneName);
    }

    public static bool TryConsume(string activeSceneName, out float orthoSize)
    {
        orthoSize = 0f;

        if (!_hasPending) return false;
        if (string.IsNullOrWhiteSpace(activeSceneName)) return false;
        if (!string.Equals(_targetSceneName, activeSceneName, StringComparison.Ordinal)) return false;

        orthoSize = _orthoSize;
        Clear();
        return true;
    }

    public static void Clear()
    {
        _hasPending = false;
        _targetSceneName = null;
        _orthoSize = 0f;
    }
}
