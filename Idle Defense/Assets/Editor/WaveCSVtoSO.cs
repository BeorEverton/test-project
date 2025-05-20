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
    public class WaveCSVtoSO
    {
        private const string CsvRelativePath = "/Editor/CSVs/WaveData.csv";
        private const string PrefabResourcesPath = "Prefabs/Enemies";
        private const string WavesSubfolder = "Scriptable Objects/Waves";

        [MenuItem("Utilities/Generate WaveConfigs")]
        public static void GenerateWaves()
        {
            // 1) Load CSV
            var fullCsv = Application.dataPath + CsvRelativePath;
            Debug.Log("Looking for CSV at: " + fullCsv);
            if (!File.Exists(fullCsv))
            {
                Debug.LogError("Wave CSV not found.");
                return;
            }

            var lines = File.ReadAllLines(fullCsv);
            if (lines.Length < 2)
            {
                Debug.LogError("Wave CSV contains no data rows.");
                return;
            }

            // 2) Ensure output folder exists on disk
            var resourcesRoot = "Assets/Resources";
            var targetFolder = resourcesRoot + "/" + WavesSubfolder;
            if (!Directory.Exists(targetFolder))
            {
                Directory.CreateDirectory(targetFolder);
                Debug.Log("Created folder on disk: " + targetFolder);
            }
            else
            {
                Debug.Log("Using existing folder: " + targetFolder);
            }

            // 3) Parse header for EnemyN/QtyN pairs
            var headers = lines[0].Split(',');
            var pairs = new List<(int enemyIdx, int qtyIdx)>();
            for (int i = 0; i < headers.Length; i++)
            {
                var h = headers[i].Trim();
                if (h.StartsWith("Enemy", StringComparison.OrdinalIgnoreCase))
                {
                    var num = h.Substring(5);
                    var qtyCol = Array.IndexOf(headers, "Qty" + num);
                    if (qtyCol >= 0)
                    {
                        pairs.Add((i, qtyCol));
                        Debug.Log($"Found pair: {h} at {i}, Qty{num} at {qtyCol}");
                    }
                }
            }
            Debug.Log("Total Enemy/Qty column pairs: " + pairs.Count);

            // 4) Process each data row
            for (int row = 1; row < lines.Length; row++)
            {
                var line = lines[row];
                if (string.IsNullOrWhiteSpace(line)) continue;

                var cols = line.Split(',');
                if (cols.Length < 2) continue;

                if (!int.TryParse(cols[0].Trim(), out var waveIndex))
                {
                    Debug.LogError($"Invalid wave index '{cols[0]}' on line {row + 1}");
                    continue;
                }
                if (!float.TryParse(cols[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var spawnDelay))
                {
                    Debug.LogError($"Invalid spawn delay '{cols[1]}' on line {row + 1}");
                    continue;
                }

                var waveSo = ScriptableObject.CreateInstance<WaveConfigSO>();
                waveSo.name = $"WaveConfig_{waveIndex}";
                waveSo.WaveStartIndex = waveIndex;
                waveSo.TimeBetweenSpawns = spawnDelay;
                waveSo.EnemyWaveEntries = new List<EnemyWaveEntry>();
                Debug.Log($"Processing wave {waveIndex} (delay {spawnDelay})");

                foreach (var (eIdx, qIdx) in pairs)
                {
                    if (eIdx >= cols.Length || qIdx >= cols.Length) continue;
                    var enemyName = cols[eIdx].Trim();
                    var qtyRaw = cols[qIdx].Trim();

                    if (string.Equals(enemyName, "Total", StringComparison.OrdinalIgnoreCase))
                        break;

                    if (string.IsNullOrEmpty(enemyName))
                        continue;

                    if (!int.TryParse(qtyRaw, out var qty) || qty <= 0)
                    {
                        Debug.LogWarning($"  Skipping invalid qty '{qtyRaw}' for '{enemyName}'");
                        continue;
                    }

                    var prefab = Resources.Load<GameObject>(PrefabResourcesPath + "/" + enemyName);
                    if (prefab == null)
                    {
                        Debug.LogWarning($"  Prefab not found: Resources/{PrefabResourcesPath}/{enemyName}");
                        continue;
                    }

                    waveSo.EnemyWaveEntries.Add(new EnemyWaveEntry
                    {
                        EnemyPrefab = prefab,
                        NumberOfEnemies = qty
                    });
                    Debug.Log($"  Added: {enemyName} x {qty}");

                }

                if (waveSo.EnemyWaveEntries.Count == 0)
                {
                    Debug.LogWarning($"Wave {waveIndex} has no valid entries, skipping asset.");
                    continue;
                }

                // --- after you build waveSo and confirm entries ---
                string assetPath = targetFolder + "/WaveConfig_" + waveIndex + ".asset";
                Debug.Log("About to CreateAsset at: " + assetPath);

                // 1) Create the asset
                AssetDatabase.CreateAsset(waveSo, assetPath);

                // 2) Mark dirty and save
                EditorUtility.SetDirty(waveSo);
                AssetDatabase.SaveAssets();

                // 3) Force import so Unity knows about it immediately
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
                Debug.Log("Imported asset: " + assetPath);

                // 4) Verify on disk
                string diskPath = Application.dataPath + assetPath.Substring("Assets".Length);
                bool existsOnDisk = File.Exists(diskPath);
                Debug.Log("File.Exists on disk? " + existsOnDisk + " (disk path: " + diskPath + ")");

            }

            AssetDatabase.SaveAssets();
            // 5) Force Unity to re-scan the Assets folder and show new files
            AssetDatabase.Refresh();
            Debug.Log("WaveConfig generation complete and AssetDatabase refreshed.");
        }
    }
}
