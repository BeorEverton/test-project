using Assets.Scripts.Enemies;
using Assets.Scripts.SO;
using Assets.Scripts.WaveSystem;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BalanceUIBridge : MonoBehaviour
{
    [Header("Refs")]
    public RuntimeBalanceTuningManager tuning;
    public BalanceHUD hud;

    [Header("Inputs")]
    public Slider enemyBaseHealthMul;
    public Slider enemyDamageMul;
    public Slider enemyMoveSpeedMul;
    public Slider enemyCoinDropMul;

    public Slider extraEnemiesPerWave;
    public Slider maxEnemiesPerWave;

    [Header("HUD Text")]
    public TMP_Text spawnedText;
    public TMP_Text aliveText;
    public TMP_Text timeToBaseText;

    private float _baseEnemyBaseHealthMul;
    private float _baseEnemyDamageMul;
    private float _baseEnemyMoveSpeedMul;
    private float _baseEnemyCoinDropMul;

    public TMP_Text enemyBaseHealthBaseText;
    public TMP_Text enemyBaseHealthValueText;

    public TMP_Text enemyDamageBaseText;
    public TMP_Text enemyDamageValueText;

    public TMP_Text enemyMoveSpeedBaseText;
    public TMP_Text enemyMoveSpeedValueText;

    public TMP_Text enemyCoinDropBaseText;
    public TMP_Text enemyCoinDropValueText;

    private float _baseExtraEnemiesPerWave;
    private int _baseMaxEnemiesPerWave;

    public TMP_Text extraEnemiesBaseText;
    public TMP_Text extraEnemiesValueText;

    public TMP_Text maxEnemiesBaseText;
    public TMP_Text maxEnemiesValueText;


    public TMP_Text waveEnemyListText;

    private void Start()
    {
        if (tuning == null) return;

        // Multipliers: 10%..300% of base, start at base
        SetupMulSlider(enemyBaseHealthMul, tuning.active.enemyBaseHealthMul, out _baseEnemyBaseHealthMul, enemyBaseHealthBaseText, enemyBaseHealthValueText);
        SetupMulSlider(enemyDamageMul, tuning.active.enemyDamageMul, out _baseEnemyDamageMul, enemyDamageBaseText, enemyDamageValueText);
        SetupMulSlider(enemyMoveSpeedMul, tuning.active.enemyMoveSpeedMul, out _baseEnemyMoveSpeedMul, enemyMoveSpeedBaseText, enemyMoveSpeedValueText);
        SetupMulSlider(enemyCoinDropMul, tuning.active.enemyCoinDropMul, out _baseEnemyCoinDropMul, enemyCoinDropBaseText, enemyCoinDropValueText);

        // Enemies per wave (extra): 10%..300% of base, start at base
        SetupIntLikeSlider(extraEnemiesPerWave, tuning.active.extraEnemiesPerWave, out _baseExtraEnemiesPerWave, extraEnemiesBaseText, extraEnemiesValueText, showPlus: true);

        // Max enemies per wave (cap): 10%..300% of base, start at base
        SetupIntSlider(maxEnemiesPerWave, tuning.active.maxEnemiesPerWave, out _baseMaxEnemiesPerWave, maxEnemiesBaseText, maxEnemiesValueText, showPlus: false);

        // Make sure list shows something once the wave exists
        InvokeRepeating(nameof(RefreshWaveEnemyList), 0.25f, 0.5f);
    }

    private void SetupMulSlider(Slider s, float baseValue, out float baseCache, TMP_Text baseText, TMP_Text valueText)
    {
        baseCache = baseValue;

        if (s == null) return;

        float min = baseValue * 0.1f;
        float max = baseValue * 3.0f;

        // If base is 0 by mistake, fallback
        if (max <= 0f) { min = 0.1f; max = 3.0f; baseCache = 1.0f; baseValue = 1.0f; }

        s.minValue = min;
        s.maxValue = max;
        s.value = baseValue;

        UpdateMulTexts(baseCache, s.value, baseText, valueText);
    }

    private void SetupIntLikeSlider(Slider s, float baseValue, out float baseCache, TMP_Text baseText, TMP_Text valueText, bool showPlus)
    {
        baseCache = baseValue;

        if (s == null) return;

        float min = baseValue * 0.1f;
        float max = baseValue * 3.0f;

        if (max <= 0f) { min = 0f; max = 100f; baseCache = 10f; baseValue = 10f; }

        s.minValue = min;
        s.maxValue = max;
        s.wholeNumbers = true;
        s.value = Mathf.Round(baseValue);

        UpdateDeltaIntTexts(Mathf.RoundToInt(baseCache), Mathf.RoundToInt(s.value), baseText, valueText, showPlus);
    }

    private void SetupIntSlider(Slider s, int baseValue, out int baseCache, TMP_Text baseText, TMP_Text valueText, bool showPlus)
    {
        baseCache = baseValue;

        if (s == null) return;

        float min = baseValue * 0.1f;
        float max = baseValue * 3.0f;

        if (max <= 0f) { min = 1f; max = 1000f; baseCache = 100; baseValue = 100; }

        s.minValue = min;
        s.maxValue = max;
        s.wholeNumbers = true;
        s.value = baseValue;

        UpdateDeltaIntTexts(baseCache, Mathf.RoundToInt(s.value), baseText, valueText, showPlus);
    }

    private void UpdateMulTexts(float baseMul, float curMul, TMP_Text baseText, TMP_Text valueText)
    {
        if (baseText) baseText.text = "Base: " + FormatMulWithDelta(baseMul);
        if (valueText) valueText.text = FormatMulWithDelta(curMul);
    }

    private string FormatMulWithDelta(float mul)
    {
        float pct = (mul - 1f) * 100f;
        string pctText = (pct >= 0f ? "+" : "") + pct.ToString("0.#") + "%";
        return mul.ToString("0.###") + " (" + pctText + ")";
    }

    private void Awake()
    {
        if (tuning == null) tuning = RuntimeBalanceTuningManager.Instance;
        if (hud == null) hud = FindFirstObjectByType<BalanceHUD>();
    }

    private void Update()
    {
        if (hud == null) return;

        if (spawnedText) spawnedText.text = hud.EnemiesSpawnedSoFar + " / " + hud.EnemiesTotalThisWave;
        if (aliveText) aliveText.text = hud.EnemiesAlive.ToString();
    }

    private void UpdateDeltaIntTexts(int baseVal, int curVal, TMP_Text baseText, TMP_Text valueText, bool showPlus)
    {
        if (baseText) baseText.text = "Base: " + FormatInt(baseVal, showPlus);
        if (valueText) valueText.text = FormatInt(curVal, showPlus);
    }

    private string FormatInt(int v, bool showPlus)
    {
        if (!showPlus) return v.ToString();
        return (v >= 0 ? "+" : "") + v.ToString();
    }


    // ---- Slider callbacks (hook these in the Inspector) ----
    public void OnEnemyBaseHealthMul(float v)
    {
        if (tuning != null) tuning.active.enemyBaseHealthMul = v;
        UpdateMulTexts(_baseEnemyBaseHealthMul, v, enemyBaseHealthBaseText, enemyBaseHealthValueText);
    }

    public void OnEnemyDamageMul(float v)
    {
        if (tuning != null) tuning.active.enemyDamageMul = v;
        UpdateMulTexts(_baseEnemyDamageMul, v, enemyDamageBaseText, enemyDamageValueText);
    }

    public void OnEnemyMoveSpeedMul(float v)
    {
        if (tuning != null) tuning.active.enemyMoveSpeedMul = v;
        UpdateMulTexts(_baseEnemyMoveSpeedMul, v, enemyMoveSpeedBaseText, enemyMoveSpeedValueText);
    }

    public void OnEnemyCoinDropMul(float v)
    {
        if (tuning != null) tuning.active.enemyCoinDropMul = v;
        UpdateMulTexts(_baseEnemyCoinDropMul, v, enemyCoinDropBaseText, enemyCoinDropValueText);
    }

    public void OnExtraEnemiesPerWave(float v)
    {
        if (tuning != null) tuning.active.extraEnemiesPerWave = v;

        UpdateDeltaIntTexts(
            Mathf.RoundToInt(_baseExtraEnemiesPerWave),
            Mathf.RoundToInt(v),
            extraEnemiesBaseText,
            extraEnemiesValueText,
            showPlus: true
        );
    }

    public void OnMaxEnemiesPerWave(float v)
    {
        int iv = Mathf.RoundToInt(v);
        if (tuning != null) tuning.active.maxEnemiesPerWave = iv;

        UpdateDeltaIntTexts(
            _baseMaxEnemiesPerWave,
            iv,
            maxEnemiesBaseText,
            maxEnemiesValueText,
            showPlus: false
        );
    }

    // ---- Button callbacks ----

    public void ApplyAndRestartWave()
    {
        if (tuning == null) return;
        tuning.Apply();

        // Let the wave rebuild, then refresh list.
        Invoke(nameof(RefreshWaveEnemyList), 0.1f);
    }

    public void RevertTuningChanges()
    {
        if (tuning == null) return;
        tuning.RevertAll();

        Invoke(nameof(RefreshWaveEnemyList), 0.1f);
    }


    public void SaveProfile()
    {
        if (tuning == null) return;
        tuning.SaveProfileToFile();
    }

    public void LoadProfileAndApply()
    {
        if (tuning == null) return;
        tuning.LoadProfileFromFileAndApply();
    }

    public void ExportReport()
    {
        if (tuning == null) return;
        tuning.SaveReportToFile();
    }

    public void RefreshWaveEnemyList()
    {
        if (waveEnemyListText == null) return;

        var wm = WaveManager.Instance;
        if (wm == null)
        {
            waveEnemyListText.text = "No WaveManager.";
            return;
        }

        var wave = wm.GetCurrentWave();
        if (wave == null)
        {
            waveEnemyListText.text = "No wave yet.";
            return;
        }

        // WaveEnemies is public on your Wave class, but keep reflection to avoid coupling.
        var dictField = wave.GetType().GetField("WaveEnemies", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        var dictObj = dictField != null ? dictField.GetValue(wave) : null;

        if (dictObj is not System.Collections.IDictionary dict)
        {
            waveEnemyListText.text = "Wave enemy list unavailable.";
            return;
        }

        System.Text.StringBuilder sb = new System.Text.StringBuilder(768);

        // 1) Base enemies (wave-scaled + tuning-scaled)
        foreach (var key in dict.Keys)
        {
            var info = dict[key] as EnemyInfoSO;
            if (info == null) continue;

            sb.Append(info.Name)
              .Append(" \x97 HP: ").Append(info.MaxHealth.ToString("0.#"))
              .Append(" | DMG: ").Append(info.Damage.ToString("0.#"))
              .Append(" | SPD: ").Append(info.MovementSpeed.ToString("0.##"))
              .Append(" | COIN: ").Append(info.CoinDropAmount.ToString())
              .AppendLine();
        }

        // 2) Boss preview (post boss-multipliers) so numbers match the actual boss instance
        EnemyInfoSO bossPreview = TryBuildBossPreview(wave, dict);
        if (bossPreview != null)
        {
            sb.AppendLine()
              .Append(bossPreview.Name)
              .Append(" \x97 HP: ").Append(bossPreview.MaxHealth.ToString("0.#"))
              .Append(" | DMG: ").Append(bossPreview.Damage.ToString("0.#"))
              .Append(" | SPD: ").Append(bossPreview.MovementSpeed.ToString("0.##"))
              .Append(" | COIN: ").Append(bossPreview.CoinDropAmount.ToString())
              .AppendLine();
        }

        waveEnemyListText.text = sb.ToString();
    }

    private EnemyInfoSO TryBuildBossPreview(Wave wave, System.Collections.IDictionary waveEnemiesDict)
    {
        if (wave == null) return null;

        bool isMini = wave.IsMiniBossWave();
        bool isBoss = wave.IsBossWave();
        if (!isMini && !isBoss) return null;

        // Prefer configured override prefab (matches EnemySpawner behavior).
        GameObject overridePrefab = null;
        if (isBoss && wave.BossPrefab != null) overridePrefab = wave.BossPrefab;
        else if (isMini && wave.MiniBossPrefab != null) overridePrefab = wave.MiniBossPrefab;

        if (overridePrefab == null)
        {
            // EnemySpawner would pick a random enemy from the wave.
            // We can't predict that deterministically without touching EnemySpawner internals.
            return null;
        }

        var enemy = overridePrefab.GetComponent<Enemy>();
        if (enemy == null || enemy.Info == null) return null;

        EnemyInfoSO baseInfo = enemy.Info;

        // EnemySpawner uses WaveEnemies baseline if available (important: includes your tuning).
        int id = baseInfo.EnemyId;
        if (id > 0 && waveEnemiesDict.Contains(id))
        {
            var scaled = waveEnemiesDict[id] as EnemyInfoSO;
            if (scaled != null) baseInfo = scaled;
        }

        EnemyInfoSO clonedInfo = UnityEngine.Object.Instantiate(baseInfo);

        int currentWave = WaveManager.Instance != null ? WaveManager.Instance.GetCurrentWaveIndex() : wave.WaveNumber;

        if (isMini)
        {
            float healthMultiplier;
            float damageMultiplier;
            float coinMultiplier;

            if (currentWave == 5)
            {
                healthMultiplier = 30f;
                damageMultiplier = 15f;
                coinMultiplier = 20f;
            }
            else
            {
                healthMultiplier = Mathf.Min(currentWave * 50f, 500f);
                damageMultiplier = Mathf.Min(currentWave * 10f, 100f);
                coinMultiplier = Mathf.Min(currentWave * 10f, 100f);
            }

            clonedInfo.MaxHealth *= healthMultiplier;
            clonedInfo.Damage *= damageMultiplier;
            clonedInfo.CoinDropAmount = (ulong)(clonedInfo.CoinDropAmount * coinMultiplier);

            clonedInfo.MovementSpeed *= 0.9f;
            clonedInfo.AttackRange += 0.2f;

            clonedInfo.Name = "MINI BOSS: " + clonedInfo.Name;
            return clonedInfo;
        }

        if (isBoss)
        {
            float healthMultiplier;
            float damageMultiplier;
            float coinMultiplier;

            if (currentWave == 10)
            {
                healthMultiplier = 60f;
                damageMultiplier = 25f;
                coinMultiplier = 45f;
            }
            else
            {
                healthMultiplier = Mathf.Min(currentWave * 100f, 1000f);
                damageMultiplier = Mathf.Min(currentWave * 20f, 500f);
                coinMultiplier = Mathf.Min(currentWave * 20f, 500f);
            }

            clonedInfo.MaxHealth *= healthMultiplier;
            clonedInfo.Damage *= damageMultiplier;
            clonedInfo.CoinDropAmount = (ulong)(clonedInfo.CoinDropAmount * coinMultiplier);

            clonedInfo.MovementSpeed *= 0.85f;
            clonedInfo.AttackRange += 0.6f;

            clonedInfo.Name = "BOSS: " + clonedInfo.Name;
            return clonedInfo;
        }

        return null;
    }

    public void ReloadFormulasFromJson()
    {
        if (BalanceFormulaManager.Instance == null) return;
        BalanceFormulaManager.Instance.ReloadFromDiskAndApply();
    }


}
