using Assets.Scripts.WaveSystem;
using System.IO;
using UnityEngine;

public class BalanceFormulaManager : MonoBehaviour
{
    public static BalanceFormulaManager Instance { get; private set; }

    [Header("JSON source")]
    [Tooltip("If true, tries persistentDataPath first (tester override), then StreamingAssets.")]
    [SerializeField] private bool _preferPersistentOverride = true;

    [Tooltip("File name, e.g. balance_formulas.json")]
    [SerializeField] private string _fileName = "balance_formulas.json";

    public BalanceFormulaConfig Config { get; private set; } = new BalanceFormulaConfig();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        DontDestroyOnLoad(gameObject);
        Load();
    }

    public string GetPersistentPath()
    {
        return Path.Combine(Application.persistentDataPath, _fileName);
    }

    public string GetStreamingPath()
    {
        return Path.Combine(Application.streamingAssetsPath, _fileName);
    }

    public void EnsurePersistentOverrideExists()
    {
        string persistent = GetPersistentPath();
        string streaming = GetStreamingPath();

        try
        {
            if (!File.Exists(persistent))
            {
                if (File.Exists(streaming))
                {
                    File.Copy(streaming, persistent);
                    Debug.Log("[BalanceFormulaManager] Created editable override at: " + persistent);
                }
                else
                {
                    // No streaming default: write current code defaults
                    WriteCurrentConfigToPath(persistent, new BalanceFormulaConfig());
                    Debug.Log("[BalanceFormulaManager] Created default formulas at: " + persistent);
                }
            }

            // Always ensure the persistent file is up to date with current fields/defaults (help, missing formulas, etc.)
            SyncPersistentWithCurrentDefaults(persistent);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[BalanceFormulaManager] Failed to create/sync persistent override: " + ex.Message);
        }
    }

    private void SyncPersistentWithCurrentDefaults(string path)
    {
        try
        {
            if (!File.Exists(path)) return;

            string json = File.ReadAllText(path);
            var loaded = JsonUtility.FromJson<BalanceFormulaConfig>(json);

            // If file is corrupted or empty, rewrite defaults
            if (loaded == null)
            {
                WriteCurrentConfigToPath(path, new BalanceFormulaConfig());
                return;
            }

            bool changed = false;

            // Inject help if missing
            if (string.IsNullOrWhiteSpace(loaded.help))
            {
                loaded.help = new BalanceFormulaConfig().help;
                changed = true;
            }

            // Ensure formula objects exist (older JSON might omit them)
            if (loaded.enemy == null)
            {
                loaded.enemy = new BalanceFormulaConfig.EnemyGlobalFormula();
                changed = true;
            }
            if (loaded.upgrades == null)
            {
                loaded.upgrades = new BalanceFormulaConfig.UpgradeCostGlobalFormula();
                changed = true;
            }

            if (loaded.enemy.healthMultiplier == null) { loaded.enemy.healthMultiplier = FormulaDefinition.Constant(1f); changed = true; }
            if (loaded.enemy.damageMultiplier == null) { loaded.enemy.damageMultiplier = FormulaDefinition.Constant(1f); changed = true; }
            if (loaded.enemy.coinMultiplier == null) { loaded.enemy.coinMultiplier = FormulaDefinition.Constant(1f); changed = true; }
            if (loaded.upgrades.costMultiplier == null) { loaded.upgrades.costMultiplier = FormulaDefinition.Constant(1f); changed = true; }

            if (changed)
            {
                WriteCurrentConfigToPath(path, loaded);
                Debug.Log("[BalanceFormulaManager] Synced persistent formulas file with current defaults: " + path);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("[BalanceFormulaManager] Sync failed: " + ex.Message);
        }
    }

    private void WriteCurrentConfigToPath(string path, BalanceFormulaConfig cfg)
    {
        string json = JsonUtility.ToJson(cfg, true);
        File.WriteAllText(path, json);
    }


    private void TryInjectHelpBlock(string path)
    {
        try
        {
            if (!File.Exists(path)) return;

            string json = File.ReadAllText(path);
            var cfg = JsonUtility.FromJson<BalanceFormulaConfig>(json);

            if (cfg == null) return;

            // If missing/empty, replace with default help text from the class definition.
            if (string.IsNullOrWhiteSpace(cfg.help))
            {
                cfg.help = new BalanceFormulaConfig().help;
                File.WriteAllText(path, JsonUtility.ToJson(cfg, true));
                Debug.Log("[BalanceFormulaManager] Injected help block into: " + path);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("[BalanceFormulaManager] Could not inject help block: " + ex.Message);
        }
    }

    public void Load()
    {
        EnsurePersistentOverrideExists();

        string persistent = GetPersistentPath();
        string streaming = GetStreamingPath();

        // Prefer persistent override if it exists (technical testers edit this).
        string path = (_preferPersistentOverride && File.Exists(persistent)) ? persistent : streaming;

        if (!File.Exists(path))
        {
            Debug.LogWarning("[BalanceFormulaManager] No formula JSON found. Using defaults.");
            Config = new BalanceFormulaConfig();
            return;
        }

        try
        {
            string json = File.ReadAllText(path);
            var loaded = JsonUtility.FromJson<BalanceFormulaConfig>(json);
            Config = loaded ?? new BalanceFormulaConfig();
            Debug.Log("[BalanceFormulaManager] Loaded formulas from: " + path);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[BalanceFormulaManager] Failed to load formulas: " + ex.Message);
            Config = new BalanceFormulaConfig();
        }
    }

    public void ReloadFromDiskAndApply()
    {
        Load();

        // Rebuild current wave so ZoneManager recreates clones using new formulas.
        if (RuntimeBalanceTuningManager.Instance != null)
            RuntimeBalanceTuningManager.Instance.ReloadCurrentWave();
        else if (WaveManager.Instance != null)
            WaveManager.Instance.ForceRestartWave();
    }


    public float GetEnemyHealthMultiplier(int globalWaveIndex)
    {
        return NormalizeEnemyFormula(Config.enemy.healthMultiplier, globalWaveIndex);
    }

    public float GetEnemyDamageMultiplier(int globalWaveIndex)
    {
        return NormalizeEnemyFormula(Config.enemy.damageMultiplier, globalWaveIndex);
    }

    public float GetEnemyCoinMultiplier(int globalWaveIndex)
    {
        return NormalizeEnemyFormula(Config.enemy.coinMultiplier, globalWaveIndex);
    }

    private float NormalizeEnemyFormula(FormulaDefinition def, int globalWaveIndex)
    {
        if (def == null) return 1f;

        float w = Mathf.Max(1, globalWaveIndex);

        float raw = def.Evaluate(w);
        float baseAt1 = def.Evaluate(1f);

        // Avoid divide-by-zero / NaNs
        if (Mathf.Approximately(baseAt1, 0f)) return 1f;

        // Normalize so wave 1 is always exactly 1.0
        float normalized = raw / baseAt1;

        // Allow reducing difficulty (normalized can be < 1)
        // Clamp only extreme invalid negatives
        if (float.IsNaN(normalized) || float.IsInfinity(normalized)) return 1f;

        return Mathf.Max(0f, normalized);
    }


    public float GetUpgradeCostMultiplier(int upgradeLevel)
    {
        return Mathf.Max(0f, Config.upgrades.costMultiplier.Evaluate(upgradeLevel));
    }
}
