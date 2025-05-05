using Assets.Scripts.SO;
using UnityEngine;

public static class TurretStatsCalculator
{
    public static float CalculateDPS(TurretInfoSO info)
    {
        float critMultiplier = 1f + (info.CriticalChance / 100f) * (info.CriticalDamageMultiplier / 100f);
        float pelletBonus = Mathf.Max(1, info.PelletCount);
        float dps = info.Damage * pelletBonus * info.FireRate * critMultiplier;
        return dps;
    }
}
