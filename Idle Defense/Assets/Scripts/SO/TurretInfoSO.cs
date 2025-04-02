using UnityEngine;

namespace Assets.Scripts.SO
{
    [CreateAssetMenu(fileName = "TurretInfo", menuName = "ScriptableObjects/TurretInfo", order = 0)]
    public class TurretInfoSO : ScriptableObject
    {
        [Header("Base Turret")]
        [Tooltip("Amount of damage dealth per hit")]
        public float Damage;
        public float DamageLevel;
        [Tooltip("Amount of time between shots")]
        public float FireRate;
        public float FireRateLevel;
        [Tooltip("How fast the turretHead rotates towards the target")]
        public float RotationSpeed;
        [Tooltip("Angle threshold between target and turret before being able to shoot")]
        public float AngleThreshold;

        [Header("Machine Gun Turret")]
        [Tooltip("Chance for a critical attack in procent")]
        [Range(0, 100)]
        public float CriticalChance;
        public float CriticalChanceLevel;
        [Tooltip("Multiplier for critical damage in procent")]
        public float CriticalDamageMultiplier;
        public float CriticalDamageMultiplierLevel;

        [Header("Missile Launcher Turret")]
        [Tooltip("Radius of the explosion")]
        public float ExplosionRadius;
        public float ExplosionRadiusLevel;
        [Tooltip("Amount of damage dealt to adjecent enemies in the explosion radius")]
        public float SplashDamage;
        public float SplashDamageLevel;

        [Header("Sniper Turret")]
        [Tooltip("Amount of enemies a bullet can pierce through")]
        public int PierceCount;
        public int PierceCountLevel;
        [Tooltip("Amount of damage falloff for each enemy the pierces through")]
        public float PierceDamageFalloff;
        public float PierceDamageFalloffLevel;

        [Header("Shotgun Turret")]
        [Tooltip("Amount of pellets shot per shot")]
        public int PelletCount;
        public int PelletCountLevel;
        [Tooltip("Amount of damage falloff over distance")]
        public float DamageFalloffOverDistance;
        public float DamageFalloffOverDistanceLevel;

        [Header("Laser Turret")]
        [Tooltip("Amount of damage added to initial Damage per second active on the same target")]
        public float ProcentBonusDamagePerSec;
        public float ProcentBonusDamagePerSecLevel;
        [Tooltip("Amount of slow effect applied to the target")]
        public float SlowEffect;
        public float SlowEffectLevel;
    }
}