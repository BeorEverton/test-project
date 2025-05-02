using UnityEngine;
using Assets.Scripts.Systems;

namespace Assets.Scripts.Turrets
{
    public static class TurretIconUtility
    {
        private static readonly int[] thresholds = { 50, 250, 500 };

        public static Sprite GetIcon(TurretStatsInstance stats)
        {
            // fetch the prefab that belongs to this type
            GameObject prefab = TurretInventoryManager.I.GetPrefab(stats.TurretType);
            var baseTurret = prefab.GetComponent<BaseTurret>();

            if (baseTurret == null) return null;

            Sprite[] set = baseTurret._turretUpgradeSprites; 
            if (set == null || set.Length == 0) return null;

            int lv = TotalLevel(stats);
            int idx = lv >= thresholds[2] ? 3 :
                      lv >= thresholds[1] ? 2 :
                      lv >= thresholds[0] ? 1 : 0;

            return idx < set.Length ? set[idx] : set[^1];
        }

        private static int TotalLevel(TurretStatsInstance s) =>
            Mathf.FloorToInt(
                  s.DamageLevel + s.FireRateLevel + s.CriticalChanceLevel
                + s.CriticalDamageMultiplierLevel + s.ExplosionRadiusLevel
                + s.SplashDamageLevel + s.PierceChanceLevel
                + s.PierceDamageFalloffLevel + s.PelletCountLevel
                + s.DamageFalloffOverDistanceLevel
                + s.PercentBonusDamagePerSecLevel + s.SlowEffectLevel);
    }
}
