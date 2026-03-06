using Assets.Scripts.Enemies;
using Assets.Scripts.Systems;
using Assets.Scripts.Turrets;
using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BouncePattern : MonoBehaviour, ITargetingPattern
{
    [SerializeField] private int bounceCount;
    [SerializeField] private float bounceRange;
    [SerializeField] private float bounceDelay; // delay between bounces in seconds

    public void ExecuteAttack(BaseTurret turret, TurretStatsInstance stats, GameObject primaryTarget)
    {
        if (primaryTarget == null) return;

        // Always reset bounce index so we never “inherit” a previous shot.
        if (turret.BounceDamageEffectRef != null)
            turret.BounceDamageEffectRef.SetBounceIndex(0);

        bool procBounce =
            (stats.BounceChance >= 100f) ||
            (stats.BounceChance > 0f && Random.value <= (stats.BounceChance / 100f));

        // If bounce doesn’t proc, behave like a single-hit pattern.
        if (!procBounce)
        {
            var enemy = primaryTarget.GetComponent<Enemy>();
            if (enemy != null)
                turret.DamageEffects?.ApplyAll(enemy, stats, turret.SlotIndex);
            return;
        }

        bounceCount = stats.BounceCount;
        bounceRange = stats.BounceRange;
        bounceDelay = stats.BounceDelay;

        StartCoroutine(BounceRoutine(turret, stats, primaryTarget));
    }

    private IEnumerator BounceRoutine(BaseTurret turret, TurretStatsInstance stats, GameObject primaryTarget)
    {
        var hitTargets = new List<Enemy>();
        var currentTarget = primaryTarget.GetComponent<Enemy>();

        // 1 primary hit + N extra bounces
        int totalHits = Mathf.Max(1, stats.BounceCount + 1);

        float currentDamage = turret.ComputeDamageAfterFalloff(currentTarget.transform.position, stats);

        for (int i = 0; i < totalHits && currentTarget != null; i++)
        {
            // compute damage for this hop:
            //  - i == 0: full damage
            //  - i > 0 : previous * pct, clamped to >= 1
            if (i > 0)
                currentDamage = Mathf.Max(1f, currentDamage * stats.BounceDamagePct);

            // optional: keep bounce index for VFX/telemetry
            if (turret.BounceDamageEffectRef != null)
                turret.BounceDamageEffectRef.SetBounceIndex(i);

            // Apply all effects but skip any internal "bounce scaler"
            turret.DamageEffects?.ApplyAll_NoBounce(currentTarget, stats, currentDamage, turret.SlotIndex);
            hitTargets.Add(currentTarget);

            // find next closest unhit enemy inside bounce range
            Enemy nextTarget = FindClosestUnhitEnemy(currentTarget.transform.position, hitTargets, stats.CanHitFlying);
            currentTarget = nextTarget;

            // pacing
            if (currentTarget != null && bounceDelay > 0f)
                yield return new WaitForSeconds(bounceDelay);
        }
    }


    private Enemy FindClosestUnhitEnemy(Vector3 fromPosition, List<Enemy> excludeList, bool hitFlying)
    {
        var nearby = GridManager.Instance.GetEnemiesInRange(fromPosition, Mathf.CeilToInt(bounceRange), hitFlying);
        Enemy closest = null;
        float minDist = float.MaxValue;

        foreach (var enemy in nearby)
        {
            if (enemy == null || excludeList.Contains(enemy))
                continue;

            float dist = Vector3.Distance(fromPosition, enemy.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                closest = enemy;
            }
        }

        return closest;
    }
}
