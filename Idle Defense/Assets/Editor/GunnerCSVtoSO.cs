using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Assets.Editor
{
    public static class GunnerCSVtoSO
    {
        // Path relative to Assets
        private static string _csvPath = "/Editor/CSVs/GunnerCSV.csv";

        // Where to create/find Gunner SOs by id
        private const string GunnerSoFolder = "Assets/Resources/Scriptable Objects/Gunners";

        private static readonly CultureInfo CI = CultureInfo.InvariantCulture;

        // Columns must match this order.
        private static readonly string[] Header =
        {
            "InGame",

            // Identity
            "GunnerId",
            "DisplayName",
            "BackgroundDescription",

            // Classification
            "Area",
            "Mood",
            "Class",
            "Tier",

            // Visuals / Assets
            "GunnerSpriteAssetPath",
            "ModelPrefabAssetPath",
            "LimitBreakReadyVfxAssetPath",
            "LimitBreakSkillAssetPath",

            // 3D model tuning
            "ModelOffsetOnTurret",          // x|y|z
            "RunSpeed",
            "ArrivalSnapDistance",

            // Flavor lists (pipe-separated)
            "IdlePhrases",
            "PraisePhrases",
            "NegativePhrases",
            "CombatChatPhrases",

            // Base stats
            "BaseHealth",
            "BaseDamage",
            "BaseFireRate",
            "BaseRange",
            "BaseDamagePerSecPctBonus",
            "BaseSlowEffect",
            "BaseCriticalChance",
            "BaseCriticalDamage",
            "BaseKnockback",
            "BaseSplash",
            "BasePierceChance",
            "BasePierceFalloff",
            "BaseArmorPenetration",

            // Leveling
            "SkillPointsPerLevel",
            "XpCurveKeys",                  // time|value|inTangent|outTangent ; time|value|inTangent|outTangent ...

            // Unlocks
            "StartingUnlocked",              // Stat|Stat|Stat
            "LevelUnlocks",                  // level:Stat|Stat;level:Stat|Stat

            // Upgrade rules
            "UpgradeRules"                   // Stat,Mode,Amount,Min,Max;Stat,Mode,Amount,Min,Max
        };

        [MenuItem("Utilities/Gunners/Import CSV")]
        public static void ImportCsv()
        {
            string full = Application.dataPath + _csvPath;
            if (!File.Exists(full))
            {
                Debug.LogError($"CSV not found at: {full}");
                return;
            }

            string[] lines = File.ReadAllLines(full);
            if (lines.Length <= 1)
            {
                Debug.LogWarning("CSV is empty or only has a header.");
                return;
            }

            EnsureFolder(GunnerSoFolder);

            // Header validation (non-fatal)
            string[] hdr = SplitCsvLine(lines[0]).ToArray();
            if (!Header.SequenceEqual(hdr))
                Debug.LogWarning("CSV header does not match expected columns. Import will still attempt by position.");

            int imported = 0;
            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i];
                if (string.IsNullOrWhiteSpace(line)) continue;

                var cols = SplitCsvLine(line).ToArray();
                if (cols.Length < Header.Length)
                {
                    Debug.LogWarning($"Skipping line {i + 1} with insufficient columns.");
                    continue;
                }

                bool inGame = ParseBool(cols[0]);
                if (!inGame) continue;

                string gunnerId = cols[1].Trim();
                if (string.IsNullOrEmpty(gunnerId))
                {
                    Debug.LogWarning($"Skipping line {i + 1}: GunnerId is empty.");
                    continue;
                }

                string soPath = $"{GunnerSoFolder}/{gunnerId}.asset";
                var so = AssetDatabase.LoadAssetAtPath<GunnerSO>(soPath);
                if (so == null)
                {
                    so = ScriptableObject.CreateInstance<GunnerSO>();
                    AssetDatabase.CreateAsset(so, soPath);
                }

                // Identity
                so.name = gunnerId;
                so.GunnerId = gunnerId;
                so.DisplayName = cols[2];
                so.backgroundDescription = cols[3];

                // Classification
                so.Area = ParseEnum(cols[4], so.Area);
                so.Mood = ParseEnum(cols[5], so.Mood);
                so.Class = ParseEnum(cols[6], so.Class);
                so.Tier = ParseEnum(cols[7], so.Tier);

                // Assets
                so.gunnerSprite = LoadAtPath<Sprite>(cols[8], "GunnerSprite", gunnerId);
                so.ModelPrefab = LoadAtPath<GameObject>(cols[9], "ModelPrefab", gunnerId);
                so.LimitBreakReadyVfx = LoadAtPath<GameObject>(cols[10], "LimitBreakReadyVfx", gunnerId);
                so.LimitBreakSkill = LoadAtPath<LimitBreakSkillSO>(cols[11], "LimitBreakSkill", gunnerId);

                // 3D model tuning
                so.ModelOffsetOnTurret = ParseVector3(cols[12], so.ModelOffsetOnTurret);
                so.RunSpeed = ParseFloat(cols[13], so.RunSpeed);
                so.ArrivalSnapDistance = ParseFloat(cols[14], so.ArrivalSnapDistance);

                // Flavor lists
                so.IdlePhrases = ParseStringList(cols[15]);
                so.PraisePhrases = ParseStringList(cols[16]);
                so.NegativePhrases = ParseStringList(cols[17]);
                so.CombatChatPhrases = ParseStringList(cols[18]);

                // Base stats
                so.BaseHealth = ParseFloat(cols[19], so.BaseHealth);
                so.BaseDamage = ParseFloat(cols[20], so.BaseDamage);
                so.BaseFireRate = ParseFloat(cols[21], so.BaseFireRate);
                so.BaseRange = ParseFloat(cols[22], so.BaseRange);
                so.BaseDamagePerSecPctBonus = ParseFloat(cols[23], so.BaseDamagePerSecPctBonus);
                so.BaseSlowEffect = ParseFloat(cols[24], so.BaseSlowEffect);
                so.BaseCriticalChance = ParseFloat(cols[25], so.BaseCriticalChance);
                so.BaseCriticalDamage = ParseFloat(cols[26], so.BaseCriticalDamage);
                so.BaseKnockback = ParseFloat(cols[27], so.BaseKnockback);
                so.BaseSplash = ParseFloat(cols[28], so.BaseSplash);
                so.BasePierceChance = ParseFloat(cols[29], so.BasePierceChance);
                so.BasePierceFalloff = ParseFloat(cols[30], so.BasePierceFalloff);
                so.BaseArmorPenetration = ParseFloat(cols[31], so.BaseArmorPenetration);

                // Leveling
                so.SkillPointsPerLevel = ParseInt(cols[32], so.SkillPointsPerLevel);
                so.XpCurve = ParseAnimationCurve(cols[33], so.XpCurve);

                // Unlocks
                so.StartingUnlocked = ParseEnumList(cols[34], new List<GunnerStatKey>());
                so.LevelUnlocks = ParseLevelUnlocks(cols[35]);

                // Upgrade rules
                so.UpgradeRules = ParseUpgradeRules(cols[36], so.UpgradeRules);

                EditorUtility.SetDirty(so);
                imported++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Gunner import complete. Imported/updated: {imported}");
        }

        [MenuItem("Utilities/Gunners/Export CSV")]
        public static void ExportCsv()
        {
            EnsureFolder(Path.GetDirectoryName("Assets" + _csvPath));

            var all = AssetDatabase.FindAssets("t:GunnerSO")
                .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                .Select(path => AssetDatabase.LoadAssetAtPath<GunnerSO>(path))
                .Where(x => x != null)
                .OrderBy(x => x.GunnerId)
                .ToList();

            var lines = new List<string>(all.Count + 1)
            {
                JoinCsv(Header)
            };

            foreach (var g in all)
            {
                var row = new string[Header.Length];

                row[0] = "TRUE";

                // Identity
                row[1] = g.GunnerId ?? string.Empty;
                row[2] = g.DisplayName ?? string.Empty;
                row[3] = g.backgroundDescription ?? string.Empty;

                // Classification
                row[4] = g.Area.ToString();
                row[5] = g.Mood.ToString();
                row[6] = g.Class.ToString();
                row[7] = g.Tier.ToString();

                // Assets
                row[8] = SafeAssetPath(g.gunnerSprite);
                row[9] = SafeAssetPath(g.ModelPrefab);
                row[10] = SafeAssetPath(g.LimitBreakReadyVfx);
                row[11] = SafeAssetPath(g.LimitBreakSkill);

                // 3D model tuning
                row[12] = $"{g.ModelOffsetOnTurret.x.ToString(CI)}|{g.ModelOffsetOnTurret.y.ToString(CI)}|{g.ModelOffsetOnTurret.z.ToString(CI)}";
                row[13] = g.RunSpeed.ToString(CI);
                row[14] = g.ArrivalSnapDistance.ToString(CI);

                // Flavor lists
                row[15] = JoinList(g.IdlePhrases);
                row[16] = JoinList(g.PraisePhrases);
                row[17] = JoinList(g.NegativePhrases);
                row[18] = JoinList(g.CombatChatPhrases);

                // Base stats
                row[19] = g.BaseHealth.ToString(CI);
                row[20] = g.BaseDamage.ToString(CI);
                row[21] = g.BaseFireRate.ToString(CI);
                row[22] = g.BaseRange.ToString(CI);
                row[23] = g.BaseDamagePerSecPctBonus.ToString(CI);
                row[24] = g.BaseSlowEffect.ToString(CI);
                row[25] = g.BaseCriticalChance.ToString(CI);
                row[26] = g.BaseCriticalDamage.ToString(CI);
                row[27] = g.BaseKnockback.ToString(CI);
                row[28] = g.BaseSplash.ToString(CI);
                row[29] = g.BasePierceChance.ToString(CI);
                row[30] = g.BasePierceFalloff.ToString(CI);
                row[31] = g.BaseArmorPenetration.ToString(CI);

                // Leveling
                row[32] = g.SkillPointsPerLevel.ToString(CI);
                row[33] = ExportAnimationCurve(g.XpCurve);

                // Unlocks
                row[34] = JoinEnumList(g.StartingUnlocked);
                row[35] = ExportLevelUnlocks(g.LevelUnlocks);

                // Upgrade rules
                row[36] = ExportUpgradeRules(g.UpgradeRules);

                lines.Add(JoinCsv(row));
            }

            string full = Application.dataPath + _csvPath;
            File.WriteAllLines(full, lines);
            AssetDatabase.Refresh();
            Debug.Log($"Gunner export complete: {full}");
        }

        [MenuItem("Utilities/Gunners/Open CSV Folder")]
        private static void OpenCsvFolder()
        {
            string dir = Path.GetDirectoryName(Application.dataPath + _csvPath);
            if (Directory.Exists(dir))
                EditorUtility.RevealInFinder(dir);
            else
                Debug.LogWarning($"Folder not found: {dir}");
        }

        // ---------------- Helpers ----------------

        private static T LoadAtPath<T>(string assetPath, string label, string gunnerId) where T : UnityEngine.Object
        {
            assetPath = (assetPath ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(assetPath)) return null;

            var obj = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (obj == null)
                Debug.LogWarning($"{label} not found at path: {assetPath} for {gunnerId}");
            return obj;
        }

        private static void EnsureFolder(string assetFolder)
        {
            if (string.IsNullOrEmpty(assetFolder)) return;
            if (AssetDatabase.IsValidFolder(assetFolder)) return;

            string relative = assetFolder.Replace("\\", "/");
            if (!relative.StartsWith("Assets"))
            {
                Debug.LogError("EnsureFolder expects a path under Assets/.");
                return;
            }

            string[] parts = relative.Split('/');
            string current = parts[0]; // "Assets"
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        private static IEnumerable<string> SplitCsvLine(string line)
        {
            if (line == null) yield break;

            bool inQuotes = false;
            var token = new StringBuilder();

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        token.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    yield return token.ToString();
                    token.Length = 0;
                }
                else
                {
                    token.Append(c);
                }
            }

            yield return token.ToString();
        }

        private static string JoinCsv(IEnumerable<string> cells)
        {
            return string.Join(",", cells.Select(EscapeCsv));
        }

        private static string EscapeCsv(string s)
        {
            if (s == null) return "";
            bool needQuotes = s.Contains(",") || s.Contains("\"") || s.Contains("\n") || s.Contains("\r");
            if (!needQuotes) return s;
            return "\"" + s.Replace("\"", "\"\"") + "\"";
        }

        private static bool ParseBool(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return false;
            s = s.Trim().ToUpperInvariant();
            return s == "TRUE" || s == "1" || s == "YES";
        }

        private static float ParseFloat(string s, float fallback = 0f)
        {
            if (float.TryParse(s, NumberStyles.Float, CI, out var v)) return v;
            return fallback;
        }

        private static int ParseInt(string s, int fallback = 0)
        {
            if (int.TryParse(s, NumberStyles.Integer, CI, out var v)) return v;
            return fallback;
        }

        private static TEnum ParseEnum<TEnum>(string s, TEnum fallback) where TEnum : struct
        {
            if (!string.IsNullOrWhiteSpace(s) && Enum.TryParse<TEnum>(s.Trim(), true, out var v))
                return v;
            return fallback;
        }

        private static List<TEnum> ParseEnumList<TEnum>(string s, List<TEnum> fallback) where TEnum : struct
        {
            if (string.IsNullOrWhiteSpace(s)) return fallback ?? new List<TEnum>();

            var list = new List<TEnum>();
            foreach (var part in s.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (Enum.TryParse<TEnum>(part.Trim(), true, out var v))
                    list.Add(v);
            }
            return list;
        }

        private static string JoinEnumList<TEnum>(IEnumerable<TEnum> list)
        {
            if (list == null) return string.Empty;
            return string.Join("|", list.Select(x => x.ToString()));
        }

        private static List<string> ParseStringList(string s)
        {
            var list = new List<string>();
            if (string.IsNullOrWhiteSpace(s)) return list;

            foreach (var part in s.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string t = part.Trim();
                if (!string.IsNullOrEmpty(t))
                    list.Add(t);
            }
            return list;
        }

        private static string JoinList(List<string> list)
        {
            if (list == null || list.Count == 0) return string.Empty;
            return string.Join("|", list.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()));
        }

        private static Vector3 ParseVector3(string s, Vector3 fallback)
        {
            if (string.IsNullOrWhiteSpace(s)) return fallback;
            var parts = s.Split('|');
            if (parts.Length != 3) return fallback;

            if (!float.TryParse(parts[0], NumberStyles.Float, CI, out var x)) return fallback;
            if (!float.TryParse(parts[1], NumberStyles.Float, CI, out var y)) return fallback;
            if (!float.TryParse(parts[2], NumberStyles.Float, CI, out var z)) return fallback;

            return new Vector3(x, y, z);
        }

        private static AnimationCurve ParseAnimationCurve(string s, AnimationCurve fallback)
        {
            if (string.IsNullOrWhiteSpace(s)) return fallback ?? AnimationCurve.Linear(1, 10, 50, 1000);

            // Format: time|value|inTangent|outTangent;time|value|inTangent|outTangent
            var keys = new List<Keyframe>();
            var keyChunks = s.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var chunk in keyChunks)
            {
                var p = chunk.Split('|');
                if (p.Length < 2) continue;

                if (!float.TryParse(p[0], NumberStyles.Float, CI, out var t)) continue;
                if (!float.TryParse(p[1], NumberStyles.Float, CI, out var v)) continue;

                float inTan = 0f;
                float outTan = 0f;
                if (p.Length >= 3) float.TryParse(p[2], NumberStyles.Float, CI, out inTan);
                if (p.Length >= 4) float.TryParse(p[3], NumberStyles.Float, CI, out outTan);

                var k = new Keyframe(t, v, inTan, outTan);
                keys.Add(k);
            }

            if (keys.Count == 0) return fallback ?? AnimationCurve.Linear(1, 10, 50, 1000);

            var curve = new AnimationCurve(keys.ToArray());
            return curve;
        }

        private static string ExportAnimationCurve(AnimationCurve curve)
        {
            if (curve == null || curve.keys == null || curve.keys.Length == 0) return string.Empty;

            // time|value|inTangent|outTangent;...
            var parts = new List<string>(curve.keys.Length);
            foreach (var k in curve.keys)
            {
                parts.Add(
                    $"{k.time.ToString(CI)}|{k.value.ToString(CI)}|{k.inTangent.ToString(CI)}|{k.outTangent.ToString(CI)}"
                );
            }
            return string.Join(";", parts);
        }

        private static List<GunnerSO.LevelUnlock> ParseLevelUnlocks(string s)
        {
            var list = new List<GunnerSO.LevelUnlock>();
            if (string.IsNullOrWhiteSpace(s)) return list;

            // Format: level:Stat|Stat;level:Stat|Stat
            var entries = s.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var e in entries)
            {
                var idx = e.IndexOf(':');
                if (idx <= 0) continue;

                string levelStr = e.Substring(0, idx).Trim();
                string unlockStr = e.Substring(idx + 1).Trim();

                if (!int.TryParse(levelStr, NumberStyles.Integer, CI, out int level)) continue;

                var unlocks = ParseEnumList<GunnerStatKey>(unlockStr, new List<GunnerStatKey>());
                list.Add(new GunnerSO.LevelUnlock
                {
                    Level = level,
                    Unlocks = unlocks
                });
            }

            // stable ordering
            list.Sort((a, b) => a.Level.CompareTo(b.Level));
            return list;
        }

        private static string ExportLevelUnlocks(List<GunnerSO.LevelUnlock> list)
        {
            if (list == null || list.Count == 0) return string.Empty;

            // level:Stat|Stat;...
            var parts = new List<string>(list.Count);
            foreach (var lu in list.OrderBy(x => x.Level))
            {
                string unlocks = (lu.Unlocks == null) ? string.Empty : string.Join("|", lu.Unlocks.Select(x => x.ToString()));
                parts.Add($"{lu.Level.ToString(CI)}:{unlocks}");
            }
            return string.Join(";", parts);
        }

        private static List<GunnerSO.GunnerUpgradeRule> ParseUpgradeRules(string s, List<GunnerSO.GunnerUpgradeRule> fallback)
        {
            if (string.IsNullOrWhiteSpace(s))
                return fallback ?? new List<GunnerSO.GunnerUpgradeRule>();

            // Format: Stat,Mode,Amount,Min,Max;Stat,Mode,Amount,Min,Max
            var list = new List<GunnerSO.GunnerUpgradeRule>();
            var entries = s.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var e in entries)
            {
                var p = e.Split(new[] { ',' }, StringSplitOptions.None);
                if (p.Length < 3) continue;

                if (!Enum.TryParse(p[0].Trim(), true, out GunnerStatKey stat)) continue;
                if (!Enum.TryParse(p[1].Trim(), true, out GunnerSO.GunnerUpgradeMode mode)) mode = GunnerSO.GunnerUpgradeMode.FlatAdd;

                float amount = (p.Length >= 3) ? ParseFloat(p[2], 0f) : 0f;
                float min = (p.Length >= 4) ? ParseFloat(p[3], 0f) : 0f;
                float max = (p.Length >= 5) ? ParseFloat(p[4], 0f) : 0f;

                list.Add(new GunnerSO.GunnerUpgradeRule
                {
                    Stat = stat,
                    Mode = mode,
                    AmountPerLevel = amount,
                    MinValue = min,
                    MaxValue = max
                });
            }

            return list;
        }

        private static string ExportUpgradeRules(List<GunnerSO.GunnerUpgradeRule> list)
        {
            if (list == null || list.Count == 0) return string.Empty;

            // Stat,Mode,Amount,Min,Max;...
            var parts = new List<string>(list.Count);
            foreach (var r in list)
            {
                parts.Add(
                    $"{r.Stat},{r.Mode},{r.AmountPerLevel.ToString(CI)},{r.MinValue.ToString(CI)},{r.MaxValue.ToString(CI)}"
                );
            }
            return string.Join(";", parts);
        }

        private static string SafeAssetPath(UnityEngine.Object obj)
        {
            if (obj == null) return string.Empty;
            return AssetDatabase.GetAssetPath(obj) ?? string.Empty;
        }
    }
}
