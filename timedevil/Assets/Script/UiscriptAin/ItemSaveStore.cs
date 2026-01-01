using System.IO;
using UnityEngine;

/// <summary>
/// JSON 세이브/로드를 담당하는 유틸리티
/// </summary>
public static class ItemSaveStore
{
    /// <summary>
    /// 세이브 파일 존재 여부 확인
    /// </summary>
    public static bool HasSave(string fileName)
    {
        string path = GetPath(fileName);
        return File.Exists(path);
    }

    /// <summary>
    /// 인벤토리를 JSON 파일로 저장
    /// </summary>
    public static void Save(ItemSave data, string fileName)
    {
        if (data == null)
        {
            Debug.LogError("❌ 저장하려는 ItemSave 데이터가 null 입니다.");
            return;
        }

        string json = JsonUtility.ToJson(data, true); // pretty print
        string path = GetPath(fileName);

        try
        {
            File.WriteAllText(path, json);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"❌ 세이브 파일 쓰기 실패: {ex.Message}");
        }
    }

    /// <summary>
    /// JSON 파일에서 인벤토리 데이터를 로드
    /// </summary>
    public static ItemSave Load(string fileName)
    {
        string path = GetPath(fileName);
        if (!File.Exists(path))
        {
            Debug.LogWarning("⚠️ 세이브 파일이 존재하지 않습니다.");
            return null;
        }

        try
        {
            string json = File.ReadAllText(path);
            return JsonUtility.FromJson<ItemSave>(json);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"❌ 세이브 파일 읽기/파싱 실패: {ex.Message}");
            return null;
        }
    }

    private static string GetPath(string fileName)
    {
        return Path.Combine(Application.persistentDataPath, fileName);
    }
}
