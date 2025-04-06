using Unity.Collections.LowLevel.Unsafe;
using Unity.VisualScripting.Dependencies.Sqlite;
using UnityEngine;

namespace Assets.Scripts.SO
{
    [CreateAssetMenu(fileName = "TurretInfo", menuName = "ScriptableObjects/TurretInfo", order = 0)]
    public class TurretInfoSO : ScriptableObject
    {
        [Header("Base Turret")]
        [Tooltip("Amount of damage dealt per hit")]
        public float Damage;
        public float DamageLevel;
        public float DamageUpgradeAmount;
        public float DamageUpgradeBaseCost;

        [Tooltip("Amount of time between shots")]
        public float FireRate;
        public float FireRateLevel;
        public float FireRateUpgradeAmount;
        public float FireRateUpgradeBaseCost;

        [Tooltip("How fast the turretHead rotates towards the target")]
        public float RotationSpeed;

        [Tooltip("Angle threshold between target and turret before being able to shoot")]
        public float AngleThreshold;

        [Header("Machine Gun Turret")]
        [Tooltip("Chance for a critical attack in Percent")]
        public float CriticalChance;
        public float CriticalChanceLevel;
        public float CriticalChanceUpgradeAmount;
        public float CriticalChanceUpgradeBaseCost;

        [Tooltip("Multiplier for critical damage in Percent")]
        public float CriticalDamageMultiplier;
        public float CriticalDamageMultiplierLevel;
        public float CriticalDamageMultiplierUpgradeAmount;
        public float CriticalDamageMultiplierUpgradeBaseCost;

        [Header("Missile Launcher Turret")]
        [Tooltip("Radius of the explosion")]
        public float ExplosionRadius;
        public float ExplosionRadiusLevel;
        public float ExplosionRadiusUpgradeAmount;
        public float ExplosionRadiusUpgradeBaseCost;

        [Tooltip("Amount of damage dealt to adjacent enemies in the explosion radius")]
        public float SplashDamage;
        public float SplashDamageLevel;
        public float SplashDamageUpgradeAmount;
        public float SplashDamageUpgradeBaseCost;

        [Header("Sniper Turret")]
        [Tooltip("Amount of enemies a bullet can pierce through")]
        public int PierceCount;
        public int PierceCountLevel;
        public int PierceCountUpgradeAmount;
        public float PierceCountUpgradeBaseCost;

        [Tooltip("Amount of damage falloff for each enemy the bullet pierces through")]
        public float PierceDamageFalloff;
        public float PierceDamageFalloffLevel;
        public float PierceDamageFalloffUpgradeAmount;
        public float PierceDamageFalloffUpgradeBaseCost;

        [Header("Shotgun Turret")]
        [Tooltip("Amount of pellets shot per shot")]
        public int PelletCount;
        public int PelletCountLevel;
        public int PelletCountUpgradeAmount;
        public float PelletCountUpgradeBaseCost;

        [Tooltip("Amount of damage falloff over distance, measured in 1 unit")]
        public float DamageFalloffOverDistance;
        public float DamageFalloffOverDistanceLevel;
        public float DamageFalloffOverDistanceUpgradeAmount;
        public float DamageFalloffOverDistanceUpgradeBaseCost;

        [Header("Laser Turret")]
        [Tooltip("Amount of damage added to initial Damage per second active on the same target")]
        public float PercentBonusDamagePerSec;
        public float PercentBonusDamagePerSecLevel;
        public float PercentBonusDamagePerSecUpgradeAmount;
        public float PercentBonusDamagePerSecUpgradeBaseCost;

        [Tooltip("Amount of slow effect applied to the target")]
        public float SlowEffect;
        public float SlowEffectLevel;
        public float SlowEffectUpgradeAmount;
        public float SlowEffectUpgradeBaseCost;

        private void OnValidate()
        {
            CriticalChance = Mathf.Clamp(CriticalChance, 0, 100);
        }
    }
}