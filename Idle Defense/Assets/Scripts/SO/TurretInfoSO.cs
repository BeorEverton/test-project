using UnityEngine;

namespace Assets.Scripts.SO
{
    [CreateAssetMenu(fileName = "TurretInfo", menuName = "ScriptableObjects/TurretInfo", order = 0)]
    public class TurretInfoSO : ScriptableObject
    {
        [Header("Base Turret")]
        [Tooltip("What type of turret is this?")]
        public TurretType TurretType;
        [Tooltip("Is the turret unlocked?")]
        public bool IsUnlocked;

        [Tooltip("Amount of damage dealt per hit")]
        public float Damage;
        public int DamageLevel;
        [Tooltip("Exponential damage upgrade amount")]
        public float DamageUpgradeAmount;
        public float DamageUpgradeBaseCost;
        public float DamageCostExponentialMultiplier;

        [Tooltip("Amount of shots per second")]
        public float FireRate;
        public int FireRateLevel;
        [Tooltip("Exponential fire rate upgrade amount")]
        public float FireRateUpgradeAmount;
        public float FireRateUpgradeBaseCost;
        public float FireRateCostExponentialMultiplier;

        [Tooltip("How far the turret shoots")]
        public float Range;

        [Tooltip("How fast the turretHead rotates towards the target")]
        public float RotationSpeed;

        [Tooltip("Angle threshold between target and turret before being able to shoot")]
        public float AngleThreshold;

        [Header("Machine Gun Turret")]
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
        public float MaxCriticalChance = 50f;

        [Header("Missile Launcher Turret")]
        [Tooltip("Radius of the explosion")]
        public float ExplosionRadius;
        public int ExplosionRadiusLevel;
        public float ExplosionRadiusUpgradeAmount;
        public float ExplosionRadiusUpgradeBaseCost;
        public float ExplosionRadiusCostExponentialMultiplier;
        public float MaxExplosionRadius = 5f;

        [Tooltip("Amount of damage dealt to adjacent enemies in the explosion radius")]
        public float SplashDamage;
        public int SplashDamageLevel;
        public float SplashDamageUpgradeAmount;
        public float SplashDamageUpgradeBaseCost;
        public float SplashDamageCostExponentialMultiplier;

        [Header("Sniper Turret")]
        [Tooltip("Chance for the bullet to pierce through an enemy")]
        public float PierceChance;
        public int PierceChanceLevel;
        public float PierceChanceUpgradeAmount;
        public float PierceChanceUpgradeBaseCost;
        public float PierceChanceCostExponentialMultiplier;
        public float MaxPierceChance = 100f;

        [Tooltip("Amount of damage falloff for each enemy the bullet pierces through")]
        public float PierceDamageFalloff;
        public int PierceDamageFalloffLevel;
        public float PierceDamageFalloffUpgradeAmount;
        public float PierceDamageFalloffUpgradeBaseCost;
        public float PierceDamageFalloffCostExponentialMultiplier;

        [Header("Shotgun Turret")]
        [Tooltip("Amount of pellets shot per shot")]
        public int PelletCount;
        public int PelletCountLevel;
        public int PelletCountUpgradeAmount;
        public float PelletCountUpgradeBaseCost;
        public float PelletCountCostExponentialMultiplier;

        [Header("Knockback Settings")]
        public float KnockbackStrength;
        public int KnockbackStrengthLevel;
        public float KnockbackStrengthUpgradeAmount;
        public float KnockbackStrengthUpgradeBaseCost;
        public float KnockbackStrengthCostExponentialMultiplier;

        [Tooltip("Amount of damage falloff over distance, measured in 1 unit")]
        public float DamageFalloffOverDistance;
        public int DamageFalloffOverDistanceLevel;
        public float DamageFalloffOverDistanceUpgradeAmount;
        public float DamageFalloffOverDistanceUpgradeBaseCost;
        public float DamageFalloffOverDistanceCostExponentialMultiplier;
        public float MinDamageFalloff = 0f;

        [Header("Laser Turret")]
        [Tooltip("Amount of damage added to initial Damage per second active on the same target")]
        public float PercentBonusDamagePerSec;
        public int PercentBonusDamagePerSecLevel;
        public float PercentBonusDamagePerSecUpgradeAmount;
        public float PercentBonusDamagePerSecUpgradeBaseCost;
        public float PercentBonusDamagePerSecCostExponentialMultiplier;

        [Tooltip("Amount of slow effect applied to the target")]
        public float SlowEffect;
        public int SlowEffectLevel;
        public float SlowEffectUpgradeAmount;
        public float SlowEffectUpgradeBaseCost;
        public float SlowEffectCostExponentialMultiplier;
        public float MaxSlowEffect = 100f;

        private void OnValidate()
        {
            CriticalChance = Mathf.Clamp(CriticalChance, 0, 100);
        }
    }

    public enum TurretType
    {
        MachineGun,
        Shotgun,
        Sniper,
        MissileLauncher,
        Laser
    }
}