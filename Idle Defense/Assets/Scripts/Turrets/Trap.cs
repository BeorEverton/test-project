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

        ownerTurret.StartCoroutine(TriggerRoutine(triggeringEnemy));
    }

    private IEnumerator TriggerRoutine(Enemy triggeringEnemy)
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
                if (e != null && Vector3.Distance(e.transform.position, trapWorldPosition) <= radius)
                {
                    ownerTurret.DamageEffects.ApplyAll(e, ownerTurret.RuntimeStats);
                }
            }
        }

        TrapPoolManager.Instance.DisableTrap(this);
    }

}
