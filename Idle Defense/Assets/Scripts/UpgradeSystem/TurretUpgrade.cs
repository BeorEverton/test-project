using Assets.Scripts.Turrets;
using System;

namespace Assets.Scripts.UpgradeSystem
{
    public class TurretUpgrade
    {
        public Func<TurretStatsInstance, float> GetCurrentValue;
        public Action<TurretStatsInstance, int> UpgradeTurret;
        public Func<TurretStatsInstance, int> GetLevel;
        public Func<TurretStatsInstance, float> GetBaseStat;
        public Func<TurretStatsInstance, float> GetBaseCost;
        public Func<TurretStatsInstance, float> GetUpgradeAmount;
        public Func<TurretStatsInstance, float> GetCostMultiplier;
        public Func<TurretStatsInstance, float> GetMaxValue;
        public Func<TurretStatsInstance, float> GetMinValue;
        public Func<TurretStatsInstance, int, float> GetCost;
        //public Func<TurretStatsInstance, int> GetAmount;
        public Func<TurretStatsInstance, int, (string value, string bonus, string cost, string count)> GetDisplayStrings;
    }
}