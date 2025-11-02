// LimitBreakTrap.cs
using Assets.Scripts.Enemies;
using Assets.Scripts.Systems;
using System.Collections;
using UnityEngine;

public class LimitBreakTrap : Trap
{
    // Optional extra tunables (kept simple for now)
    [Range(0f, 100f)] public float armorPenetrationPct = 100f;

    /// <summary>
    /// Called by the placer (LimitBreakManager) after getting a pooled instance.
    /// You can pass the precomputed absolute damage here.
    /// </summary>
    public void ConfigureLB(float absoluteDamage, float apPct, float radiusOverride = -1f, float delayOverride = -1f)
    {
        damage = absoluteDamage;
        if (apPct >= 0f) armorPenetrationPct = apPct;
        if (radiusOverride >= 0f) radius = radiusOverride;
        if (delayOverride >= 0f) delay = delayOverride;
    }

    // Override the base trigger to use our own flat AoE damage, ignoring armor.
    protected override IEnumerator TriggerRoutine(Enemy triggeringEnemy)
    {
        Debug.Log("Calling trigger routine for LimitBreakTrap");
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        Vector3 trapWorldPosition = GridManager.Instance.GetWorldPosition(cell, worldY);

        if (radius <= 0f)
        {
            if (triggeringEnemy != null && triggeringEnemy.IsAlive)
                triggeringEnemy.TakeDamage(damage, armorPenetrationPct: armorPenetrationPct, isAoe: true);
        }
        else
        {
            var enemies = GridManager.Instance.GetEnemiesInRange(trapWorldPosition, Mathf.CeilToInt(radius));
            for (int i = 0; i < enemies.Count; i++)
            {
                var e = enemies[i];
                if (e == null || !e.IsAlive) continue;
                if (Vector3.Distance(e.transform.position, trapWorldPosition) > radius) continue;

                e.TakeDamage(damage, armorPenetrationPct: armorPenetrationPct, isAoe: true);
            }
        }

        TrapPoolManager.Instance.DisableTrap(this);
    }
}
