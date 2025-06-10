using Assets.Scripts.PlayerBase;
using System;

namespace Assets.Scripts.UpgradeSystem
{
    public class PlayerBaseUpgrade
    {
        public Func<PlayerBaseStatsInstance, float> GetCurrentValue;
        public Action<PlayerBaseStatsInstance, int> Upgrade;
        public Func<PlayerBaseStatsInstance, int> GetLevel;
        public Func<PlayerBaseStatsInstance, float> GetUpgradeAmount;
        public Func<PlayerBaseStatsInstance, float> GetBaseCost;
        public Func<PlayerBaseStatsInstance, float> GetMaxValue;
        public Func<PlayerBaseStatsInstance, float> GetMinValue;
        public Func<PlayerBaseStatsInstance, int, float> GetCost;
        //public Func<PlayerBaseStatsInstance, int> GetAmount;
        public Func<PlayerBaseStatsInstance, int, (string value, string bonus, string cost, string count)> GetDisplayStrings;
    }
}