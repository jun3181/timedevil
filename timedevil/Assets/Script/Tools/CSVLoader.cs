using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;

public static class CsvLoader
{
    // ---------- Public API ----------

    public static List<T> LoadCsv<T>(string fullPath) where T : new()
    {
        if (!File.Exists(fullPath))
        {
            Debug.LogWarning($"CSV file not found: {fullPath}");
            return new List<T>();
        }
        string csvText = File.ReadAllText(fullPath);
        return ParseCsvText<T>(csvText);
    }

    public static List<T> ParseCsvText<T>(string csvText) where T : new()
    {
        if (string.IsNullOrEmpty(csvText))
            return new List<T>();

        // 라인 분해(빈 줄 허용), BOM 제거
        var raw = csvText.Replace("\r\n", "\n").Replace("\r", "\n");
        if (raw.Length > 0 && raw[0] == '\uFEFF') raw = raw.Substring(1);
        var lines = raw.Split('\n');

        // 헤더 필요
        if (lines.Length == 0) return new List<T>();
        var headerFields = ParseCsvLine(lines[0]);

        // 대상 타입의 “실제 포함할 필드” 목록 구성 (public 또는 [SerializeField] private)
        var allFields = typeof(T).GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        var fieldMap = new Dictionary<string, FieldInfo>(StringComparer.OrdinalIgnoreCase);
        foreach (var f in allFields)
        {
            if (f.IsPublic || f.GetCustomAttribute<SerializeField>() != null)
                fieldMap[f.Name] = f;
        }

        var list = new List<T>();

        for (int li = 1; li < lines.Length; li++)
        {
            var line = lines[li];
            if (string.IsNullOrWhiteSpace(line)) continue;

            var values = ParseCsvLine(line);
            var obj = new T();

            for (int hi = 0; hi < headerFields.Count; hi++)
            {
                string header = headerFields[hi].Trim();
                if (!fieldMap.TryGetValue(header, out var field)) continue;

                string value = (hi < values.Count) ? values[hi] : string.Empty;

                try
                {
                    object converted = ConvertValue(field.FieldType, value);
                    field.SetValue(obj, converted);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[CsvLoader] Parse Error @ line {li + 1}, field '{header}' : '{value}' → {ex.Message}");
                }
            }

            list.Add(obj);
        }

        return list;
    }

    public static void SaveCsv<T>(string fullPath, IList<T> dataList)
    {
        if (dataList == null || dataList.Count == 0)
        {
            Debug.LogWarning("[CsvLoader] No data to save.");
            return;
        }

        // 포함할 필드(헤더/값 모두 동일 규칙 적용)
        var allFields = typeof(T).GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        var included = new List<FieldInfo>();
        foreach (var f in allFields)
            if (f.IsPublic || f.GetCustomAttribute<SerializeField>() != null)
                included.Add(f);

        var sb = new StringBuilder();

        // 헤더
        for (int i = 0; i < included.Count; i++)
        {
            sb.Append(included[i].Name);
            if (i < included.Count - 1) sb.Append(',');
        }
        sb.AppendLine();

        // 데이터
        foreach (var item in dataList)
        {
            for (int i = 0; i < included.Count; i++)
            {
                object v = included[i].GetValue(item);
                string s = v != null ? v.ToString() : string.Empty;

                // CSV-safe quoting
                if (s.Contains(",") || s.Contains("\n") || s.Contains("\r") || s.Contains("\""))
                    s = $"\"{s.Replace("\"", "\"\"")}\"";

                sb.Append(s);
                if (i < included.Count - 1) sb.Append(',');
            }
            sb.AppendLine();
        }

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
        File.WriteAllText(fullPath, sb.ToString(), Encoding.UTF8);
        Debug.Log($"[CsvLoader] CSV saved: {fullPath}");
    }

    // ---------- Helpers ----------

    // RFC4180 호환 간단 파서: 따옴표 내부 콤마/따옴표 이스케이프 처리
    private static List<string> ParseCsvLine(string line)
    {
        var cells = new List<string>();
        if (line == null) return cells;

        var sb = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (inQuotes)
            {
                if (c == '"')
                {
                    // 이스케이프된 따옴표 ("")
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        sb.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    sb.Append(c);
                }
            }
            else
            {
                if (c == ',')
                {
                    cells.Add(sb.ToString());
                    sb.Length = 0;
                }
                else if (c == '"')
                {
                    inQuotes = true;
                }
                else
                {
                    sb.Append(c);
                }
            }
        }

        cells.Add(sb.ToString());
        return cells;
    }

    private static object ConvertValue(Type type, string value)
    {
        // 빈 값 처리
        if (string.IsNullOrEmpty(value))
        {
            if (type.IsValueType) return Activator.CreateInstance(type);
            return null;
        }

        // Enum
        if (type.IsEnum) return Enum.Parse(type, value, true);

        // 기본형
        if (type == typeof(int)) { int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i); return i; }
        if (type == typeof(float)) { float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var f); return f; }
        if (type == typeof(double)) { double.TryParse(value, NumberStyles.Float, CultureBox, out var d); return d; }
        if (type == typeof(bool)) { bool.TryParse(value, out var b); return b; }
        if (type == typeof(string)) return value;

        // UnityEngine.Object 등은 경로/이름을 별도로 매핑해야 함(여기서는 미지원)
        return null;
    }

    // 캐시된 Culture
    private static readonly CultureInfo CultureBox = CultureInfo.InvariantCulture;
}
