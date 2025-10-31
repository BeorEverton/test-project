using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StarterGunnerCardUI : MonoBehaviour
{
    [Header("Header")]
    public Image coverImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI levelText;
    public Slider xpSlider;
    public TextMeshProUGUI xpValueText;

    [Header("Stats")]
    public Transform statRoot;
    public GunnerStatRowUI statRowPrefab; // reuse the same row prefab as GunnerDetailsUI if you like

    [Header("Controls")]
    public Button selectButton;
    public TextMeshProUGUI selectLabel; // will show "Select", or "Select (1st/2nd/3rd)" if picked

    public GunnerSO Bound { get; private set; }

    private System.Action<GunnerSO> _onSelect;
    private GunnerRuntime _rt;

    public void Bind(GunnerSO so, System.Action<GunnerSO> onSelect)
    {
        Bound = so;
        _onSelect = onSelect;

        var gm = GunnerManager.Instance;
        _rt = (gm != null) ? gm.GetRuntime(so.GunnerId) : null;

        // header
        if (nameText) nameText.text = so.DisplayName;
        if (coverImage) coverImage.sprite = so.gunnerSprite;

        int lvl = _rt != null ? _rt.Level : 1;
        float curXp = _rt != null ? _rt.CurrentXp : 0f;
        float nxtXp = so.XpRequiredForLevel(lvl);
        if (levelText) levelText.text = "Lv " + lvl;
        if (xpSlider)
        {
            xpSlider.minValue = 0f;
            xpSlider.maxValue = Mathf.Max(1f, nxtXp);
            xpSlider.value = Mathf.Clamp(curXp, 0f, nxtXp);
        }
        if (xpValueText) xpValueText.text = Mathf.FloorToInt(curXp) + " / " + Mathf.FloorToInt(nxtXp);

        // stats
        RebuildStats(so, _rt);

        // button
        if (selectButton)
        {
            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(() => _onSelect?.Invoke(so));
        }
        if (selectLabel) selectLabel.text = "Select";
    }

    public void SetPickedIndex(int pickedIndex) // -1 if not picked, else 0
    {
        if (!selectLabel) return;

        // For single-selection flow:
        // pickedIndex >= 0 => show "Selected"
        // pickedIndex < 0  => show "Select"
        if (pickedIndex >= 0)
            selectLabel.text = "Selected";
        else
            selectLabel.text = "Select";
    }


    private void RebuildStats(GunnerSO so, GunnerRuntime rt)
    {
        if (!statRoot || !statRowPrefab) return;

        for (int i = statRoot.childCount - 1; i >= 0; --i)
            Destroy(statRoot.GetChild(i).gameObject);

        var keys = CollectAllStatKeys(so);
        foreach (var key in keys)
        {
            var row = Instantiate(statRowPrefab, statRoot);

            bool unlocked = rt != null && rt.Unlocked != null && rt.Unlocked.Contains(key);
            int unlockLevel = GetUnlockLevel(so, key);

            if (!unlocked && unlockLevel > 0)
            {
                // future stat (show lock & level)
                row.SetLocked(GetIconFor(key), GetNiceName(key), unlockLevel);
                continue;
            }

            // current value
            float cur = (rt != null && GunnerUpgradeManager.Instance != null)
                        ? GunnerUpgradeManager.Instance.GetEffectiveStatValue(so, rt, key)
                        : GetBaseValueFor(so, key);

            // preview next (+1)
            int nextLvl = rt != null ? rt.GetUpgradeLevel(key) + 1 : 1;
            float next = (GunnerUpgradeManager.Instance != null)
                         ? GunnerUpgradeManager.Instance.GetEffectiveStatValueAtLevel(so, key, nextLvl)
                         : cur;

            // read-only display (use UnlockedWithUpgrade for nice dual columns, but pass a no-op)
            row.SetUnlocked(
                GetIconFor(key),
                GetNiceName(key),
                FormatValueFor(key, cur));
        }
    }

    // --- lightweight copies of helpers from GunnerDetailsUI ---

    private List<GunnerStatKey> CollectAllStatKeys(GunnerSO so)
    {
        var result = new List<GunnerStatKey>();
        var seen = new HashSet<GunnerStatKey>();

        if (so.StartingUnlocked != null)
            foreach (var k in so.StartingUnlocked)
                if (seen.Add(k)) result.Add(k);

        if (so.LevelUnlocks != null && so.LevelUnlocks.Count > 0)
        {
            var sorted = new List<GunnerSO.LevelUnlock>(so.LevelUnlocks);
            sorted.Sort((a, b) => a.Level.CompareTo(b.Level));

            foreach (var group in sorted)
                if (group.Unlocks != null)
                    foreach (var k in group.Unlocks)
                        if (seen.Add(k)) result.Add(k);
        }
        return result;
    }

    private int GetUnlockLevel(GunnerSO so, GunnerStatKey key)
    {
        if (so.StartingUnlocked != null && so.StartingUnlocked.Contains(key)) return 0;
        if (so.LevelUnlocks != null)
        {
            for (int i = 0; i < so.LevelUnlocks.Count; i++)
            {
                var g = so.LevelUnlocks[i];
                if (g.Unlocks != null && g.Unlocks.Contains(key))
                    return g.Level;
            }
        }
        return int.MaxValue;
    }

    private float GetBaseValueFor(GunnerSO so, GunnerStatKey key)
    {
        switch (key)
        {
            case GunnerStatKey.Health: return so.BaseHealth;
            case GunnerStatKey.Damage: return so.BaseDamage;
            case GunnerStatKey.FireRate: return so.BaseFireRate;
            case GunnerStatKey.Range: return so.BaseRange;
            case GunnerStatKey.PercentBonusDamagePerSec: return so.BaseDamagePerSecPctBonus;
            case GunnerStatKey.SlowEffect: return so.BaseSlowEffect;
            case GunnerStatKey.CriticalChance: return so.BaseCriticalChance;
            case GunnerStatKey.CriticalDamageMultiplier: return so.BaseCriticalDamage;
            case GunnerStatKey.KnockbackStrength: return so.BaseKnockback;
            case GunnerStatKey.SplashDamage: return so.BaseSplash;
            case GunnerStatKey.PierceChance: return so.BasePierceChance;
            case GunnerStatKey.PierceDamageFalloff: return so.BasePierceFalloff;
            case GunnerStatKey.ArmorPenetration: return so.BaseArmorPenetration;
            default: return 0f;
        }
    }

    private string GetNiceName(GunnerStatKey key)
    {
        switch (key)
        {
            case GunnerStatKey.Damage: return "Damage";
            case GunnerStatKey.FireRate: return "Attack Speed";
            case GunnerStatKey.Range: return "Range";
            case GunnerStatKey.CriticalChance: return "Critical Chance";
            case GunnerStatKey.CriticalDamageMultiplier: return "Critical Damage";
            case GunnerStatKey.SlowEffect: return "Slow";
            case GunnerStatKey.KnockbackStrength: return "Knockback";
            case GunnerStatKey.SplashDamage: return "Splash";
            case GunnerStatKey.PierceChance: return "Pierce Chance";
            case GunnerStatKey.PierceDamageFalloff: return "Pierce Falloff";
            case GunnerStatKey.PercentBonusDamagePerSec: return "Damage Ramp";
            case GunnerStatKey.ArmorPenetration: return "Armor Penetration";
            case GunnerStatKey.Health: return "Health";
            default: return key.ToString();
        }
    }

    private string FormatValueFor(GunnerStatKey key, float v)
    {
        switch (key)
        {
            case GunnerStatKey.CriticalChance:
            case GunnerStatKey.SlowEffect:
            case GunnerStatKey.PierceChance:
            case GunnerStatKey.ArmorPenetration:
                return v.ToString("0.#") + "%";
            case GunnerStatKey.CriticalDamageMultiplier:
                return v.ToString("0.#") + "%";
            case GunnerStatKey.FireRate:
                return v.ToString("0.##") + "/s";
            default:
                return v.ToString("0.##");
        }
    }

    private Sprite GetIconFor(GunnerStatKey key)
    {
        return GameIconManager.Instance != null
            ? GameIconManager.Instance.IconForGunnerStat(key)
            : null;
    }
}
