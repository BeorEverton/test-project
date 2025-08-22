using Assets.Scripts.Enemies;
using Assets.Scripts.Turrets;

/// <summary>
/// Multiplies the final accumulated damage by (stats.SplashDamage / 100f).
/// Use this ONLY for non-primary targets (secondary hits).
/// </summary>
public sealed class SplashDamageEffect : IDamageEffect
{
    public float ModifyDamage(float currentDamage, TurretStatsInstance stats, Enemy enemy)
    {
        float pct = stats.SplashDamage;
        if (pct <= 0f || currentDamage <= 0f) return currentDamage;
        return currentDamage * pct;
    }
}
