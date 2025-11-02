using Assets.Scripts.Enemies;
using Assets.Scripts.Systems;
using Assets.Scripts.Turrets;
using System.Collections;
using UnityEngine;

public class Trap 
{
    public GameObject visual;
    public Vector2Int cell;
    public float worldY;
    public float damage;
    public float delay;
    public float radius;
    public bool isActive;

    public BaseTurret ownerTurret; // store when placed

    public void Trigger(Enemy triggeringEnemy)
    {
        if (!isActive) return;

        // Run on turret when available; otherwise use the pool manager as a coroutine host
        MonoBehaviour runner = ownerTurret != null ? ownerTurret : TrapPoolManager.Instance;
        runner.StartCoroutine(TriggerRoutine(triggeringEnemy));
    }


    protected virtual IEnumerator TriggerRoutine(Enemy triggeringEnemy)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        if (radius <= 0f)
        {
            // Single target
            ownerTurret.DamageEffects.ApplyAll(triggeringEnemy, ownerTurret.RuntimeStats);
        }
        else
        {
            // AOE from trap position
            Vector3 trapWorldPosition = GridManager.Instance.GetWorldPosition(cell, worldY);

            var enemies = GridManager.Instance.GetEnemiesInRange(
                trapWorldPosition,
                Mathf.CeilToInt(radius));

            foreach (var e in enemies)
            {
                if (e == null) continue;
                if (Vector3.Distance(e.transform.position, trapWorldPosition) > radius) continue;

                bool isPrimary = (e == triggeringEnemy);

                if (isPrimary)
                {
                    // Full flat damage to the one that triggered the trap
                    ownerTurret.DamageEffects.ApplyAll(e, ownerTurret.RuntimeStats);
                }
                else if (ownerTurret.SplashDamageEffectRef != null && ownerTurret.RuntimeStats.SplashDamage > 0f)
                {
                    // Splash damage (percentage of total)
                    ownerTurret.DamageEffects.ApplyAll(e, ownerTurret.RuntimeStats, ownerTurret.SplashDamageEffectRef);
                }
                else
                {
                    // Fallback: apply full damage if splash not configured
                    ownerTurret.DamageEffects.ApplyAll(e, ownerTurret.RuntimeStats);
                }
            }

        }

        TrapPoolManager.Instance.DisableTrap(this);
    }

}
