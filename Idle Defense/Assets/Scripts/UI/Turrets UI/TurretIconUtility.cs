using Assets.Scripts.Systems;
using Assets.Scripts.Turrets;
using UnityEngine;

namespace Assets.Scripts.UI
{
    public static class TurretIconUtility
    {
        private static readonly int[] Thresholds = { 50, 250, 500 };

        public static Sprite GetIcon(TurretStatsInstance stats)
        {
            // fetch the prefab that belongs to this type
            GameObject prefab = TurretInventoryManager.Instance.GetPrefab(stats.TurretType);
            BaseTurret baseTurret = prefab.GetComponent<BaseTurret>();

            if (baseTurret == null)
                return null;

            Sprite[] set = baseTurret._turretUpgradeSprites;
            if (set == null || set.Length == 0)
                return null;

            int lv = stats.TotalLevel();
            int idx = lv >= Thresholds[2] ? 3 :
                      lv >= Thresholds[1] ? 2 :
                      lv >= Thresholds[0] ? 1 : 0;

            return idx < set.Length ? set[idx] : set[^1];
        }

    }
}