// PlayerDataStore.cs
using System.IO;
using UnityEngine;

public static class PlayerDataStore
{
    static string Path => System.IO.Path.Combine(Application.persistentDataPath, "player.json");

    public static void Save(PlayerData data)
    {
        if (data == null) return;
        var json = JsonUtility.ToJson(data, true);
        File.WriteAllText(Path, json);
#if UNITY_EDITOR
        Debug.Log($"[PlayerDataStore] Saved ¡æ {Path}");
#endif
    }

    public static PlayerData Load()
    {
        if (!File.Exists(Path)) return null;
        var json = File.ReadAllText(Path);
        var data = JsonUtility.FromJson<PlayerData>(json);
#if UNITY_EDITOR
        Debug.Log($"[PlayerDataStore] Loaded ¡ç {Path}");
#endif
        return data;
    }
}
