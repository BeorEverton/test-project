using Assets.Scripts.Enemies;
using Assets.Scripts.Systems;
using Assets.Scripts.Turrets;
using System.Collections.Generic;
using UnityEngine;

public class ConeAOEPattern : MonoBehaviour, ITargetingPattern
{
    [SerializeField] private float coneAngle;
    float range;

    public void ExecuteAttack(BaseTurret turret, TurretStatsInstance stats, GameObject primaryTarget)
    {
        coneAngle = stats.ConeAngle;
        range = stats.Range;

        // Direction is from the rotation point forward (turret head direction)
        Vector3 forward = turret._rotationPoint.up;

        // Get all enemies within attack range
        List<Enemy> enemies = GridManager.Instance.GetEnemiesInRange(
            turret.transform.position, Mathf.CeilToInt(range));

        foreach (var enemy in enemies)
        {
            if (enemy == null) continue;

            Vector3 dirToEnemy = (enemy.transform.position - turret._rotationPoint.position).normalized;

            if (Vector3.Angle(forward, dirToEnemy) <= coneAngle * 0.5f)
            {
                turret.DamageEffects?.ApplyAll(enemy, stats);
            }
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!(GetComponent<BaseTurret>() is BaseTurret turret)) return;

        // Center of cone = turret head (_rotationPoint)
        Vector3 origin = turret._rotationPoint.position;
        Vector3 forward = turret._rotationPoint.up;

        // Draw cone boundaries
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.35f); // orange semi-transparent
        float halfAngle = coneAngle * 0.5f;

        Quaternion leftRot = Quaternion.AngleAxis(-halfAngle, Vector3.forward);
        Quaternion rightRot = Quaternion.AngleAxis(halfAngle, Vector3.forward);

        Vector3 leftDir = leftRot * forward;
        Vector3 rightDir = rightRot * forward;

        Gizmos.DrawRay(origin, leftDir * range);
        Gizmos.DrawRay(origin, rightDir * range);

        // Draw arc lines to visualize cone fill
        int segments = 20;
        Vector3 lastPoint = origin + leftDir * range;
        for (int i = 1; i <= segments; i++)
        {
            float t = i / (float)segments;
            Quaternion segRot = Quaternion.AngleAxis(-halfAngle + coneAngle * t, Vector3.forward);
            Vector3 segDir = segRot * forward;
            Vector3 nextPoint = origin + segDir * range;
            Gizmos.DrawLine(lastPoint, nextPoint);
            lastPoint = nextPoint;
        }
    }
#endif
}
