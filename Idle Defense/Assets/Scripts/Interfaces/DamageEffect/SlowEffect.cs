using Assets.Scripts.Enemies;
using Assets.Scripts.Turrets;

public class SlowEffect : IDamageEffect
{
    public float ModifyDamage(float currentDamage, TurretStatsInstance stats, Enemy enemy)
    {
        if (stats.SlowEffect > 0 && !enemy.IsSlowed)
        {
            enemy.ReduceMovementSpeed(stats.SlowEffect);
        }

        // Does not change damage
        return currentDamage;
    }
}
