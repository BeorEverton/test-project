using Assets.Scripts.Enemies;
using Assets.Scripts.Turrets;

public class LimitBreakDamageEffect : IDamageEffect
{
    public float ModifyDamage(float currentDamage, TurretStatsInstance stats, Enemy enemy)
    {
        var lb = LimitBreakManager.Instance;
        if (lb == null) return currentDamage;

        float mult = lb.DamageMultiplier; // e.g., 1.00 .. 2.00+
        // If you decide to use click-only damage, uncomment next line:
        // mult *= (1f + lb.ClickDamageBonusPct / 100f);

        return currentDamage * mult;
    }

}
