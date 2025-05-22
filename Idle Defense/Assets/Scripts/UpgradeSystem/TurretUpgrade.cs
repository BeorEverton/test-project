using Assets.Scripts.Turrets;
using System;

namespace Assets.Scripts.UpgradeSystem
{
    public class TurretUpgrade
    {
        public Func<TurretStatsInstance, float> GetCurrentValue;
        public Action<TurretStatsInstance, float> SetCurrentValue;
        public Func<TurretStatsInstance, int> GetLevel;
        public Action<TurretStatsInstance, int> SetLevel;
        public Func<TurretStatsInstance, float> GetBaseCode;
        public Func<TurretStatsInstance, float> GetUpgradeAmount;
        public Func<TurretStatsInstance, float> GetCostMultiplier;
        public Func<TurretStatsInstance, float> GetMaxValue;
        public Func<TurretStatsInstance, float> GetMinValue;
        public Func<TurretStatsInstance, float> GetCost;
        public Action<TurretStatsInstance> UpdateDisplay;
        public Action<TurretStatsInstance> OnUpgrade;
    }
}