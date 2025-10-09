using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Gunner", menuName = "ScriptableObjects/Gunner", order = 10)]
public class GunnerSO : ScriptableObject
{
    [Header("Identity")]
    public string GunnerId;                 // unique, e.g. "gunner_mira"
    public string DisplayName;    

    [Header("Visuals")]
    public Sprite IdleSprite;
    public Sprite RunSprite;
    public Sprite OnTurretSprite;    

    [Header("Flavor")]
    [TextArea(2, 6)] public List<string> IdlePhrases = new List<string>();

    [Header("Base Stats")]
    public float BaseHealth = 20f;          // gunner personal HP (enemies can hit them)
    public float BaseDamage = 0f;           // added to turret Damage
    public float BaseFireRate = 0f;         // added to turret FireRate (shots/sec)
    public float BaseRange = 0f;            // added to turret Range
    public float BaseDamagePerSecPctBonus = 0f; // added to Damage Ramp
    public float BaseSlowEffect = 0f;       // added to Slow Effect
    public float BaseCriticalChance = 0f;   // %
    public float BaseCriticalDamage = 0f;   // %
    public float BaseKnockback = 0f;
    public float BaseSplash = 0f;
    public float BasePierceChance = 0f;
    public float BasePierceFalloff = 0f;
    public float BaseArmorPenetration = 0f; // %

    [Header("Limit Break")]
    public LimitBreakSkillSO LimitBreakSkill; 

    [Header("Leveling")]
    public AnimationCurve XpCurve = AnimationCurve.Linear(1, 10, 50, 1000);
    public int SkillPointsPerLevel = 1;

    [Serializable]
    public struct LevelUnlock
    {
        public int Level;                        // when reaching this level...
        public List<GunnerStatKey> Unlocks;     // ...unlock these stats
    }
    [Header("Unlocks")]
    public List<GunnerStatKey> StartingUnlocked = new List<GunnerStatKey> { GunnerStatKey.Health, GunnerStatKey.Damage };
    public List<LevelUnlock> LevelUnlocks = new List<LevelUnlock>();

    [Serializable]
    public enum GunnerUpgradeMode { FlatAdd, PercentMultiplier } // Percent of BASE value per level

    [Serializable]
    public struct GunnerUpgradeRule
    {
        public GunnerStatKey Stat;      // which stat
        public GunnerUpgradeMode Mode;  // FlatAdd or PercentMultiplier
        public float AmountPerLevel;    // Flat: +X each level; Percent: +X% each level (of the BASE)
        public float MinValue;          // optional clamp (use 0 for most)
        public float MaxValue;          // optional clamp (use large for most)
    }

    [Header("Per-Stat Upgrade Rules")]
    public List<GunnerUpgradeRule> UpgradeRules = new List<GunnerUpgradeRule>
    {
        new GunnerUpgradeRule { Stat = GunnerStatKey.Health, Mode = GunnerUpgradeMode.FlatAdd, AmountPerLevel = 5f },
        new GunnerUpgradeRule { Stat = GunnerStatKey.Damage, Mode = GunnerUpgradeMode.FlatAdd, AmountPerLevel = 1f },
        new GunnerUpgradeRule { Stat = GunnerStatKey.FireRate, Mode = GunnerUpgradeMode.FlatAdd, AmountPerLevel = 0.05f },
        new GunnerUpgradeRule { Stat = GunnerStatKey.Range, Mode = GunnerUpgradeMode.FlatAdd, AmountPerLevel = 0.5f },
        new GunnerUpgradeRule { Stat = GunnerStatKey.PercentBonusDamagePerSec, Mode = GunnerUpgradeMode.FlatAdd, AmountPerLevel = 0.5f },
        new GunnerUpgradeRule { Stat = GunnerStatKey.SlowEffect, Mode = GunnerUpgradeMode.FlatAdd, AmountPerLevel = 0.5f, MinValue = 0f, MaxValue = 100f },
        new GunnerUpgradeRule { Stat = GunnerStatKey.CriticalChance, Mode = GunnerUpgradeMode.FlatAdd, AmountPerLevel = 0.5f, MinValue = 0f, MaxValue = 100f },
        new GunnerUpgradeRule { Stat = GunnerStatKey.CriticalDamageMultiplier, Mode = GunnerUpgradeMode.FlatAdd, AmountPerLevel = 1f },
        new GunnerUpgradeRule { Stat = GunnerStatKey.KnockbackStrength, Mode = GunnerUpgradeMode.FlatAdd, AmountPerLevel = 0.5f },
        new GunnerUpgradeRule { Stat = GunnerStatKey.SplashDamage, Mode = GunnerUpgradeMode.FlatAdd, AmountPerLevel = 0.5f },
        new GunnerUpgradeRule { Stat = GunnerStatKey.PierceChance, Mode = GunnerUpgradeMode.FlatAdd, AmountPerLevel = 0.5f, MinValue = 0f, MaxValue = 100f },
        new GunnerUpgradeRule { Stat = GunnerStatKey.PierceDamageFalloff, Mode = GunnerUpgradeMode.FlatAdd, AmountPerLevel = 0.5f, MinValue = 0f },
        new GunnerUpgradeRule { Stat = GunnerStatKey.ArmorPenetration, Mode = GunnerUpgradeMode.FlatAdd, AmountPerLevel = 0.5f, MinValue = 0f, MaxValue = 100f },
    };


    public float XpRequiredForLevel(int level)
    {
        // authoring-friendly curve → fallback guard
        return Mathf.Max(1f, XpCurve.Evaluate(level));
    }
}

public enum GunnerStatKey
{
    Health,
    Damage,
    FireRate,
    Range,
    PercentBonusDamagePerSec,
    SlowEffect,
    CriticalChance,
    CriticalDamageMultiplier,
    KnockbackStrength,
    SplashDamage,
    PierceChance,
    PierceDamageFalloff,
    ArmorPenetration
}

public enum LimitBreakType
{
    None = 0,
    GlobalFireRateBoost,   // Temporarily multiplies attack speed
    GlobalDamageBoost      // Temporarily multiplies damage
}

