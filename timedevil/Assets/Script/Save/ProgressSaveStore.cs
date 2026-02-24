// Assets/Script/Save/ProgressSaveStore.cs
using System.IO;
using UnityEngine;
using Newtonsoft.Json;

public static class ProgressSaveStore
{
    private static string SavePath =>
        Path.Combine(Application.persistentDataPath, "progress.json");

    public static ProgressSaveData Load()
    {
        if (!File.Exists(SavePath))
            return new ProgressSaveData();

        string json = File.ReadAllText(SavePath);
        var data = JsonConvert.DeserializeObject<ProgressSaveData>(json);
        return data ?? new ProgressSaveData();
    }

    public static void Save(ProgressSaveData data)
    {
        if (data == null) data = new ProgressSaveData();
        string json = JsonConvert.SerializeObject(data, Formatting.Indented);
        File.WriteAllText(SavePath, json);
#if UNITY_EDITOR
        Debug.Log($"[ProgressSaveStore] Saved ¡æ {SavePath}\n{json}");
#endif
    }

    public static string GetPath() => SavePath;
}