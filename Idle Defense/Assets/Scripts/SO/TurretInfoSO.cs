using UnityEngine;

namespace Assets.Scripts.SO
{
    [CreateAssetMenu(fileName = "TurretInfo", menuName = "ScriptableObjects/TurretInfo", order = 0)]
    public class TurretInfoSO : ScriptableObject
    {
        [Header("ALL TURRETS")]
        [Tooltip("If true, this turret can target flying enemies.")]
        public bool CanHitFlying;
        [Tooltip("Is the turret unlocked?")]
        public bool IsUnlocked;
        [Tooltip("What type of turret is this?")]
        public TurretType TurretType;
        [Tooltip("Angle threshold between target and turret before being able to shoot")]
        public float AngleThreshold;

        [Header("BASE UPGRADES")]

        [Header("Rotation")]
        [Tooltip("How fast the turretHead rotates towards the target")]
        public float RotationSpeed;
        public float RotationSpeedUpgradeAmount;
        public float RotationSpeedUpgradeBaseCost;
        public int RotationSpeedLevel;
        public float RotationSpeedCostExponentialMultiplier;
        [Header("Damage")]
        [Tooltip("Amount of damage dealt per hit")]
        public float Damage;
        public int DamageLevel;
        [Tooltip("Exponential damage upgrade amount")]
        public float DamageUpgradeAmount;
        public float DamageUpgradeBaseCost;
        public float DamageCostExponentialMultiplier;
        [Header("Fire Rate")]
        [Tooltip("Amount of shots per second")]
        public float FireRate;
        public int FireRateLevel;
        [Tooltip("Exponential fire rate upgrade amount")]
        public float FireRateUpgradeAmount;
        public float FireRateUpgradeBaseCost;
        public float FireRateCostExponentialMultiplier;
        [Header("Range")]
        [Tooltip("How far the turret shoots")]
        public float Range;
        public float RangeUpgradeAmount;
        public float RangeUpgradeBaseCost;
        public int RangeLevel;
        public float RangeCostExponentialMultiplier;        

        [Header("TARGETTING PATTERNS")]

        [Header("Bullets")]
        [Tooltip("Amount of pellets shot per shot, used by shotgun and multi target")]
        public int PelletCount;
        public int PelletCountLevel;
        public int PelletCountUpgradeAmount;
        public float PelletCountUpgradeBaseCost;
        public float PelletCountCostExponentialMultiplier;

        [Header("Knockback")]
        public float KnockbackStrength;
        public int KnockbackStrengthLevel;
        public float KnockbackStrengthUpgradeAmount;
        public float KnockbackStrengthUpgradeBaseCost;
        public float KnockbackStrengthCostExponentialMultiplier;

        [Header("AOE")]
        [Tooltip("Radius of the explosion, used by missile launcher, trap, multi target")]
        public float ExplosionRadius;
        public int ExplosionRadiusLevel;
        public float ExplosionRadiusUpgradeAmount;
        public float ExplosionRadiusUpgradeBaseCost;
        public float ExplosionRadiusCostExponentialMultiplier;                

        [Header("Piercing Pattern")]
        [Tooltip("Chance for the bullet to pierce through an enemy")]
        public float PierceChance;
        public int PierceChanceLevel;
        public float PierceChanceUpgradeAmount;
        public float PierceChanceUpgradeBaseCost;
        public float PierceChanceCostExponentialMultiplier;

        [Tooltip("Amount of damage falloff for each enemy the bullet pierces through")]
        public float PierceDamageFalloff;
        public int PierceDamageFalloffLevel;
        public float PierceDamageFalloffUpgradeAmount;
        public float PierceDamageFalloffUpgradeBaseCost;
        public float PierceDamageFalloffCostExponentialMultiplier;

        [Header("Bounce Pattern")]
        [Tooltip("Number of times the projectile or effect can bounce to a new target")]
        public int BounceCount;
        public int BounceCountLevel;
        public float BounceCountUpgradeAmount;
        public float BounceCountUpgradeBaseCost;
        public float BounceCountCostExponentialMultiplier;

        [Tooltip("Maximum range in units for each bounce to find a new target")]
        public float BounceRange;
        public int BounceRangeLevel;
        public float BounceRangeUpgradeAmount;
        public float BounceRangeUpgradeBaseCost;
        public float BounceRangeCostExponentialMultiplier;

        [Tooltip("Delay in seconds between each bounce hit")]
        public float BounceDelay;
        public int BounceDelayLevel;
        public float BounceDelayUpgradeAmount;
        public float BounceDelayUpgradeBaseCost;
        public float BounceDelayCostExponentialMultiplier;

        [Tooltip("How much damage it loses per bounce - percentage")]
        public float BounceDamagePct;
        public int BounceDamagePctLevel;
        public float BounceDamagePctUpgradeAmount;
        public float BounceDamagePctUpgradeBaseCost;
        public float BounceDamagePctCostExponentialMultiplier;

        [Header("Cone AOE Pattern")]
        [Tooltip("Angle in degrees for the cone attack area")]
        public float ConeAngle;
        public int ConeAngleLevel;
        public float ConeAngleUpgradeAmount;
        public float ConeAngleUpgradeBaseCost;
        public float ConeAngleCostExponentialMultiplier;

        [Header("Delayed AOE Pattern")]
        [Tooltip("Delay in seconds before the explosion triggers")]
        public float ExplosionDelay;
        public int ExplosionDelayLevel;
        public float ExplosionDelayUpgradeAmount;
        public float ExplosionDelayUpgradeBaseCost;
        public float ExplosionDelayCostExponentialMultiplier;

        [Header("Trap Pattern")]
        [Tooltip("The prefab used for traps placed by this turret")]
        public GameObject TrapPrefab;

        [Tooltip("Cells in front of the enemy to place trap when targeting them")]
        public float AheadDistance;
        public int AheadDistanceLevel;
        public float AheadDistanceUpgradeAmount;
        public float AheadDistanceUpgradeBaseCost;
        public float AheadDistanceCostExponentialMultiplier;        

        [Tooltip("Maximum number of active traps this turret can have at once")]
        public int MaxTrapsActive;
        public int MaxTrapsActiveLevel;
        public float MaxTrapsActiveUpgradeAmount;
        public float MaxTrapsActiveUpgradeBaseCost;
        public float MaxTrapsActiveCostExponentialMultiplier;

        [Header("DAMAGE EFFECTS")]

        [Header("Critical")]
        [Tooltip("Chance for a critical attack in Percent")]
        public float CriticalChance;
        public int CriticalChanceLevel;
        [Tooltip("Linear critical chance upgrade amount")]
        public float CriticalChanceUpgradeAmount;
        public float CriticalChanceUpgradeBaseCost;
        public float CriticalChanceCostExponentialMultiplier;

        [Tooltip("Multiplier for critical damage in Percent")]
        public float CriticalDamageMultiplier;
        public int CriticalDamageMultiplierLevel;
        [Tooltip("Linear critical damage upgrade amount")]
        public float CriticalDamageMultiplierUpgradeAmount;
        public float CriticalDamageMultiplierUpgradeBaseCost;
        public float CriticalDamageCostExponentialMultiplier;

        [Header("Splash Damage")]
        [Tooltip("Amount of damage dealt to adjacent enemies in the explosion radius")]
        public float SplashDamage;
        public int SplashDamageLevel;
        public float SplashDamageUpgradeAmount;
        public float SplashDamageUpgradeBaseCost;
        public float SplashDamageCostExponentialMultiplier;

        [Header("Damage Falloff")]
        [Tooltip("Amount of damage falloff over distance, measured in 1 unit")]
        public float DamageFalloffOverDistance;
        public int DamageFalloffOverDistanceLevel;
        public float DamageFalloffOverDistanceUpgradeAmount;
        public float DamageFalloffOverDistanceUpgradeBaseCost;
        public float DamageFalloffOverDistanceCostExponentialMultiplier;

        [Header("Damage Ramp")]
        [Tooltip("Amount of damage added to initial Damage per second active on the same target")]
        public float PercentBonusDamagePerSec;
        public int PercentBonusDamagePerSecLevel;
        public float PercentBonusDamagePerSecUpgradeAmount;
        public float PercentBonusDamagePerSecUpgradeBaseCost;
        public float PercentBonusDamagePerSecCostExponentialMultiplier;

        [Header("Slow Effect")]
        [Tooltip("Amount of slow effect applied to the target")]
        public float SlowEffect;
        public int SlowEffectLevel;
        public float SlowEffectUpgradeAmount;
        public float SlowEffectUpgradeBaseCost;
        public float SlowEffectCostExponentialMultiplier;

        [Header("Armor Penetration")]
        [Tooltip("Percent of enemy armor ignored by this turret (0-100).")]
        public float ArmorPenetration;
        public int ArmorPenetrationLevel;
        [Tooltip("Linear upgrade amount for Armor Penetration (percent).")]
        public float ArmorPenetrationUpgradeAmount;
        public float ArmorPenetrationUpgradeBaseCost;
        public float ArmorPenetrationCostExponentialMultiplier;

        private void OnValidate()
        {
            CriticalChance = Mathf.Clamp(CriticalChance, 0, 100);
            ArmorPenetration = Mathf.Clamp(ArmorPenetration, 0f, 100f);
        }
    }

    public enum TurretType
    {
        MachineGun,
        DoubleSplitter,
        Sniper,
        VolatileFlaskLobber,
        ObsidianLens,
        WrenchSpinner,
        FlameBelcher,
        SteamMortar,
        ClockBombDistributor,
        TeslaArc,
        RicochetSpikes,
        FrostTether,
        Geargrinder,
        AlchemicalSprayer,
        HammerSlammer,
        GreenGooBombDispenser,
        CapacitorCannon,
        VaporJetTurret,
        ThermiteCore,
        HeavySlammer,
        SteamSawCutter,
        PulseSlammer,
        EtherNeedle,
        RailSpikeDriver,
        EnergyCondenser,
        MagneticRevolver,
        OozeNetLauncher,
        ReverseTimeModule,
        WeakpointFinder,
        DartLauncher,
        MagnetSpikecaster,
        FungalOvergrowthPod,
        TrapGridProjector,
        ExplosiveMineshaft
    }
}