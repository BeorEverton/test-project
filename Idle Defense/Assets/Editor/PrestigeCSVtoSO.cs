using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class PrestigeCSVtoSO
{
    private static readonly string CsvPath = "Assets/Editor/CSVs/PrestigeNodesCSV.csv";
    private static readonly string NodeOutputFolder = "Assets/Resources/Scriptable Objects/Prestige/Nodes";
    private static readonly string TreeOutputFolder = "Assets/Resources/Scriptable Objects/Prestige/Trees";

    [MenuItem("Utilities/Prestige/Export Nodes (SO → CSV)")]
    public static void ExportNodes()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(CsvPath) ?? "Assets/Editor/CSVs");
            string[] guids = AssetDatabase.FindAssets("t:PrestigeNodeSO");
            var nodes = guids.Select(g => AssetDatabase.GUIDToAssetPath(g))
                             .Select(p => AssetDatabase.LoadAssetAtPath<PrestigeNodeSO>(p))
                             .Where(x => x != null).ToList();

            if (nodes.Count == 0)
            {
                Debug.LogWarning("[PrestigeCSVtoSO] No PrestigeNodeSO assets found to export.");
                return;
            }

            var soType = typeof(PrestigeNodeSO);
            var fields = soType.GetFields(BindingFlags.Public | BindingFlags.Instance);

            var header = new List<string> { "Name" };
            header.AddRange(fields.Select(f => f.Name));

            using (var sw = new StreamWriter(CsvPath, false))
            {
                sw.WriteLine(string.Join(",", header.Select(EscapeCsv)));

                var ci = CultureInfo.InvariantCulture;
                foreach (var node in nodes.OrderBy(n => n.name))
                {
                    var row = new List<string> { node.name };
                    foreach (var f in fields)
                    {
                        object val = f.GetValue(node);
                        row.Add(SerializeValue(f.FieldType, val, ci));
                    }
                    sw.WriteLine(string.Join(",", row.Select(EscapeCsv)));
                }
            }

            AssetDatabase.Refresh();
            Debug.Log($"[PrestigeCSVtoSO] Exported {nodes.Count} nodes to {CsvPath}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[PrestigeCSVtoSO] Export failed: {ex}");
        }
    }

    [MenuItem("Utilities/Prestige/Import Nodes (CSV → SO)")]
    public static void ImportNodes()
    {
        try
        {
            if (!File.Exists(CsvPath))
            {
                Debug.LogError($"[PrestigeCSVtoSO] CSV file not found at {CsvPath}");
                return;
            }

            Directory.CreateDirectory(NodeOutputFolder);
            var lines = File.ReadAllLines(CsvPath).Where(l => !string.IsNullOrWhiteSpace(l)).ToList();
            if (lines.Count < 2)
            {
                Debug.LogError("[PrestigeCSVtoSO] CSV must have header and at least one row.");
                return;
            }

            var headers = SplitCsvLine(lines[0]).Select(h => h.Trim()).ToList();
            var headerIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < headers.Count; i++) headerIndex[headers[i]] = i;

            var soType = typeof(PrestigeNodeSO);
            var fields = soType.GetFields(BindingFlags.Public | BindingFlags.Instance);
            var fieldMap = fields.ToDictionary(f => f.Name, f => f, StringComparer.OrdinalIgnoreCase);

            int created = 0, updated = 0;
            foreach (var line in lines.Skip(1))
            {
                var row = SplitCsvLine(line);
                if (row.Count == 0) continue;

                string name = row[0];
                string assetPath = $"{NodeOutputFolder}/{SanitizeFileName(name)}.asset";
                var so = AssetDatabase.LoadAssetAtPath<PrestigeNodeSO>(assetPath);
                bool isNew = false;

                if (so == null)
                {
                    so = ScriptableObject.CreateInstance<PrestigeNodeSO>();
                    AssetDatabase.CreateAsset(so, assetPath);
                    isNew = true;
                }

                foreach (var kv in fieldMap)
                {
                    if (!headerIndex.TryGetValue(kv.Key, out int c)) continue;
                    string raw = c < row.Count ? row[c] : string.Empty;
                    try
                    {
                        object parsed = ParseValue(kv.Value.FieldType, raw);
                        if (parsed != null) kv.Value.SetValue(so, parsed);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[PrestigeCSVtoSO] Field {kv.Key} failed to parse: {ex.Message}");
                    }
                }

                EditorUtility.SetDirty(so);
                if (isNew) created++; else updated++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[PrestigeCSVtoSO] Import complete. Created: {created}, Updated: {updated}.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[PrestigeCSVtoSO] Import failed: {ex}");
        }
    }

    // ---------- Helper Methods ----------

    private static object ParseValue(Type t, string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var ci = CultureInfo.InvariantCulture;

        if (t == typeof(string)) return raw;
        if (t == typeof(int) && int.TryParse(raw, NumberStyles.Integer, ci, out int i)) return i;
        if (t == typeof(float) && float.TryParse(raw, NumberStyles.Float, ci, out float f)) return f;
        if (t == typeof(Vector2))
        {
            var parts = raw.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2)
                return new Vector2(float.Parse(parts[0], ci), float.Parse(parts[1], ci));
        }
        if (t == typeof(List<string>))
            return raw.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToList();

        if (t.IsEnum)
        {
            if (Enum.TryParse(t, raw, true, out object e)) return e;
        }

        if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(List<>))
        {
            var elemType = t.GetGenericArguments()[0];
            var method = typeof(PrestigeCSVtoSO).GetMethod(nameof(ParseList), BindingFlags.NonPublic | BindingFlags.Static);
            var generic = method.MakeGenericMethod(elemType);
            return generic.Invoke(null, new object[] { raw });
        }

        return null;
    }

    private static List<T> ParseList<T>(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return new List<T>();
        var parts = raw.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
        var ci = CultureInfo.InvariantCulture;
        var result = new List<T>();
        foreach (var p in parts)
        {
            try
            {
                if (typeof(T).IsEnum)
                    result.Add((T)Enum.Parse(typeof(T), p.Trim(), true));
                else
                    result.Add((T)Convert.ChangeType(p.Trim(), typeof(T), ci));
            }
            catch { }
        }
        return result;
    }

    private static string SerializeValue(Type t, object value, CultureInfo ci)
    {
        if (value == null) return string.Empty;
        if (t == typeof(Vector2))
        {
            var v = (Vector2)value;
            return $"{v.x.ToString(ci)},{v.y.ToString(ci)}";
        }
        if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(List<>))
        {
            var list = (System.Collections.IEnumerable)value;
            return string.Join(";", list.Cast<object>());
        }
        return Convert.ToString(value, ci);
    }

    private static List<string> SplitCsvLine(string line)
    {
        var result = new List<string>();
        if (line == null) return result;
        bool inQuotes = false;
        var cur = new System.Text.StringBuilder();
        foreach (var ch in line)
        {
            if (ch == '"') inQuotes = !inQuotes;
            else if (ch == ',' && !inQuotes) { result.Add(cur.ToString()); cur.Length = 0; }
            else cur.Append(ch);
        }
        result.Add(cur.ToString());
        return result;
    }

    private static string EscapeCsv(string s)
    {
        if (s == null) return "";
        bool mustQuote = s.Contains(",") || s.Contains("\"") || s.Contains("\n");
        if (mustQuote) s = $"\"{s.Replace("\"", "\"\"")}\"";
        return s;
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Join("", name.Where(c => !invalid.Contains(c)));
    }
}
