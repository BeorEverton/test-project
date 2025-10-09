using Assets.Scripts.Enemies;
using Assets.Scripts.Turrets;

public class LimitBreakDamageEffect : IDamageEffect
{
    public float ModifyDamage(float currentDamage, TurretStatsInstance stats, Enemy enemy)
    {
        if (LimitBreakManager.Instance == null) return currentDamage;

        // Global LB damage multiplier (e.g., 1.5x)
        float lbMult = LimitBreakManager.Instance.DamageMultiplier;

        // Click damage % only matters while an LB that enables clicking is active.
        // LimitBreakManager internally zeroes this when no relevant LB is active.
        float clickPct = LimitBreakManager.Instance.ClickDamageBonusPct; // 0..100
        float clickMult = 1f + (clickPct / 100f);

        return currentDamage * lbMult * clickMult;
    }
}
