using Assets.Scripts.SO;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Assets.Editor
{
    public static class EnemyCSVtoSO
    {
        // Path relative to Assets
        private static string _csvPath = "/Editor/CSVs/EnemyInfoCSV.csv";

        // Where to create/find Enemy SOs by name
        private const string EnemySoFolder = "Assets/Resources/Scriptable Objects/Enemies";

        private static readonly CultureInfo CI = CultureInfo.InvariantCulture;

        // Full header used by both export and import (columns must match the Read/Write order)
        private static readonly string[] Header =
        {
            // General
            "InGame",                 // 0
            "Name",                   // 1
            "EnemyId",             // 2
            "IconAssetPath",          // 3 optional (can be empty)
            // Base stats
            "MaxHealth",              // 4
            "HealthMultiplierByWaveCount", // 5
            "MovementSpeed",          // 6
            "MovementSpeedDifference",// 7
            "IsFlying",               // 8
            "CoinDropAmount",         // 9
            "CurrencyDropType",       // 10
            "CoinDropMultiplierByWaveCount", // 11
            // Attack stats
            "Damage",                 // 12
            "DamageMultiplierByWaveCount", // 13  NEW
            "AttackRange",            // 14
            "AttackRangeDifference",  // 15
            "AttackSpeed",            // 16
            "SweepTargets",           // 17
            // Defense
            "Armor",                  // 18

            "DodgeChance",            // 19
            "ShieldCharges",          // 20
            // Exploder
            "ExploderEnabled",        // 21
            "ExploderDelay",          // 22
            "ExploderRadius",         // 23
            "ExploderMaxGunners",     // 24
            // Healer
            "HealerEnabled",          // 25
            "HealerCooldown",         // 26
            "HealerHealPctOfMaxHP",   // 27
            "HealerRadius",           // 28
            "HealerMaxTargets",       // 29
            // Kamikaze
            "KamikazeOnReach",        // 30
            "KamikazeRadius",         // 31
            "KamikazeMaxGunners",     // 32
            // Summoner
            "SummonerEnabled",        // 33
            "SummonPrefabAssetPath",  // 34 optional (can be empty)
            "SummonPrewarmCount",     // 35
            "SummonType",             // 36
            "SummonCount",            // 37
            "SummonStreamInterval",   // 38
            // Summon Placement
            "SummonForwardDepth",     // 39
            "SummonXJitter",          // 40
            // Summon Timing
            "SummonFirstDelay",       // 41
            "SummonCooldown"          // 42
        };

        // ===== MENU =====

        [MenuItem("Utilities/Enemies/Import CSV")]
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

            EnsureFolder(EnemySoFolder);

            // Validate header (non-fatal; logs mismatch)
            string[] hdr = SplitCsvLine(lines[0]).ToArray();
            if (!Header.SequenceEqual(hdr))
            {
                Debug.LogWarning("CSV header does not match expected columns. Import will still attempt by position.");
            }

            foreach (string line in lines.Skip(1))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                var cols = SplitCsvLine(line).ToArray();
                if (cols.Length < Header.Length)
                {
                    Debug.LogWarning($"Skipping line with insufficient columns: {line}");
                    continue;
                }

                bool inGame = ParseBool(cols[0]);
                string name = cols[1].Trim();
                if (!inGame || string.IsNullOrEmpty(name))
                    continue;

                // Enemy SO path by name
                string soPath = $"{EnemySoFolder}/{name}.asset";
                var enemyInfo = AssetDatabase.LoadAssetAtPath<EnemyInfoSO>(soPath);
                if (enemyInfo == null)
                {
                    enemyInfo = ScriptableObject.CreateInstance<EnemyInfoSO>();
                    AssetDatabase.CreateAsset(enemyInfo, soPath);
                }

                // Base info and icon
                enemyInfo.name = name;
                enemyInfo.Name = name;
                enemyInfo.EnemyId = ParseEnemyId(cols[2], name);

                string iconPath = cols[3].Trim();
                if (!string.IsNullOrEmpty(iconPath))
                {
                    var icon = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
                    if (icon == null)
                        Debug.LogWarning($"Icon not found at path: {iconPath} for {name}");
                    enemyInfo.Icon = icon;
                }

                // Base stats
                enemyInfo.MaxHealth = ParseFloat(cols[4]);
                enemyInfo.HealthMultiplierByWaveCount = ParseFloat(cols[5]);
                enemyInfo.MovementSpeed = ParseFloat(cols[6]);
                enemyInfo.MovementSpeedDifference = ParseFloat(cols[7]);
                enemyInfo.IsFlying = ParseBool(cols[8]);
                enemyInfo.CoinDropAmount = ParseULong(cols[9]);
                enemyInfo.CurrencyDropType = ParseEnum(cols[10], enemyInfo.CurrencyDropType);
                enemyInfo.CoinDropMultiplierByWaveCount = ParseFloat(cols[11]);

                // Attack
                enemyInfo.Damage = ParseFloat(cols[12]);
                enemyInfo.DamageMultiplierByWaveCount = ParseFloat(cols[13]); // NEW
                enemyInfo.AttackRange = ParseFloat(cols[14]);
                enemyInfo.AttackRangeDifference = ParseFloat(cols[15]);
                enemyInfo.AttackSpeed = ParseFloat(cols[16]);
                enemyInfo.SweepTargets = Mathf.Clamp(ParseInt(cols[17], 1), 1, 5);

                // Defense
                enemyInfo.Armor = Mathf.Clamp01(ParseFloat(cols[18]));
                enemyInfo.DodgeChance = Mathf.Clamp01(ParseFloat(cols[19]));
                enemyInfo.ShieldCharges = Mathf.Max(0, ParseInt(cols[20]));

                // Exploder
                enemyInfo.ExploderEnabled = ParseBool(cols[21]);
                enemyInfo.ExploderDelay = ParseFloat(cols[22]);
                enemyInfo.ExploderRadius = ParseFloat(cols[23]);
                enemyInfo.ExploderMaxGunners = Mathf.Clamp(ParseInt(cols[24], 1), 1, 5);

                // Healer
                enemyInfo.HealerEnabled = ParseBool(cols[25]);
                enemyInfo.HealerCooldown = ParseFloat(cols[26]);
                enemyInfo.HealerHealPctOfMaxHP = Mathf.Clamp01(ParseFloat(cols[27]));
                enemyInfo.HealerRadius = ParseFloat(cols[28]);
                enemyInfo.HealerMaxTargets = Mathf.Max(0, ParseInt(cols[29]));

                // Kamikaze
                enemyInfo.KamikazeOnReach = ParseBool(cols[30]);
                enemyInfo.KamikazeRadius = ParseFloat(cols[31]);
                enemyInfo.KamikazeMaxGunners = Mathf.Clamp(ParseInt(cols[32], 1), 1, 5);

                // Summoner
                enemyInfo.SummonerEnabled = ParseBool(cols[33]);

                string prefabPath = cols[33].Trim();
                if (!string.IsNullOrEmpty(prefabPath))
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                    if (prefab == null)
                        Debug.LogWarning($"Summon prefab not found at path: {prefabPath} for {name}");
                    enemyInfo.SummonPrefab = prefab;
                }
                else
                {
                    enemyInfo.SummonPrefab = null;
                }

                enemyInfo.SummonPrewarmCount = Mathf.Max(0, ParseInt(cols[34]));
                enemyInfo.SummonType = ParseEnum(cols[35], EnemyInfoSO.SummonMode.Burst);
                enemyInfo.SummonCount = Mathf.Max(0, ParseInt(cols[36]));
                enemyInfo.SummonStreamInterval = ParseFloat(cols[37]);

                // Summon placement
                enemyInfo.SummonForwardDepth = ParseFloat(cols[38]);
                enemyInfo.SummonXJitter = ParseFloat(cols[39]);

                // Summon timing
                enemyInfo.SummonFirstDelay = ParseFloat(cols[40]);
                enemyInfo.SummonCooldown = ParseFloat(cols[41]);

                EditorUtility.SetDirty(enemyInfo);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Enemy import complete.");
        }

        [MenuItem("Utilities/Enemies/Export CSV")]
        public static void ExportCsv()
        {
            EnsureFolder(Path.GetDirectoryName("Assets" + _csvPath));

            var all = AssetDatabase.FindAssets("t:EnemyInfoSO")
                .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                .Select(path => AssetDatabase.LoadAssetAtPath<EnemyInfoSO>(path))
                .Where(x => x != null)
                .OrderBy(x => x.Name)
                .ToList();

            var lines = new List<string>(all.Count + 1)
            {
                JoinCsv(Header)
            };

            foreach (var e in all)
            {
                var row = new string[Header.Length];

                // General
                row[0] = "TRUE"; // InGame flag; exporter assumes existing SOs are in-game
                row[1] = e.Name ?? string.Empty;
                row[2] = e.EnemyId.ToString();
                row[3] = SafeAssetPath(e.Icon); // IconAssetPath

                // Base stats
                row[4] = e.MaxHealth.ToString(CI);
                row[5] = e.HealthMultiplierByWaveCount.ToString(CI);
                row[6] = e.MovementSpeed.ToString(CI);
                row[7] = e.MovementSpeedDifference.ToString(CI);
                row[8] = e.IsFlying ? "TRUE" : "FALSE";
                row[9] = e.CoinDropAmount.ToString(CI);
                row[10] = e.CurrencyDropType.ToString();
                row[11] = e.CoinDropMultiplierByWaveCount.ToString(CI);

                // Attack
                row[12] = e.Damage.ToString(CI);
                row[13] = e.DamageMultiplierByWaveCount.ToString(CI); // NEW
                row[14] = e.AttackRange.ToString(CI);
                row[15] = e.AttackRangeDifference.ToString(CI);
                row[16] = e.AttackSpeed.ToString(CI);
                row[17] = e.SweepTargets.ToString(CI);

                // Defense
                row[18] = e.Armor.ToString(CI);
                row[19] = e.DodgeChance.ToString(CI);
                row[20] = e.ShieldCharges.ToString(CI);


                // Exploder
                row[21] = e.ExploderEnabled ? "TRUE" : "FALSE";
                row[22] = e.ExploderDelay.ToString(CI);
                row[23] = e.ExploderRadius.ToString(CI);
                row[24] = e.ExploderMaxGunners.ToString(CI);

                // Healer
                row[25] = e.HealerEnabled ? "TRUE" : "FALSE";
                row[26] = e.HealerCooldown.ToString(CI);
                row[27] = e.HealerHealPctOfMaxHP.ToString(CI);
                row[28] = e.HealerRadius.ToString(CI);
                row[29] = e.HealerMaxTargets.ToString(CI);

                // Kamikaze
                row[30] = e.KamikazeOnReach ? "TRUE" : "FALSE";
                row[31] = e.KamikazeRadius.ToString(CI);
                row[32] = e.KamikazeMaxGunners.ToString(CI);

                // Summoner
                row[33] = e.SummonerEnabled ? "TRUE" : "FALSE";
                row[34] = SafeAssetPath(e.SummonPrefab); // SummonPrefabAssetPath
                row[35] = e.SummonPrewarmCount.ToString(CI);
                row[36] = e.SummonType.ToString();
                row[37] = e.SummonCount.ToString(CI);
                row[38] = e.SummonStreamInterval.ToString(CI);

                // Summon placement
                row[39] = e.SummonForwardDepth.ToString(CI);
                row[40] = e.SummonXJitter.ToString(CI);

                // Summon timing
                row[41] = e.SummonFirstDelay.ToString(CI);
                row[42] = e.SummonCooldown.ToString(CI);

                lines.Add(JoinCsv(row));
            }

            string full = Application.dataPath + _csvPath;
            File.WriteAllLines(full, lines);
            AssetDatabase.Refresh();
            Debug.Log($"Enemy export complete: {full}");
        }

        [MenuItem("Utilities/Enemies/Open CSV Folder")]
        private static void OpenCsvFolder()
        {
            string dir = Path.GetDirectoryName(Application.dataPath + _csvPath);
            if (Directory.Exists(dir))
                EditorUtility.RevealInFinder(dir);
            else
                Debug.LogWarning($"Folder not found: {dir}");
        }

        // ===== Helpers =====

        private static int ParseEnemyId(string s, string enemyName)
        {
            // Preferred: numeric ID
            if (int.TryParse(s?.Trim(), NumberStyles.Integer, CI, out int id) && id > 0)
                return id;

            // Back-compat: older CSVs may contain the old EnemyClass string here.
            // We generate a stable (but not guaranteed collision-free) fallback ID.
            // IMPORTANT: You should eventually replace these with real unique IDs in the CSV.
            int hash = Mathf.Abs((enemyName ?? string.Empty).GetHashCode());
            return Mathf.Max(1, hash);
        }


        private static void EnsureFolder(string assetFolder)
        {
            if (string.IsNullOrEmpty(assetFolder)) return;
            if (AssetDatabase.IsValidFolder(assetFolder)) return;

            // Create nested folders under Assets
            string relative = assetFolder.Replace("\\", "/");
            if (!relative.StartsWith("Assets"))
            {
                Debug.LogError("EnsureFolder expects a path under Assets/");
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
            // Simple CSV splitter w/ quotes support: "a,b",c -> [a,b], c
            if (line == null) yield break;

            bool inQuotes = false;
            var token = new System.Text.StringBuilder();

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '\"')
                {
                    // Double quote inside quoted field -> treat as literal quote
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '\"')
                    {
                        token.Append('\"');
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

        private static float ParseFloat(string s)
        {
            if (float.TryParse(s, NumberStyles.Float, CI, out var v)) return v;
            return 0f;
        }

        private static int ParseInt(string s, int fallback = 0)
        {
            if (int.TryParse(s, NumberStyles.Integer, CI, out var v)) return v;
            return fallback;
        }

        private static ulong ParseULong(string s)
        {
            if (ulong.TryParse(s, NumberStyles.Integer, CI, out var v)) return v;
            return 0UL;
        }

        private static T ParseEnum<T>(string s, T fallback) where T : struct
        {
            if (!string.IsNullOrWhiteSpace(s) && Enum.TryParse<T>(s.Trim(), true, out var v))
                return v;
            return fallback;
        }

        private static string SafeAssetPath(UnityEngine.Object obj)
        {
            if (obj == null) return string.Empty;
            return AssetDatabase.GetAssetPath(obj) ?? string.Empty;
        }
    }
}
