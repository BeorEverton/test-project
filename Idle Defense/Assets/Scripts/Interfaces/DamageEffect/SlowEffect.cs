using Assets.Scripts.Enemies;
using Assets.Scripts.Turrets;

public class SlowEffect : IDamageEffect
{
    public float ModifyDamage(float currentDamage, TurretStatsInstance stats, Enemy enemy)
    {
        if (stats.SlowEffect > 0 && !enemy.IsSlowed)
        {
            float chance = stats.SlowChance;

            bool proc =
                (chance >= 100f) ||
                (chance > 0f && UnityEngine.Random.value <= (chance / 100f));

            if (proc)
                enemy.ReduceMovementSpeed(stats.SlowEffect);
        }

        // Does not change damage
        return currentDamage;
    }
}
