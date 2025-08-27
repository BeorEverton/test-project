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

    [Header("Base Stats contributed to turret (additive)")]
    public float BaseHealth = 20f;          // gunner personal HP (enemies can hit them)
    public float BaseDamage = 0f;           // added to turret Damage
    public float BaseFireRate = 0f;         // added to turret FireRate (shots/sec)
    public float BaseRange = 0f;            // added to turret Range
    public float BasePercentBonusDamagePerSec = 0f; // added to Damage Ramp
    public float BaseSlowEffect = 0f;       // added to Slow Effect
    public float BaseCriticalChance = 0f;   // %
    public float BaseCriticalDamage = 0f;   // %
    public float BaseKnockback = 0f;
    public float BaseSplash = 0f;
    public float BasePierceChance = 0f;
    public float BasePierceFalloff = 0f;
    public float BaseArmorPenetration = 0f; // %


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

