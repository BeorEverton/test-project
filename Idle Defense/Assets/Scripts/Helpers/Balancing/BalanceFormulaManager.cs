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
        if (File.Exists(persistent)) return;

        string streaming = GetStreamingPath();
        try
        {
            if (File.Exists(streaming))
            {
                File.Copy(streaming, persistent);
                Debug.Log("[BalanceFormulaManager] Created editable override at: " + persistent);
            }
            else
            {
                // If there is no streaming default, write a sane minimal default.
                string json = JsonUtility.ToJson(new BalanceFormulaConfig(), true);
                File.WriteAllText(persistent, json);
                Debug.Log("[BalanceFormulaManager] Created default formulas at: " + persistent);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[BalanceFormulaManager] Failed to create persistent override: " + ex.Message);
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
        return Mathf.Max(0f, Config.enemy.healthMultiplier.Evaluate(globalWaveIndex));
    }

    public float GetEnemyDamageMultiplier(int globalWaveIndex)
    {
        return Mathf.Max(0f, Config.enemy.damageMultiplier.Evaluate(globalWaveIndex));
    }

    public float GetEnemyCoinMultiplier(int globalWaveIndex)
    {
        return Mathf.Max(0f, Config.enemy.coinMultiplier.Evaluate(globalWaveIndex));
    }

    public float GetUpgradeCostMultiplier(int upgradeLevel)
    {
        return Mathf.Max(0f, Config.upgrades.costMultiplier.Evaluate(upgradeLevel));
    }
}
