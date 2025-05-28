using Assets.Scripts.PlayerBase;
using Assets.Scripts.Turrets;
using System;

namespace Assets.Scripts.UpgradeSystem
{
    public class PlayerBaseUpgrade
    {
        public Func<PlayerBaseStatsInstance, float> GetCurrentValue;
        public Action<PlayerBaseStatsInstance, float> SetCurrentValue;
        public Func<PlayerBaseStatsInstance, int> GetLevel;
        public Action<PlayerBaseStatsInstance, int> SetLevel;
        public Func<PlayerBaseStatsInstance, float> GetUpgradeAmount;
        public Func<PlayerBaseStatsInstance, float> GetCostMultiplier;
        public Func<PlayerBaseStatsInstance, float> GetMaxValue;
        public Func<PlayerBaseStatsInstance, float> GetMinValue;
        public Func<PlayerBaseStatsInstance, int, float> GetCost;
        public Func<PlayerBaseStatsInstance, int, (string value, string bonus, string cost)> GetDisplayStrings;
    }
}