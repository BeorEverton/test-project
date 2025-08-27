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

        // Use the muzzle as geometric origin to match visuals and fix lateral offset
        Vector3 origin =
            turret._muzzleFlashPosition ? turret._muzzleFlashPosition.position
            : (turret._rotationPoint ? turret._rotationPoint.position : turret.transform.position);

        // Axis = current aim toward target (fallback to turret head's up, projected to XZ)
        Vector3 axis = primaryTarget
            ? (primaryTarget.transform.position - origin)
            : (turret._rotationPoint ? turret._rotationPoint.TransformDirection(Vector3.up) : turret.transform.up);

        // Work in XZ (depth Z): project and normalize
        axis.y = 0f;
        if (axis.sqrMagnitude < 1e-6f) axis = Vector3.forward; // depth fallback
        axis.Normalize();

        float halfRad = 0.5f * coneAngle * Mathf.Deg2Rad;
        float cosHalf = Mathf.Cos(halfRad);

        // IMPORTANT: Grid query expects a cell radius. Convert world range -> grid cells.
        // (_cellSize is public; GetEnemiesInRange enumerates around a grid center in cells.)
        int gridRange = Mathf.CeilToInt(range / Mathf.Max(0.0001f, GridManager.Instance._cellSize));
        List<Enemy> enemies = GridManager.Instance.GetEnemiesInRange(origin, gridRange);  // center at muzzle
                                                                                          // Do NOT do a second Euclidean range check here — it truncates the cone prematurely.

        foreach (var enemy in enemies)
        {
            if (!enemy) continue;

            Vector3 to = enemy.transform.position - origin;
            to.y = 0f; // XZ plane
            if (to.sqrMagnitude < 1e-6f) continue;
            to.Normalize();

            // inside cone if dot >= cos(halfAngle)
            if (Vector3.Dot(axis, to) >= cosHalf)
                turret.DamageEffects?.ApplyAll(enemy, stats);
        }
    }



#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!(GetComponent<BaseTurret>() is BaseTurret turret)) return;

        // Center of cone = turret head (_rotationPoint)
        Vector3 origin = turret._rotationPoint ? turret._rotationPoint.position : turret.transform.position;

        // Use same axis as ExecuteAttack (fallback to turret head's up)
        Vector3 axis = turret._rotationPoint ? turret._rotationPoint.TransformDirection(Vector3.up) : turret.transform.up;
        axis.y = 0f; if (axis.sqrMagnitude < 1e-6f) axis = Vector3.forward; axis.Normalize();

        Gizmos.color = new Color(1f, 0.5f, 0f, 0.35f);
        float half = coneAngle * 0.5f;

        // Rotate in XZ plane -> around world Y
        Quaternion leftRot = Quaternion.AngleAxis(-half, Vector3.up);
        Quaternion rightRot = Quaternion.AngleAxis(half, Vector3.up);

        Vector3 leftDir = leftRot * axis;
        Vector3 rightDir = rightRot * axis;

        Gizmos.DrawRay(origin, leftDir * range);
        Gizmos.DrawRay(origin, rightDir * range);

        // Arc
        int segments = 20;
        Vector3 last = origin + leftDir * range;
        for (int i = 1; i <= segments; i++)
        {
            float t = i / (float)segments;
            Quaternion segRot = Quaternion.AngleAxis(-half + coneAngle * t, Vector3.up);
            Vector3 segDir = segRot * axis;
            Vector3 next = origin + segDir * range;
            Gizmos.DrawLine(last, next);
            last = next;
        }
    }
#endif
}
