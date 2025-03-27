using UnityEngine;

namespace Assets.Scripts.SO
{
    [CreateAssetMenu(fileName = "TurretInfo", menuName = "ScriptableObjects/TurretInfo", order = 0)]
    public class TurretInfoSO : ScriptableObject
    {
        [Header("Base Turret")]
        [Tooltip("Amount of damage dealth per hit")]
        public float Damage;
        [Tooltip("Amount of time between shots")]
        public float FireRate;
        [Tooltip("Distance from the turret that it can shoot")]
        public float Range;
        [Tooltip("How fast the turretHead rotates towards the target")]
        public float RotationSpeed;

        [Header("Machine Gun Turret")]
        [Tooltip("Chance for a critical attack in procent")]
        [Range(0, 100)]
        public float CriticalChance;
        [Tooltip("Multiplier for critical damage in procent")]
        public float CriticalDamageMultiplier;

        [Header("Missile Launcher Turret")]
        [Tooltip("Radius of the explosion")]
        public float ExplosionRadius;
        [Tooltip("Amount of damage dealt to adjecent enemies in the explosion radius")]
        public float SplashDamage;

        [Header("Sniper Turret")]
        [Tooltip("Amount of enemies a bullet can pierce through")]
        public int PierceCount;
        [Tooltip("Amount of damage falloff for each enemy the pierces through")]
        public float PierceDamageFalloff;

        [Header("Shotgun Turret")]
        [Tooltip("Amount of pellets shot per shot")]
        public int PelletCount;
        [Tooltip("Amount of damage falloff over distance")]
        public float DamageFalloffOverDistance;

        [Header("Laser Turret")]
        [Tooltip("Amount of damage added to initial Damage per second active on the same target")]
        public float ProcentBonusDamagePerSec;
        [Tooltip("Amount of slow effect applied to the target")]
        public float SlowEffect;
    }
}