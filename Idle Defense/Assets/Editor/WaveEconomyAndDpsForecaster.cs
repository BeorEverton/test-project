// File: Assets/Editor/WaveEconomyAndDpsForecaster.cs
// Purpose: Data-accurate simulation of wave economy (Scraps/BlackSteel) and DPS requirements.
// Mirrors your WaveManager/EnemySpawner logic and your Z-axis world (spawn z=50 -> base z≈0).
// ASCII only. Unity 2020+ compatible.

#if UNITY_EDITOR
using Assets.Scripts.SO;            // WaveConfigSO, EnemyInfoSO
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public class WaveEconomyAndDpsForecaster : EditorWindow
{
    [Serializable]
    public class BaseWaveSet
    {
        public WaveConfigSO WaveConfig;   // Matches WaveManager _baseWaveSOs usage
    }

    // ---- Inputs: Data & Ranges ----
    [Header("Wave Data")]
    [SerializeField] private List<BaseWaveSet> baseWaves = new List<BaseWaveSet>();
    [SerializeField] private int wavesToSimulate = 100;

    [Header("Prestige / Global Multipliers")]
    [SerializeField] private float enemyCountMultiplier = 1f;      // matches PrestigeManager.GetEnemyCountMultiplier()
    [SerializeField] private float enemyHealthMultiplier = 1f;     // matches PrestigeManager.GetEnemyHealthMultiplier()
    [SerializeField] private float scrapsGainMultiplier = 1f;      // matches GetScrapsGainMultiplier()
    [SerializeField] private float blackSteelGainMultiplier = 1f;  // matches GetBlackSteelGainMultiplier()

    [Header("Black Steel (Normals)")]
    [SerializeField, Tooltip("Per-kill chance on normal enemies to drop BS = round(scraps/5) * chance")]
    private float normalBlackSteelChance = 1f / 1000f;

    [Header("Spawn / Travel (Z-axis world)")]
    [SerializeField, Tooltip("Spawn depth Z (default=50). Matches EnemyConfig.EnemySpawnDepth.")]
    private float spawnDepthZ = 50f;
    [SerializeField, Tooltip("How quickly enemies advance in Z relative to MovementSpeed (0..1). Head-on≈1; midline≈0.8; conservative≈0.6")]
    private float forwardZFactor = 0.80f;  // encodes diagonal paths + steering
    [SerializeField, Tooltip("Pick the effective forward model")]
    private TravelModel travelModel = TravelModel.Midline;

    public enum TravelModel { HeadOn, Midline, Conservative }

    [Header("Boss / Mini-boss Rules (from EnemySpawner)")]
    [SerializeField] private bool useSpawnerDefaults = true;
    // Mini-boss
    [SerializeField] private float miniBossHPPerWave = 50f;    // min(w*50, 500) except w=5 special
    [SerializeField] private float miniBossDMGPerWave = 10f;   // min(w*10,100)  except w=5 special
    [SerializeField] private float miniBossCoinPerWave = 10f;  // min(w*10,100)  except w=5 special
    [SerializeField] private float miniBossSpecialWave5_HP = 30f;
    [SerializeField] private float miniBossSpecialWave5_DMG = 15f;
    [SerializeField] private float miniBossSpecialWave5_COIN = 20f;
    // Boss
    [SerializeField] private float bossHPPerWave = 100f;       // min(w*100, 1000) except w=10 special
    [SerializeField] private float bossDMGPerWave = 20f;       // min(w*20, 500)  except w=10 special
    [SerializeField] private float bossCoinPerWave = 20f;      // min(w*20, 500)  except w=10 special
    [SerializeField] private float bossSpecialWave10_HP = 60f;
    [SerializeField] private float bossSpecialWave10_DMG = 25f;
    [SerializeField] private float bossSpecialWave10_COIN = 45f;

    [Header("Output")]
    [SerializeField] private string csvOutputPath = "Assets/WaveForecast.csv";

    private Vector2 scroll;

    // ===== Auto-sync sources for Base Waves =====
    private enum WaveSourceMode { ManualList, FromWaveManager, FromAssets }
    [SerializeField] private WaveSourceMode waveSource = WaveSourceMode.FromWaveManager;

    // Optional: link to the WaveManager in the scene (auto-detected if left null)
    [SerializeField] private MonoBehaviour waveManagerRef; // expect your WaveManager component here

    // Internal cache used at simulate time
    private readonly List<WaveConfigSO> _activeWaveList = new List<WaveConfigSO>();

    private static readonly string[] CandidateListFieldNames = {
    "BaseWaveSOs","_baseWaveSOs","baseWaveSOs","BaseWaves","_baseWaves","baseWaves"
};

    [MenuItem("Tools/Idle Defense/Wave Economy & DPS Forecaster")]
    public static void Open()
    {
        var w = GetWindow<WaveEconomyAndDpsForecaster>("Wave Forecaster");
        w.minSize = new Vector2(820, 560);
        w.Show();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Wave Economy & DPS Forecaster", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // Wave data
        DrawWaveSourceUI();

        // In OnGUI(), after DrawWaveSourceUI(); and before 'Waves To Simulate'
        EditorGUILayout.Space();
        int activeCount = _activeWaveList != null ? _activeWaveList.Count : 0;
        EditorGUILayout.HelpBox($"Active Wave Configs: {activeCount}  (Source: {waveSource})", MessageType.None);


        wavesToSimulate = Mathf.Max(1, EditorGUILayout.IntField("Waves To Simulate", wavesToSimulate));

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Prestige / Global Multipliers", EditorStyles.boldLabel);
        enemyCountMultiplier = EditorGUILayout.FloatField("Enemy Count Multiplier", Mathf.Max(0f, enemyCountMultiplier));
        enemyHealthMultiplier = EditorGUILayout.FloatField("Enemy Health Multiplier", Mathf.Max(0f, enemyHealthMultiplier));
        scrapsGainMultiplier = EditorGUILayout.FloatField("Scraps Gain Multiplier", Mathf.Max(0f, scrapsGainMultiplier));
        blackSteelGainMultiplier = EditorGUILayout.FloatField("Black Steel Gain Multiplier", Mathf.Max(0f, blackSteelGainMultiplier));

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Black Steel (Normals)", EditorStyles.boldLabel);
        normalBlackSteelChance = EditorGUILayout.Slider("Chance per Kill", normalBlackSteelChance, 0f, 1f);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Spawn / Travel (Z axis)", EditorStyles.boldLabel);
        travelModel = (TravelModel)EditorGUILayout.EnumPopup("Forward Model", travelModel);
        switch (travelModel)
        {
            case TravelModel.HeadOn: forwardZFactor = 1.0f; break;
            case TravelModel.Midline: forwardZFactor = 0.80f; break;
            case TravelModel.Conservative: forwardZFactor = 0.60f; break;
        }
        spawnDepthZ = EditorGUILayout.FloatField("Spawn Depth Z", spawnDepthZ);
        forwardZFactor = EditorGUILayout.Slider("Effective Z Factor", forwardZFactor, 0.1f, 1f);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Boss / Mini-boss Rules (from EnemySpawner)", EditorStyles.boldLabel);
        useSpawnerDefaults = EditorGUILayout.ToggleLeft("Use EnemySpawner Defaults", useSpawnerDefaults);
        using (new EditorGUI.DisabledScope(useSpawnerDefaults))
        {
            EditorGUILayout.LabelField("Mini-boss", EditorStyles.miniBoldLabel);
            miniBossSpecialWave5_HP = EditorGUILayout.FloatField("W5 HP x", miniBossSpecialWave5_HP);
            miniBossSpecialWave5_DMG = EditorGUILayout.FloatField("W5 DMG x", miniBossSpecialWave5_DMG);
            miniBossSpecialWave5_COIN = EditorGUILayout.FloatField("W5 COIN x", miniBossSpecialWave5_COIN);
            miniBossHPPerWave = EditorGUILayout.FloatField("HP per Wave (cap 500)", miniBossHPPerWave);
            miniBossDMGPerWave = EditorGUILayout.FloatField("DMG per Wave (cap 100)", miniBossDMGPerWave);
            miniBossCoinPerWave = EditorGUILayout.FloatField("COIN per Wave (cap 100)", miniBossCoinPerWave);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Boss", EditorStyles.miniBoldLabel);
            bossSpecialWave10_HP = EditorGUILayout.FloatField("W10 HP x", bossSpecialWave10_HP);
            bossSpecialWave10_DMG = EditorGUILayout.FloatField("W10 DMG x", bossSpecialWave10_DMG);
            bossSpecialWave10_COIN = EditorGUILayout.FloatField("W10 COIN x", bossSpecialWave10_COIN);
            bossHPPerWave = EditorGUILayout.FloatField("HP per Wave (cap 1000)", bossHPPerWave);
            bossDMGPerWave = EditorGUILayout.FloatField("DMG per Wave (cap 500)", bossDMGPerWave);
            bossCoinPerWave = EditorGUILayout.FloatField("COIN per Wave (cap 500)", bossCoinPerWave);
        }

        EditorGUILayout.Space();
        csvOutputPath = EditorGUILayout.TextField("CSV Output Path", csvOutputPath);

        EditorGUILayout.Space();
        if (GUILayout.Button("Simulate And Export CSV", GUILayout.Height(40)))
        {
            try
            {
                BuildActiveWaveList();
                var rows = Simulate();
                WriteCsv(rows, csvOutputPath);
                EditorUtility.DisplayDialog("Wave Forecaster", $"CSV exported:\n{csvOutputPath}", "OK");
                AssetDatabase.Refresh();
            }
            catch (Exception ex)
            {
                Debug.LogError("[WaveForecaster] " + ex);
                EditorUtility.DisplayDialog("Error", ex.Message, "OK");
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "This tool mirrors your current WaveManager scaling, EnemySpawner boss rules, and EnemyInfoSO fields.\n" +
            "- Counts: baseCount + wave (x Count Multiplier)\n" +
            "- Health (plateaued): wave-indexed spike + linear plateau, x Health Multiplier\n" +
            "- Damage (mild exponential): +20% per 10 waves\n" +
            "- Coins: base*coinMult + wave*coinMult, x Scraps multiplier on drop\n" +
            "- Boss/miniboss: 5/10 specials, per-wave caps\n" +
            "- Travel/DPS: from z=spawnDepth to AttackRange, using effective Z factor.",
            MessageType.Info);
    }

    private void DrawWaveSourceUI()
    {
        EditorGUILayout.LabelField("Base Waves Source", EditorStyles.boldLabel);
        waveSource = (WaveSourceMode)EditorGUILayout.EnumPopup("Source", waveSource);

        if (waveSource == WaveSourceMode.ManualList)
        {
            EditorGUILayout.LabelField("Manual Base Wave Configs (ordered like WaveManager)", EditorStyles.miniBoldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("+", GUILayout.Width(28))) baseWaves.Add(new BaseWaveSet());
                if (GUILayout.Button("Clear", GUILayout.Width(60))) baseWaves.Clear();
            }
            for (int i = 0; i < baseWaves.Count; i++)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    baseWaves[i].WaveConfig = (WaveConfigSO)EditorGUILayout.ObjectField($"Base Wave [{i}]", baseWaves[i].WaveConfig, typeof(WaveConfigSO), false);
                    if (GUILayout.Button("X", GUILayout.Width(28))) { baseWaves.RemoveAt(i); break; }
                }
            }
        }
        else if (waveSource == WaveSourceMode.FromWaveManager)
        {
            EditorGUILayout.HelpBox("Reads the serialized BaseWave list from the selected WaveManager (same list your game uses).", MessageType.Info);
            waveManagerRef = (MonoBehaviour)EditorGUILayout.ObjectField("WaveManager (Scene)", waveManagerRef, typeof(MonoBehaviour), true);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Auto-Detect In Scene"))
                {
                    waveManagerRef = FindWaveManagerInScene();
                }
                if (GUILayout.Button("Preview List"))
                {
                    var list = TryReadBaseWavesFromManager(waveManagerRef);
                    ShowPreviewDialog(list);
                }
            }
        }
        else // FromAssets
        {
            EditorGUILayout.HelpBox("Finds ALL WaveConfigSO assets in the project and sorts by WaveStartIndex (ascending).", MessageType.Info);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Preview Project Scan"))
                {
                    var list = ScanProjectWaveConfigs();
                    ShowPreviewDialog(list);
                }
            }
        }
    }

    private MonoBehaviour FindWaveManagerInScene()
    {
        // Try to find a component named "WaveManager" in the scene
        var all = GameObject.FindObjectsOfType<MonoBehaviour>();
        foreach (var m in all)
        {
            if (m == null) continue;
            if (m.GetType().Name == "WaveManager") return m;
        }
        return null;
    }

    private List<WaveConfigSO> TryReadBaseWavesFromManager(MonoBehaviour wm)
    {
        var result = new List<WaveConfigSO>();
        if (wm == null) return result;

        // Use SerializedObject to read the list, trying several common field names
        var so = new SerializedObject(wm);
        SerializedProperty found = null;
        foreach (var name in CandidateListFieldNames)
        {
            var p = so.FindProperty(name);
            if (p != null && p.isArray) { found = p; break; }
        }

        if (found == null)
        {
            // Fallback via reflection for private fields
            var t = wm.GetType();
            foreach (var name in CandidateListFieldNames)
            {
                var f = t.GetField(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                if (f != null)
                {
                    var obj = f.GetValue(wm) as System.Collections.IList;
                    if (obj != null)
                    {
                        foreach (var e in obj)
                        {
                            var w = e as WaveConfigSO;
                            if (w != null) result.Add(w);
                        }
                        break;
                    }
                }
            }
            return result;
        }

        // Serialized path
        for (int i = 0; i < found.arraySize; i++)
        {
            var elem = found.GetArrayElementAtIndex(i);
            var obj = elem.objectReferenceValue as WaveConfigSO;
            if (obj != null) result.Add(obj);
        }
        return result;
    }

    private List<WaveConfigSO> ScanProjectWaveConfigs()
    {
        var list = new List<WaveConfigSO>();
        var guids = AssetDatabase.FindAssets("t:WaveConfigSO");
        foreach (var g in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(g);
            var obj = AssetDatabase.LoadAssetAtPath<WaveConfigSO>(path);
            if (obj != null) list.Add(obj);
        }
        // Sort by WaveStartIndex ascending
        list.Sort((a, b) => a.WaveStartIndex.CompareTo(b.WaveStartIndex));
        return list;
    }

    private void ShowPreviewDialog(List<WaveConfigSO> list)
    {
        if (list == null || list.Count == 0)
        {
            EditorUtility.DisplayDialog("Preview", "No WaveConfigSO found.", "OK");
            return;
        }
        var sb = new StringBuilder();
        sb.AppendLine($"Found {list.Count} WaveConfigSO:");
        for (int i = 0; i < list.Count; i++)
            sb.AppendLine($"{i}. {list[i].name} (Start {list[i].WaveStartIndex})");
        EditorUtility.DisplayDialog("Preview", sb.ToString(), "OK");
    }

    private struct WaveOutput
    {
        public int Wave;
        public string WaveType;                   // Normal / MiniBoss / Boss
        public int TotalEnemies;
        public double TotalWaveHP;
        public double TotalWaveDamagePerSecAtBase; // informational from attack speeds? (optional extension)
        public double AvgScrapsPerKill;
        public double TotalScraps;
        public double EV_BS_from_Normals;
        public double Guaranteed_BS;             // boss/miniboss only
        public double Total_BlackSteel;          // EV normals + guaranteed
        public double TimeBetweenSpawns;
        public double AvgTravelTime;
        public double SustainedDPS_Needed;       // totalHP / waveDuration
        public double FocusDPS_Needed;           // max_i (HP_i / t_i)
        public double Cumulative_Scraps;
        public double Cumulative_BlackSteel;
    }

    private List<WaveOutput> Simulate()
    {
        // We now rely on _activeWaveList built by BuildActiveWaveList()
        if (_activeWaveList == null || _activeWaveList.Count == 0)
            throw new InvalidOperationException(
                "No Base Wave Configs available. Use 'Auto-Detect In Scene' or switch Source to 'From Assets' or 'Manual List', then click Simulate."
            );


        var outputs = new List<WaveOutput>(wavesToSimulate);
        double cumScraps = 0, cumBS = 0;

        for (int w = 1; w <= wavesToSimulate; w++)
        {
            // Select base wave config like WaveManager.GetBasicWaveConfigSo
            var baseWave = PickBaseWaveFor(w);

            // Build entries for this wave: counts = baseCount + wave, then scale stats (CloneEnemyInfoWithScale mirror)
            var perEnemyHP = new List<double>();
            var perEnemyScraps = new List<double>();
            var perEnemyTravelTime = new List<double>();

            int totalEnemies = 0;
            double sumScraps = 0;
            double sumHP = 0;
            double sumEVBSNormals = 0;

            // Track average per-kill for boss surrogate
            double avgScrapsPerKill = 0;
            double avgHPPerKill = 0;
            int types = 0;

            foreach (var entry in baseWave.EnemyWaveEntries)
            {
                if (entry == null || entry.EnemyPrefab == null) continue;
                var enemy = entry.EnemyPrefab.GetComponent<Assets.Scripts.Enemies.Enemy>();
                if (enemy == null || enemy.Info == null) continue;

                var infoBase = enemy.Info;

                // --- Mirror WaveManager.CloneEnemyInfoWithScale (current version) ---
                float baseHealth = infoBase.MaxHealth;
                int bossInterval = 10;
                int bossIndex = w / bossInterval;
                int plateauBaseWave = bossIndex * bossInterval;
                float spikeMultiplier = Mathf.Pow(1.25f, bossIndex);
                float plateauMultiplier = 1f + ((w - plateauBaseWave) * 0.01f);
                float wavePower = Mathf.Pow(plateauBaseWave > 0 ? plateauBaseWave : 1f, 1.3f);

                // health
                double hp = baseHealth * wavePower * spikeMultiplier * plateauMultiplier;
                hp *= enemyHealthMultiplier; // prestige health mult

                // damage (mild exponential)
                double dmg = infoBase.Damage + infoBase.Damage * (bossIndex * 0.2f); // +20% per 10 waves

                // coins before multipliers: base*mult + w*mult
                double coinBase = infoBase.CoinDropAmount * infoBase.CoinDropMultiplierByWaveCount;
                double coinBonus = w * infoBase.CoinDropMultiplierByWaveCount;
                double scrapsPerKillRaw = coinBase + coinBonus;

                // APPLY SCRAPS MULTIPLIER, THEN FLOOR TO INT PER KILL (no fractional carry)
                int scrapsPerKillInt = Mathf.FloorToInt((float)(scrapsPerKillRaw * scrapsGainMultiplier));

                // count per entry
                int count = Mathf.Max(0, Mathf.RoundToInt((entry.NumberOfEnemies + w) * enemyCountMultiplier));

                // travel time estimate to attackRange
                double travelTime = EstimateTravelTime(spawnDepthZ, infoBase.AttackRange, infoBase.MovementSpeed);

                // Aggregate
                totalEnemies += count;
                sumHP += hp * count;
                // total scraps sums the integer per-kill value
                sumScraps += (double)scrapsPerKillInt * count;

                // EV BS normals uses the INTEGER scraps per kill (game rolls from the int drop)
                // EV per kill = round(intScraps/5) * chance * BS mult  (fractional EV allowed)
                double evBSPerKill = Math.Round(scrapsPerKillInt / 5.0) * normalBlackSteelChance * blackSteelGainMultiplier;
                sumEVBSNormals += evBSPerKill * count;

                // detail lists for DPS stats
                for (int i = 0; i < count; i++)
                {
                    perEnemyHP.Add(hp);
                    perEnemyScraps.Add(scrapsPerKillInt);   // keep for debugging/inspection
                    perEnemyTravelTime.Add(travelTime);
                }

                // Track averages based on INTEGER scraps per kill
                avgScrapsPerKill += scrapsPerKillInt;
                avgHPPerKill += hp;
                types++;

            }

            double avgScraps = types > 0 ? avgScrapsPerKill / types : 0.0;
            double avgHP = types > 0 ? avgHPPerKill / types : 0.0;

            // Boss / Miniboss
            bool isBoss = (w % 10 == 0);
            bool isMini = (!isBoss && w % 5 == 0);
            string waveType = isBoss ? "Boss" : (isMini ? "MiniBoss" : "Normal");
            double guaranteedBS = 0;

            if (isBoss || isMini)
            {
                // approximate boss based on average per-kill (same approach as your spawner picks a random base enemy)
                double bossHPx, bossDMGx, bossCOINx;
                if (useSpawnerDefaults)
                {
                    if (isMini)
                    {
                        if (w == 5) { bossHPx = 30; bossDMGx = 15; bossCOINx = 20; }
                        else { bossHPx = Math.Min(w * 50.0, 500.0); bossDMGx = Math.Min(w * 10.0, 100.0); bossCOINx = Math.Min(w * 10.0, 100.0); }
                    }
                    else
                    {
                        if (w == 10) { bossHPx = 60; bossDMGx = 25; bossCOINx = 45; }
                        else { bossHPx = Math.Min(w * 100.0, 1000.0); bossDMGx = Math.Min(w * 20.0, 500.0); bossCOINx = Math.Min(w * 20.0, 500.0); }
                    }
                }
                else
                {
                    if (isMini)
                    {
                        if (w == 5) { bossHPx = miniBossSpecialWave5_HP; bossDMGx = miniBossSpecialWave5_DMG; bossCOINx = miniBossSpecialWave5_COIN; }
                        else { bossHPx = Math.Min(w * miniBossHPPerWave, 500.0); bossDMGx = Math.Min(w * miniBossDMGPerWave, 100.0); bossCOINx = Math.Min(w * miniBossCoinPerWave, 100.0); }
                    }
                    else
                    {
                        if (w == 10) { bossHPx = bossSpecialWave10_HP; bossDMGx = bossSpecialWave10_DMG; bossCOINx = bossSpecialWave10_COIN; }
                        else { bossHPx = Math.Min(w * bossHPPerWave, 1000.0); bossDMGx = Math.Min(w * bossDMGPerWave, 500.0); bossCOINx = Math.Min(w * bossCoinPerWave, 500.0); }
                    }
                }

                // Apply to avg enemy
                // ---- Boss / Mini-boss block ----
                // Use averages built from INTEGER per-kill scraps
                double bossHP = avgHP * bossHPx;

                // Boss/miniboss coin spike applied to INT-based average scraps
                // Make boss scraps an INT, then guaranteed BS is INT as well
                int bossScrapsInt = Mathf.RoundToInt((float)(avgScraps * bossCOINx));
                int guaranteedBSInt = Mathf.RoundToInt(bossScrapsInt / 2f);

                // Boss guaranteed BS does NOT add any fractional EV (no normal BS chance on boss)
                guaranteedBS = guaranteedBSInt * blackSteelGainMultiplier;  // multiplier keeps it integer if =1; if you prefer, cast to (double)guaranteedBSInt


                // inset: if you want boss HP to also affect DPSNeeded (one more target),
                // you could append one more perEnemyHP and a travel time ~ centered X:
                perEnemyHP.Add(bossHP);
                perEnemyScraps.Add(bossScrapsInt);
                // Boss spawns centrally; head-on-ish travel:                
                perEnemyTravelTime.Add(EstimateTravelTime(spawnDepthZ, /*avg*/ 0.6f, /*speed*/  AvgTravelSpeedFallback(perEnemyTravelTime, 1f))); // optional


            }

            // DPS requirements
            double avgTravelTime = 0;
            foreach (var t in perEnemyTravelTime) avgTravelTime += t;
            avgTravelTime = perEnemyTravelTime.Count > 0 ? avgTravelTime / perEnemyTravelTime.Count : 0;

            // Focus: kill strongest (HP/time) before it reaches base
            double focusDPSNeeded = 0;
            for (int i = 0; i < perEnemyHP.Count; i++)
            {
                double t = Math.Max(0.05, perEnemyTravelTime[i]);
                focusDPSNeeded = Math.Max(focusDPSNeeded, perEnemyHP[i] / t);
            }

            // Sustained: total HP / (wave duration)
            double spawnInterval = baseWave.TimeBetweenSpawns;
            double waveDuration = (totalEnemies > 0) ? ((totalEnemies - 1) * spawnInterval + avgTravelTime) : 0.1;
            double sustainedDPSNeeded = (waveDuration > 0.05) ? (sumHP / waveDuration) : sumHP;

            // Totals
            double totalBS = sumEVBSNormals + guaranteedBS;
            cumScraps += sumScraps;
            cumBS += totalBS;

            outputs.Add(new WaveOutput
            {
                Wave = w,
                WaveType = waveType,
                TotalEnemies = totalEnemies,
                TotalWaveHP = sumHP,
                TotalWaveDamagePerSecAtBase = 0, // optional extension
                AvgScrapsPerKill = avgScraps,
                TotalScraps = sumScraps,
                EV_BS_from_Normals = sumEVBSNormals,
                Guaranteed_BS = guaranteedBS,
                Total_BlackSteel = totalBS,
                TimeBetweenSpawns = baseWave.TimeBetweenSpawns,
                AvgTravelTime = avgTravelTime,
                SustainedDPS_Needed = sustainedDPSNeeded,
                FocusDPS_Needed = focusDPSNeeded,
                Cumulative_Scraps = cumScraps,
                Cumulative_BlackSteel = cumBS
            });
        }

        return outputs;
    }

    private void BuildActiveWaveList()
    {
        _activeWaveList.Clear();

        if (waveSource == WaveSourceMode.FromWaveManager)
        {
            if (waveManagerRef == null) waveManagerRef = FindWaveManagerInScene();
            var list = TryReadBaseWavesFromManager(waveManagerRef);
            if (list != null && list.Count > 0) _activeWaveList.AddRange(list);
        }
        else if (waveSource == WaveSourceMode.FromAssets)
        {
            var list = ScanProjectWaveConfigs();
            if (list != null && list.Count > 0) _activeWaveList.AddRange(list);
        }
        else // Manual
        {
            foreach (var b in baseWaves)
                if (b != null && b.WaveConfig != null) _activeWaveList.Add(b.WaveConfig);
        }

        // Sort by WaveStartIndex ascending to replicate WaveManager selection
        _activeWaveList.Sort((a, b) => a.WaveStartIndex.CompareTo(b.WaveStartIndex));

        if (_activeWaveList.Count == 0)
            throw new InvalidOperationException("No WaveConfigSO found. Set Source or add at least one Base Wave.");
    }

    private WaveConfigSO PickBaseWaveFor(int wave)
    {
        if (_activeWaveList.Count == 0) BuildActiveWaveList();

        WaveConfigSO chosen = null;
        for (int i = 0; i < _activeWaveList.Count; i++)
        {
            var w = _activeWaveList[i];
            if (w == null) continue;
            if (w.WaveStartIndex <= wave) chosen = w;
            else break;
        }
        if (chosen == null) throw new InvalidOperationException($"No WaveConfigSO with WaveStartIndex <= {wave}.");
        return chosen;
    }


    private double EstimateTravelTime(float spawnZ, float attackRange, float moveSpeed)
    {
        float zDistance = Mathf.Max(0f, spawnZ - attackRange);
        float effSpeedZ = Mathf.Max(0.01f, moveSpeed * forwardZFactor);
        return zDistance / effSpeedZ;
    }

    private float AvgTravelSpeedFallback(List<double> times, float defaultSpeed = 1f)
    {
        // times are durations (s). We invert the average time as a rough "units/sec" fallback.
        if (times == null || times.Count == 0) return defaultSpeed;

        double sum = 0.0;
        for (int i = 0; i < times.Count; i++) sum += times[i];

        double avgTime = sum / Math.Max(1, times.Count);
        if (avgTime <= 0.0001) return defaultSpeed;

        // 1 / avgTime ≈ average rate; clamp to a sane minimum
        return Mathf.Max(0.01f, (float)(1.0 / avgTime));
    }

    private void WriteCsv(List<WaveOutput> rows, string path)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Wave,Type,Enemies,TotalHP,AvgScrapsPerKill,TotalScraps,EV_BS_from_Normals,Guaranteed_BS,Total_Black_Steel,TimeBetweenSpawns,AvgTravelTime,SustainedDPS_Needed,FocusDPS_Needed,Cumulative_Scraps,Cumulative_BlackSteel");
        foreach (var r in rows)
        {
            sb.AppendLine(string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                "{0},{1},{2},{3:F3},{4:F3},{5:F3},{6:F3},{7:F3},{8:F3},{9:F3},{10:F3},{11:F3},{12:F3},{13:F3},{14:F3}",
                r.Wave, r.WaveType, r.TotalEnemies, r.TotalWaveHP,
                r.AvgScrapsPerKill, r.TotalScraps, r.EV_BS_from_Normals, r.Guaranteed_BS,
                r.Total_BlackSteel, r.TimeBetweenSpawns, r.AvgTravelTime,
                r.SustainedDPS_Needed, r.FocusDPS_Needed, r.Cumulative_Scraps, r.Cumulative_BlackSteel
            ));
        }

        var dir = Path.GetDirectoryName(path);
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
    }
}
#endif
