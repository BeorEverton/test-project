using Assets.Scripts.SO;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Assets.Editor
{
    public static class TurretCSVtoSO
    {
        // CSV file used for Import and Export (round-trip friendly)
        private static readonly string CsvRelativePath = "Assets/Editor/CSVs/TurretInfoCSV.csv";

        // Where to create/update ScriptableObjects when importing
        private static readonly string OutputFolder = "Assets/Resources/Scriptable Objects/Turrets";

        // Where to look first when exporting existing assets (matches your screenshot)
        private static readonly string PreferredTurretFolder = "Assets/Resources/Scriptable Objects/Turrets";

        // =========================
        // IMPORT: CSV -> ScriptableObjects
        // =========================
        [MenuItem("Utilities/Turrets/Import (CSV → SO)")]
        public static void ImportTurrets()
        {
            try
            {
                if (!File.Exists(CsvRelativePath))
                {
                    Debug.LogError($"[TurretCSVtoSO] CSV file not found at: {CsvRelativePath}");
                    return;
                }

                Directory.CreateDirectory(OutputFolder);

                var lines = File.ReadAllLines(CsvRelativePath)
                                .Where(l => !string.IsNullOrWhiteSpace(l))
                                .ToList();

                if (lines.Count < 2)
                {
                    Debug.LogError("[TurretCSVtoSO] CSV must have a header row and at least one data row.");
                    return;
                }

                // Header
                var headers = SplitCsvLine(lines[0]).Select(h => h.Trim()).ToList();
                var headerIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < headers.Count; i++) headerIndex[headers[i]] = i;

                if (!headerIndex.ContainsKey("TurretType"))
                {
                    Debug.LogError("[TurretCSVtoSO] CSV must include a 'TurretType' column matching the enum names.");
                    return;
                }

                // SO fields
                var soType = typeof(TurretInfoSO);
                var fields = soType.GetFields(BindingFlags.Public | BindingFlags.Instance);
                var fieldMap = fields.ToDictionary(f => f.Name, f => f, StringComparer.OrdinalIgnoreCase);

                int created = 0, updated = 0, rows = 0;

                for (int r = 1; r < lines.Count; r++)
                {
                    rows++;
                    var row = SplitCsvLine(lines[r]);
                    if (row.Count == 0) continue;

                    var turretTypeStr = GetValue(row, headerIndex, "TurretType");
                    if (string.IsNullOrWhiteSpace(turretTypeStr))
                    {
                        Debug.LogWarning($"[TurretCSVtoSO] Row {r + 1}: TurretType empty. Skipping.");
                        continue;
                    }

                    if (!Enum.TryParse(turretTypeStr.Trim(), true, out TurretType turretType))
                    {
                        Debug.LogWarning($"[TurretCSVtoSO] Row {r + 1}: Unknown TurretType '{turretTypeStr}'. Skipping.");
                        continue;
                    }

                    var nameFromCsv = headerIndex.ContainsKey("Name") ? GetValue(row, headerIndex, "Name") : null;
                    var assetName = string.IsNullOrWhiteSpace(nameFromCsv) ? turretType.ToString() : SanitizeFileName(nameFromCsv);

                    var assetPath = $"{OutputFolder}/{assetName}.asset";
                    var so = AssetDatabase.LoadAssetAtPath<TurretInfoSO>(assetPath);
                    bool isNew = false;

                    if (so == null)
                    {
                        so = ScriptableObject.CreateInstance<TurretInfoSO>();
                        AssetDatabase.CreateAsset(so, assetPath);
                        isNew = true;
                    }

                    // Ensure enum is set
                    so.TurretType = turretType;

                    // Assign ALL public fields from CSV where present
                    foreach (var kv in fieldMap)
                    {
                        string fieldName = kv.Key;
                        var fi = kv.Value;

                        if (string.Equals(fieldName, nameof(TurretInfoSO.TurretType), StringComparison.OrdinalIgnoreCase))
                            continue;

                        if (!headerIndex.TryGetValue(fieldName, out int c)) continue;
                        var raw = c >= 0 && c < row.Count ? row[c] : string.Empty;

                        try
                        {
                            object parsed = ParseValue(fi.FieldType, raw);
                            if (parsed != null) fi.SetValue(so, parsed);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning($"[TurretCSVtoSO] Row {r + 1}, Field '{fieldName}': value '{raw}' failed to parse. {ex.Message}");
                        }
                    }

                    EditorUtility.SetDirty(so);
                    if (isNew) created++; else updated++;
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"[TurretCSVtoSO] Import complete. Rows: {rows}, Created: {created}, Updated: {updated}. Output: {OutputFolder}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TurretCSVtoSO] Import failed: {ex}");
            }
        }

        // =========================
        // EXPORT: ScriptableObjects -> CSV
        // =========================
        [MenuItem("Utilities/Turrets/Export (SO → CSV)")]
        public static void ExportTurrets()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(CsvRelativePath) ?? "Assets/Editor/CSVs");

                // Find all TurretInfoSO assets, preferring the folder from your project structure
                string[] guids;
                if (AssetDatabase.IsValidFolder(PreferredTurretFolder))
                {
                    guids = AssetDatabase.FindAssets("t:TurretInfoSO", new[] { PreferredTurretFolder });
                }
                else
                {
                    guids = AssetDatabase.FindAssets("t:TurretInfoSO");
                }

                var assets = guids
                    .Select(g => AssetDatabase.GUIDToAssetPath(g))
                    .Select(p => AssetDatabase.LoadAssetAtPath<TurretInfoSO>(p))
                    .Where(x => x != null)
                    .ToList();

                if (assets.Count == 0)
                {
                    Debug.LogWarning("[TurretCSVtoSO] No TurretInfoSO assets found to export.");
                    return;
                }

                // Get public fields
                var soType = typeof(TurretInfoSO);
                var fields = soType.GetFields(BindingFlags.Public | BindingFlags.Instance);

                // Put Name first (asset name), then all fields (TurretType included)
                var header = new List<string> { "Name" };
                header.AddRange(fields.Select(f => f.Name));

                using (var sw = new StreamWriter(CsvRelativePath, false))
                {
                    sw.WriteLine(string.Join(",", header.Select(EscapeCsv)));

                    var ci = CultureInfo.InvariantCulture;

                    foreach (var so in assets.OrderBy(a => a.name, StringComparer.OrdinalIgnoreCase))
                    {
                        var row = new List<string> { so.name };

                        foreach (var f in fields)
                        {
                            object val = f.GetValue(so);
                            row.Add(SerializeValue(f.FieldType, val, ci));
                        }

                        sw.WriteLine(string.Join(",", row.Select(EscapeCsv)));
                    }
                }

                AssetDatabase.Refresh();
                Debug.Log($"[TurretCSVtoSO] Exported {assets.Count} turrets to: {CsvRelativePath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TurretCSVtoSO] Export failed: {ex}");
            }
        }

        // ---------- Shared helpers ----------

        private static string GetValue(List<string> row, Dictionary<string, int> headerIndex, string key)
        {
            if (!headerIndex.TryGetValue(key, out int idx) || idx < 0 || idx >= row.Count) return string.Empty;
            return row[idx]?.Trim() ?? string.Empty;
        }

        private static List<string> SplitCsvLine(string line)
        {
            var result = new List<string>();
            if (line == null) return result;

            bool inQuotes = false;
            var current = new System.Text.StringBuilder();

            for (int i = 0; i < line.Length; i++)
            {
                char ch = line[i];
                if (ch == '\"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '\"')
                    {
                        current.Append('\"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (ch == ',' && !inQuotes)
                {
                    result.Add(current.ToString());
                    current.Length = 0;
                }
                else
                {
                    current.Append(ch);
                }
            }
            result.Add(current.ToString());
            return result;
        }

        private static string SanitizeFileName(string name)
        {
            var invalid = Path.GetInvalidFileNameChars();
            return string.Join("", name.Where(c => !invalid.Contains(c)));
        }

        private static object ParseValue(Type t, string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            var ci = CultureInfo.InvariantCulture;

            if (t == typeof(string)) return raw;

            if (t == typeof(bool))
            {
                if (int.TryParse(raw, NumberStyles.Integer, ci, out int i)) return i != 0;
                var s = raw.Trim().ToLowerInvariant();
                if (s == "true" || s == "yes" || s == "y") return true;
                if (s == "false" || s == "no" || s == "n") return false;
                throw new FormatException("Expected bool (true/false/1/0).");
            }

            if (t == typeof(int))
            {
                if (int.TryParse(raw, NumberStyles.Integer, ci, out int v)) return v;
                throw new FormatException("Expected int.");
            }

            if (t == typeof(float))
            {
                if (float.TryParse(raw, NumberStyles.Float | NumberStyles.AllowThousands, ci, out float v)) return v;
                throw new FormatException("Expected float.");
            }

            if (t.IsEnum)
            {
                if (Enum.TryParse(t, raw, true, out object e)) return e;
                throw new FormatException($"Unknown enum value '{raw}'.");
            }

            if (t == typeof(Vector2))
            {
                var nums = ExtractNumbers(raw, 2, ci);
                return new Vector2((float)nums[0], (float)nums[1]);
            }
            if (t == typeof(Vector3))
            {
                var nums = ExtractNumbers(raw, 3, ci);
                return new Vector3((float)nums[0], (float)nums[1], (float)nums[2]);
            }

            throw new NotSupportedException($"Field type '{t.Name}' is not supported by CSV importer.");
        }

        private static double[] ExtractNumbers(string raw, int expected, CultureInfo ci)
        {
            raw = raw.Replace("(", "").Replace(")", "").Replace(" ", "");
            var parts = raw.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != expected) throw new FormatException($"Expected {expected} numbers, got {parts.Length}.");
            var outv = new double[expected];
            for (int i = 0; i < expected; i++)
            {
                if (!double.TryParse(parts[i], NumberStyles.Float | NumberStyles.AllowThousands, ci, out outv[i]))
                    throw new FormatException($"Bad number '{parts[i]}'.");
            }
            return outv;
        }

        private static string SerializeValue(Type t, object value, CultureInfo ci)
        {
            if (value == null) return string.Empty;

            if (t == typeof(string)) return (string)value;
            if (t == typeof(bool)) return ((bool)value) ? "true" : "false";
            if (t == typeof(int)) return ((int)value).ToString(ci);
            if (t == typeof(float)) return ((float)value).ToString("G9", ci); // precise & culture-invariant
            if (t.IsEnum) return value.ToString();

            if (t == typeof(Vector2))
            {
                var v = (Vector2)value;
                return $"{v.x.ToString("G9", ci)},{v.y.ToString("G9", ci)}";
            }
            if (t == typeof(Vector3))
            {
                var v = (Vector3)value;
                return $"{v.x.ToString("G9", ci)},{v.y.ToString("G9", ci)},{v.z.ToString("G9", ci)}";
            }

            // Fallback to string
            return value.ToString();
        }

        private static string EscapeCsv(string s)
        {
            if (s == null) return "";
            bool mustQuote = s.Contains(",") || s.Contains("\"") || s.Contains("\n") || s.Contains("\r");
            if (mustQuote)
            {
                s = s.Replace("\"", "\"\"");
                return $"\"{s}\"";
            }
            return s;
        }
    }
}
