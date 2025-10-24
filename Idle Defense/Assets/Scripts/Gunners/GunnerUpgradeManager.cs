using Assets.Scripts.Systems.Save;
using UnityEngine;

public class GunnerUpgradeManager : MonoBehaviour
{
    public static GunnerUpgradeManager Instance { get; private set; }
    private void Awake() { Instance = this; }

    // Returns the per-level rule for a stat, or null if not authored.
    private static bool TryGetRule(GunnerSO so, GunnerStatKey key, out GunnerSO.GunnerUpgradeRule rule)
    {
        for (int i = 0; i < so.UpgradeRules.Count; i++)
            if (so.UpgradeRules[i].Stat == key) { rule = so.UpgradeRules[i]; return true; }
        rule = default; return false;
    }

    // Compute the effective bonus value this gunner contributes for a stat (base + upgrade growth),
    // clamped by authored min/max if provided.
    // Preview helper: compute value as if the stat had "lvl" upgrades (without mutating runtime)
    public float GetEffectiveStatValueAtLevel(GunnerSO so, GunnerStatKey key, int lvl)
    {
        float baseVal = key switch
        {
            GunnerStatKey.Health => so.BaseHealth,
            GunnerStatKey.Damage => so.BaseDamage,
            GunnerStatKey.FireRate => so.BaseFireRate,
            GunnerStatKey.Range => so.BaseRange,
            GunnerStatKey.PercentBonusDamagePerSec => so.BaseDamagePerSecPctBonus,
            GunnerStatKey.SlowEffect => so.BaseSlowEffect,
            GunnerStatKey.CriticalChance => so.BaseCriticalChance,
            GunnerStatKey.CriticalDamageMultiplier => so.BaseCriticalDamage,
            GunnerStatKey.KnockbackStrength => so.BaseKnockback,
            GunnerStatKey.SplashDamage => so.BaseSplash,
            GunnerStatKey.PierceChance => so.BasePierceChance,
            GunnerStatKey.PierceDamageFalloff => so.BasePierceFalloff,
            GunnerStatKey.ArmorPenetration => so.BaseArmorPenetration,
            _ => 0f
        };

        if (!TryGetRule(so, key, out var rule) || lvl <= 0)
            return baseVal;

        float v = (rule.Mode == GunnerSO.GunnerUpgradeMode.FlatAdd)
            ? baseVal + rule.AmountPerLevel * lvl
            : baseVal * Mathf.Pow(1f + (rule.AmountPerLevel / 100f), lvl);

        if (rule.MinValue != 0f || rule.MaxValue != 0f)
        {
            float min = (rule.MinValue == 0f) ? float.NegativeInfinity : rule.MinValue;
            float max = (rule.MaxValue == 0f) ? float.PositiveInfinity : rule.MaxValue;
            v = Mathf.Clamp(v, min, max);
        }
        return v;
    }

    public float GetEffectiveStatValue(GunnerSO so, GunnerRuntime rt, GunnerStatKey key)
    {
        int lvl = rt.GetUpgradeLevel(key);
        return GetEffectiveStatValueAtLevel(so, key, lvl);
    }


    // Spend ONE point to upgrade a single stat on a specific gunner.
    public bool TrySpendPoint(GunnerSO so, GunnerRuntime rt, GunnerStatKey key)
    {
        if (rt.IsDead) return false;
        if (rt.UnspentSkillPoints <= 0) return false;
        if (rt.Unlocked == null || !rt.Unlocked.Contains(key)) return false;
        if (!TryGetRule(so, key, out _)) return false;

        rt.AddUpgradeLevel(key, +1);
        rt.UnspentSkillPoints -= 1;

        // Health upgrade affects MaxHealth immediately; keep current health ratio
        if (key == GunnerStatKey.Health)
        {
            float oldMax = rt.MaxHealth;
            float newMax = GetEffectiveStatValue(so, rt, GunnerStatKey.Health);
            newMax = Mathf.Max(1f, newMax);

            float ratio = (oldMax <= 0f) ? 1f : (rt.CurrentHealth / oldMax);
            rt.MaxHealth = newMax;
            rt.CurrentHealth = Mathf.Clamp(newMax * ratio, 0f, newMax);
        }

        // If equipped, refresh the turret’s effective bonuses right away
        if (rt.EquippedSlot >= 0)
            GunnerManager.Instance?.NotifySlotStatsChanged(rt.EquippedSlot);

        SaveGameManager.Instance.SaveGame(); // keep it consistent with your flow
        return true;
    }
}
