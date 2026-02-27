// Assets/Script/Save/ProgressSaveStore.cs
using System.IO;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public static class ProgressSaveStore
{
    private static string SavePath =>
        Path.Combine(Application.persistentDataPath, "progress.json");

    // Unity Vector 타입을 Newtonsoft로 그대로 직렬화하면
    // Vector3.normalized -> Vector3.normalized ... 자기참조 루프가 발생할 수 있어
    // 저장 포맷을 x/y/z 평면 객체로 고정한다.
    private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
    {
        Formatting = Formatting.Indented,
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
        Converters = { new Vector3JsonConverter() }
    };

    public static ProgressSaveData Load()
    {
        if (!File.Exists(SavePath))
            return new ProgressSaveData();

        string json = File.ReadAllText(SavePath);
        var data = JsonConvert.DeserializeObject<ProgressSaveData>(json, JsonSettings);
        return data ?? new ProgressSaveData();
    }

    public static void Save(ProgressSaveData data)
    {
        if (data == null) data = new ProgressSaveData();
        string json = JsonConvert.SerializeObject(data, JsonSettings);
        File.WriteAllText(SavePath, json);
#if UNITY_EDITOR
        Debug.Log($"[ProgressSaveStore] Saved -> {SavePath}\n{json}");
#endif
    }

    public static void DeleteSave()
    {
        if (File.Exists(SavePath))
        {
            File.Delete(SavePath);
#if UNITY_EDITOR
            Debug.Log($"[ProgressSaveStore] Deleted -> {SavePath}");
#endif
        }
    }

    public static string GetPath() => SavePath;

    private sealed class Vector3JsonConverter : JsonConverter<Vector3>
    {
        public override void WriteJson(JsonWriter writer, Vector3 value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("x");
            writer.WriteValue(value.x);
            writer.WritePropertyName("y");
            writer.WriteValue(value.y);
            writer.WritePropertyName("z");
            writer.WriteValue(value.z);
            writer.WriteEndObject();
        }

        public override Vector3 ReadJson(
            JsonReader reader,
            System.Type objectType,
            Vector3 existingValue,
            bool hasExistingValue,
            JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return Vector3.zero;

            var obj = JObject.Load(reader);
            float x = obj.Value<float?>("x") ?? 0f;
            float y = obj.Value<float?>("y") ?? 0f;
            float z = obj.Value<float?>("z") ?? 0f;
            return new Vector3(x, y, z);
        }
    }
}
