using Assets.Scripts.Enemies;
using Assets.Scripts.Turrets;
using UnityEngine;

public class KnockbackEffect : IDamageEffect
{
    public float ModifyDamage(float currentDamage, TurretStatsInstance stats, Enemy enemy)
    {
        if (stats.KnockbackStrength > 0f)
        {
            // Z-only push (backwards from the player toward deeper depth)
            enemy.KnockbackVelocity = new Vector2(0f, stats.KnockbackStrength);
            enemy.KnockbackTime = 0.2f;
        }

        return currentDamage; // No change to damage
    }
}
