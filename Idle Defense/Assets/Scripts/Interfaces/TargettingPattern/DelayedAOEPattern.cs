using Assets.Scripts.Enemies;
using Assets.Scripts.Systems;
using Assets.Scripts.Turrets;
using System.Collections;
using UnityEngine;

public class DelayedAOEPattern : MonoBehaviour, ITargetingPattern
{
    [SerializeField] private float delay;
    [Tooltip("Radius of the AOE effect. Set to 0 for single-target hit.")]
    [SerializeField] private float radius;
    private Coroutine _detonationCoroutine;

    public void ExecuteAttack(BaseTurret turret, TurretStatsInstance stats, GameObject primaryTarget)
    {
        // prevents multiple coroutines from being started
        if (_detonationCoroutine == null)
        {
            delay = stats.ExplosionDelay;
            radius = stats.ExplosionRadius;
            _detonationCoroutine = StartCoroutine(Detonate(turret, stats, primaryTarget));
        }
    }

    private IEnumerator Detonate(BaseTurret turret, TurretStatsInstance stats, GameObject primaryTarget)
    {
        yield return new WaitForSeconds(delay);

        if (radius <= 0f) // Single-target hit
        {
            if (primaryTarget != null)
            {
                var enemy = primaryTarget.GetComponent<Enemy>();
                if (enemy != null)
                {
                    turret.DamageEffects.ApplyAll(enemy, stats);
                }
            }
        }
        else // Multi-target AOE hit
        {
            var enemies = GridManager.Instance.GetEnemiesInRange(
                primaryTarget.transform.position, Mathf.CeilToInt(radius));

            foreach (var enemy in enemies)
            {
                if (enemy == null) continue;
                if (Vector3.Distance(primaryTarget.transform.position, enemy.transform.position) <= radius)
                {
                    turret.DamageEffects.ApplyAll(enemy, stats);
                }
            }
        }

        _detonationCoroutine = null; // Reset coroutine reference after detonation
    }
}
