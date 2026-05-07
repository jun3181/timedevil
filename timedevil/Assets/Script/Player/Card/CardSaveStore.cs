// CardSaveStore.cs (수정 버전)
using System.IO;
using UnityEngine;
using Newtonsoft.Json;
using System.Collections.Generic;

public static class CardSaveStore
{
    private static string SavePath =>
        Path.Combine(Application.persistentDataPath, "cards.json");

    public static CardSaveData Load()
    {
        if (!File.Exists(SavePath))
            return CreateDefaultData();

        string json = File.ReadAllText(SavePath);
        var data = JsonConvert.DeserializeObject<CardSaveData>(json);
        return data ?? CreateDefaultData();
    }

    private static CardSaveData CreateDefaultData()
    {
        var owned = new List<string>(13);
        for (int i = 1; i <= 13; i++)
            owned.Add($"Card{i}");

        return new CardSaveData
        {
            owned = owned,
            deck = new List<string>()
        };
    }

    public static void Save(CardSaveData data)
    {
        string json = JsonConvert.SerializeObject(data, Formatting.Indented);
        File.WriteAllText(SavePath, json);
#if UNITY_EDITOR
        Debug.Log($"[CardSaveStore] Saved → {SavePath}\n{json}");
#endif
    }

    public static void DeleteSave()
    {
        if (File.Exists(SavePath))
        {
            File.Delete(SavePath);
#if UNITY_EDITOR
            Debug.Log($"[CardSaveStore] Deleted -> {SavePath}");
#endif
        }
    }

    public static string GetPath() => SavePath;
}
