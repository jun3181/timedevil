// CardSaveStore.cs (수정 버전)
using System.IO;
using UnityEngine;
using Newtonsoft.Json;

public static class CardSaveStore
{
    private static string SavePath =>
        Path.Combine(Application.persistentDataPath, "cards.json");

    public static CardSaveData Load()
    {
        // ❌ 기본 카드 자동 생성 제거
        if (!File.Exists(SavePath))
            return new CardSaveData();  // 비어 있는 상태 반환

        string json = File.ReadAllText(SavePath);
        var data = JsonConvert.DeserializeObject<CardSaveData>(json);
        return data ?? new CardSaveData();
    }

    public static void Save(CardSaveData data)
    {
        string json = JsonConvert.SerializeObject(data, Formatting.Indented);
        File.WriteAllText(SavePath, json);
#if UNITY_EDITOR
        Debug.Log($"[CardSaveStore] Saved → {SavePath}\n{json}");
#endif
    }

    public static string GetPath() => SavePath;
}
