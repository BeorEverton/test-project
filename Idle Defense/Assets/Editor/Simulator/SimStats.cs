// Assets/Editor/Simulation/SimStats.cs
using System.Collections.Generic;

namespace IdleDefense.Editor.Simulation
{
    public struct WaveStat
    {
        public int Wave;
        public int EnemiesSpawned;
        public int EnemiesKilled;
        public int BossesKilled;
        public float DamageDealt;
        public ulong MoneyEarned;
        public ulong MoneySpent;
        public int TurretUpgrades;
        public int BaseUpgrades;
        public float DamageTaken;
        public float HealthStart;
        public float HealthEnd;
        public float HealthRegen;
        public bool WaveBeaten;        // true = cleared
        public float SpeedBoostClicks;
        public double MachineGunDamage;
        public double ShotgunDamage;
        public double SniperDamage;
        public double MissileLauncherDamage;
        public double LaserDamage;

        public int MaxHealthLevel;
        public int RegenAmountLevel;
        public int RegenIntervalLevel;

        // Add current base stats
        public float CurrentMaxHealth;
        public float CurrentRegenAmount;
        public float CurrentRegenInterval;

        // breakdown of turret upgrades by stat
        public int DamageUpgrades;
        public int FireRateUpgrades;
        public int CriticalChanceUpgrades;
        public int CriticalDamageMultiplierUpgrades;
        public int ExplosionRadiusUpgrades;
        public int SplashDamageUpgrades;
        public int PierceChanceUpgrades;
        public int PierceDamageFalloffUpgrades;
        public int PelletCountUpgrades;
        public int KnockbackStrengthUpgrades;
        public int DamageFalloffOverDistanceUpgrades;
        public int PercentBonusDamagePerSecUpgrades;
        public int SlowEffectUpgrades;

        // total turret-upgrade spend this wave
        public ulong TurretMoneySpent;

        public string Slot1;
        public string Slot2;
        public string Slot3;
        public string Slot4;
        public string Slot5;
    }

    public struct SimStats
    {
        public int WavesBeaten;
        public int EnemiesKilled;
        public int BossesKilled;
        public double TotalDamageDealt;
        public int MaxZone;
        public double MoneySpent;
        public int UpgradeAmount;

        public List<WaveStat> Waves;    // <<< per-wave detail >>>

        public double TotalDamageTaken;
        public double TotalHealthRepaired;
        public int MissionsFailed;
        public int SpeedBoostClicks;

        public float SimMinutes;
    }
}
