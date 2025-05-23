// Assets/Editor/Simulation/SimStats.cs
using System.Collections.Generic;

namespace IdleDefense.Editor.Simulation
{
    public struct SimStats
    {
        public int WavesBeaten;
        public int EnemiesKilled;
        public int BossesKilled;
        public double TotalDamageDealt;
        public int MaxZone;
        public int TotalZonesSecured;
        public double MoneySpent;
        public int UpgradeAmount;

        public List<string> UpgradeHistory;

        public List<string> TurretSnapshots;

        public List<string> BaseSnapshots;

        public double TotalDamageTaken;
        public double TotalHealthRepaired;
        public int MissionsFailed;
        public int SpeedBoostClicks;
        public double MachineGunDamage;
        public double ShotgunDamage;
        public double SniperDamage;
        public double MissileLauncherDamage;
        public double LaserDamage;
        public float SimMinutes;
    }

}
