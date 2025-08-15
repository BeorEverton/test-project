using Assets.Scripts.Enemies;
using Assets.Scripts.Turrets;
using UnityEngine;

public class KnockbackEffect : IDamageEffect
{
    public float ModifyDamage(float currentDamage, TurretStatsInstance stats, Enemy enemy)
    {
        if (stats.KnockbackStrength > 0)
        {
            Vector2 dir = (enemy.transform.position - Vector3.zero).normalized;
            enemy.KnockbackVelocity = dir * stats.KnockbackStrength;
            enemy.KnockbackTime = 0.2f;
        }
        return currentDamage; // No change to damage
    }
}
