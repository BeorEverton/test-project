using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "IdleDefense/Prestige/Node")]
public class PrestigeNodeSO : ScriptableObject
{
    [Header("Identity")]
    public string NodeId;              // unique, e.g. "core_dmg_01"
    public string DisplayName;
    [TextArea(2, 6)] public string Description;

    [Header("UI")]
    public Sprite Icon;                     // node icon in UI
    public Vector2 AnchoredPosition;        // position on the prestige canvas

    [Header("Structure")]
    public string BranchId;            // e.g. "Offense", "Economy", "Control"
    public List<string> RequiresAny;   // any of these nodeIds must be owned (OR)
    public List<string> RequiresAll;   // all of these nodeIds must be owned (AND)
    public int MinPrestigeLevel;       // optional overall prestige level gate

    [Header("Cost")]
    public int CrimsonCost = 1;        // price in Crimson Core

    [Header("Effects — Global Multipliers (additive in %)")]
    public float GlobalDamagePct;
    public float GlobalFireRatePct;
    public float GlobalCritChancePct;
    public float GlobalCritDamagePct;
    public float GlobalPierceChancePct;
    public float RangePct;
    public float RotationSpeedPct;
    public float ExplosionRadiusPct;
    public float SplashDamagePct;
    public float PierceDamageFalloffPct;        // negative to reduce falloff
    public float PelletCountPct;                // treated as % add on integer; we floor in manager
    public float DamageFalloffOverDistancePct;  // negative to reduce falloff
    public float PercentBonusDamagePerSecPct;
    public float SlowEffectPct;                 // capped elsewhere 0..100
    public float KnockbackStrengthPct;

    [Header("Bounce Pattern (%)")]
    public float BounceCountPct;
    public float BounceRangePct;
    public float BounceDelayPct;                // negative reduces delay
    public float BounceDamagePctPct;            // "damage lost per bounce" — negative reduces loss

    [Header("Cone / Delay / Trap (%)")]
    public float ConeAnglePct;
    public float ExplosionDelayPct;             // negative reduces delay
    public float AheadDistancePct;
    public float MaxTrapsActivePct;

    [Header("Armor Penetration (%)")]
    public float ArmorPenetrationPct;

    [Header("Economy / Meta")]
    public float ScrapsGainPct;
    public float BlackSteelGainPct;

    [Header("Enemy Modifiers (multiplicative, use negative for reductions)")]
    public float EnemyHealthPct;
    public float EnemyCountPct;

    [Header("LimitBreak")]
    public float SpeedMultiplierCapBonus;
    public float DamageMultiplierCapBonus;

    [Header("Unlocks")]
    public List<LimitBreakType> UnlockLimitBreaks = new();
    public List<string> UnlockGunnerIds = new();
    public List<Assets.Scripts.SO.TurretType> UnlockTurretTypes = new();

    [Header("Upgrade Cost Reductions")]
    [Range(0, 100)] public float AllUpgradeCostPct; // global discount for all turret upgrades

    [Serializable]
    public struct PerTypeCost
    {
        public Assets.Scripts.Systems.TurretUpgradeType Type;
        [Range(0, 100)] public float DiscountPct;
    }
    public List<PerTypeCost> PerUpgradeTypeDiscounts = new();

    [Header("One-shot runtime buffs (optional/temporary)")]
    public float OnResetGrantCrimson;

}
