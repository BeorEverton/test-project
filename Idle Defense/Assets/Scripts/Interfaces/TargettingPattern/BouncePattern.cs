using Assets.Scripts.Enemies;
using Assets.Scripts.Systems;
using Assets.Scripts.Turrets;
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

        bounceCount = stats.BounceCount;
        bounceRange = stats.BounceRange;
        bounceDelay = stats.BounceDelay;
        StartCoroutine(BounceRoutine(turret, stats, primaryTarget));
    }

    private IEnumerator BounceRoutine(BaseTurret turret, TurretStatsInstance stats, GameObject primaryTarget)
    {
        var hitTargets = new List<Enemy>();
        var currentTarget = primaryTarget.GetComponent<Enemy>();
        float currentDamage = stats.Damage;

        for (int i = 0; i < bounceCount && currentTarget != null; i++)
        {
            // Hit current target

            if (turret.BounceDamageEffectRef != null)
                turret.BounceDamageEffectRef.SetBounceIndex(i);

            turret.DamageEffects?.ApplyAll(currentTarget, stats);
            hitTargets.Add(currentTarget);

            // Find the closest unhit enemy within range
            Enemy nextTarget = FindClosestUnhitEnemy(currentTarget.transform.position, hitTargets);
            currentTarget = nextTarget;

            // Wait before next bounce if there is another target
            if (currentTarget != null && bounceDelay > 0f)
                yield return new WaitForSeconds(bounceDelay);
        }
    }

    private Enemy FindClosestUnhitEnemy(Vector3 fromPosition, List<Enemy> excludeList)
    {
        var nearby = GridManager.Instance.GetEnemiesInRange(fromPosition, Mathf.CeilToInt(bounceRange));
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
