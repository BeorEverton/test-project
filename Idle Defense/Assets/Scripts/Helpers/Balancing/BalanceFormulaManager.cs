using Assets.Scripts.SO;
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
        // Upgrade costs are not normalized; allow values < 1 to reduce costs.
        if (Config == null || Config.upgrades == null || Config.upgrades.costMultiplier == null)
            return 1f;

        float v = Config.upgrades.costMultiplier.Evaluate(Mathf.Max(0, upgradeLevel));
        if (float.IsNaN(v) || float.IsInfinity(v)) return 1f;
        return Mathf.Max(0f, v);
    }

    public void SetEnemyHealthFormula(FormulaDefinition def)
    {
        if (Config.enemy == null) Config.enemy = new BalanceFormulaConfig.EnemyGlobalFormula();
        Config.enemy.healthMultiplier = def ?? FormulaDefinition.Constant(1f);
    }

    public void SetEnemyDamageFormula(FormulaDefinition def)
    {
        if (Config.enemy == null) Config.enemy = new BalanceFormulaConfig.EnemyGlobalFormula();
        Config.enemy.damageMultiplier = def ?? FormulaDefinition.Constant(1f);
    }

    public void SetEnemyCoinFormula(FormulaDefinition def)
    {
        if (Config.enemy == null) Config.enemy = new BalanceFormulaConfig.EnemyGlobalFormula();
        Config.enemy.coinMultiplier = def ?? FormulaDefinition.Constant(1f);
    }

    public void SetUpgradeCostFormula(FormulaDefinition def)
    {
        if (Config.upgrades == null) Config.upgrades = new BalanceFormulaConfig.UpgradeCostGlobalFormula();
        Config.upgrades.costMultiplier = def ?? FormulaDefinition.Constant(1f);
    }

    /// <summary>
    /// This is the critical missing piece:
    /// "Apply" must force the wave to rebuild so ZoneManager recreates the scaled EnemyInfoSO clones
    /// using the new formulas.
    /// </summary>
    public void ApplyCurrentFormulasNow(bool restartWave = true)
    {
        // Optional: keep file in sync so testers can see current state (even if they don't hit Save).
        // Comment out if you only want disk writes when Save is clicked.
        TryWriteCurrentToPersistentWithoutSpamming();

        if (!restartWave) return;

        if (RuntimeBalanceTuningManager.Instance != null)
        {
            RuntimeBalanceTuningManager.Instance.ReloadCurrentWave();
            return;
        }

        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.ForceRestartWave();
            return;
        }

        Debug.LogWarning("[BalanceFormulaManager] ApplyCurrentFormulasNow: no WaveManager/RuntimeBalanceTuningManager found.");
    }

    public void SaveCurrentConfigToPersistent()
    {
        EnsurePersistentOverrideExists();
        string path = GetPersistentPath();
        try
        {
            // Ensure help text exists before writing.
            if (string.IsNullOrWhiteSpace(Config.help))
                Config.help = new BalanceFormulaConfig().help;

            File.WriteAllText(path, JsonUtility.ToJson(Config, true));
            Debug.Log("[BalanceFormulaManager] Saved formulas to: " + path);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[BalanceFormulaManager] Save failed: " + ex.Message);
        }
    }

    public string ExportCurrentConfigToJson()
    {
        if (Config == null) Config = new BalanceFormulaConfig();
        if (string.IsNullOrWhiteSpace(Config.help))
            Config.help = new BalanceFormulaConfig().help;

        return JsonUtility.ToJson(Config, true);
    }

    private float _lastAutoWriteTime;
    private void TryWriteCurrentToPersistentWithoutSpamming()
    {
        // Avoid writing every click/keystroke; only write at most once per second.
        if (Time.unscaledTime - _lastAutoWriteTime < 1f) return;
        _lastAutoWriteTime = Time.unscaledTime;

        try
        {
            EnsurePersistentOverrideExists();
            string path = GetPersistentPath();
            File.WriteAllText(path, JsonUtility.ToJson(Config, true));
        }
        catch
        {
            // Silent: auto-write is optional convenience, not required for gameplay.
        }
    }


    public void ApplyCurrentFormulasNow()
    {
        // Forces enemies to be rebuilt with the new formulas.
        if (RuntimeBalanceTuningManager.Instance != null)
            RuntimeBalanceTuningManager.Instance.ReloadCurrentWave();
        else if (WaveManager.Instance != null)
            WaveManager.Instance.ForceRestartWave();

        // Upgrade costs: TurretUpgradeManager reads formula multiplier when it recalculates costs.
        // If you have a "refresh UI" call in your upgrade UI, call it from your UI bridge.
    }

    /// <summary>
    /// Apply the current enemy formula multipliers directly to a runtime Wave instance.
    /// Call this AFTER any other runtime tuning that might rebuild/override stats.
    /// </summary>
    public void ApplyToWave(Wave wave, int globalWaveIndex)
    {
        if (wave == null || wave.WaveEnemies == null || wave.WaveEnemies.Count == 0) return;

        float hMul = GetEnemyHealthMultiplier(globalWaveIndex);
        float dMul = GetEnemyDamageMultiplier(globalWaveIndex);
        float cMul = GetEnemyCoinMultiplier(globalWaveIndex);

        // Defensive: avoid weird negatives/NaNs.
        if (float.IsNaN(hMul) || float.IsInfinity(hMul) || hMul < 0f) hMul = 1f;
        if (float.IsNaN(dMul) || float.IsInfinity(dMul) || dMul < 0f) dMul = 1f;
        if (float.IsNaN(cMul) || float.IsInfinity(cMul) || cMul < 0f) cMul = 1f;

        foreach (var kvp in wave.WaveEnemies)
        {
            EnemyInfoSO info = kvp.Value;
            if (info == null) continue;

            info.MaxHealth *= hMul;
            info.Damage *= dMul;

            double coins = info.CoinDropAmount * (double)cMul;
            if (coins > ulong.MaxValue) coins = ulong.MaxValue;
            if (coins < 0) coins = 0;
            info.CoinDropAmount = (ulong)System.Math.Ceiling(coins);
        }
    }
}
