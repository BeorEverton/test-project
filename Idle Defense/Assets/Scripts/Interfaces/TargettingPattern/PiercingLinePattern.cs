using Assets.Scripts.Enemies;
using Assets.Scripts.Systems;
using Assets.Scripts.Turrets;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PiercingLinePattern : MonoBehaviour, ITargetingPattern
{
    private List<Vector2Int> _pathCells = new();
    private readonly float _cellSize = 1f;
    private readonly float _bulletWidth = 0.15f;
    public Vector3 startPos;

    public void ExecuteAttack(BaseTurret turret, TurretStatsInstance stats, GameObject primaryTarget)
    {
        float pierceDamageMultiplier = stats.PierceDamageFalloff / 100f;
        float currentDamage = stats.Damage;
        bool firstHit = true;

        // --- Project onto the grid plane: X (world X) and Depth (world Z) ---
        Vector2 startXZ = new Vector2(startPos.x, startPos.Depth());               // (x, z)
        Vector2 targetXZ = new Vector2(primaryTarget.transform.position.x,
                                       primaryTarget.transform.position.Depth());

        // Direction and distance on the XZ plane only
        Vector2 dirXZ = (targetXZ - startXZ).normalized;
        float planarDist = Vector2.Distance(startXZ, targetXZ);

        float extra = 3f; // tail past the target so pierce can hit things behind
        Vector2 extendedXZ = startXZ + dirXZ * (planarDist + extra);

        // 1) For the raycaster (likely uses XY): feed Y = Depth, ignore Z
        Vector3 rayStart = new Vector3(startXZ.x, startXZ.y, 0f);
        Vector3 rayEnd = new Vector3(extendedXZ.x, extendedXZ.y, 0f);

        // 2) For world-space hit checks (XZ): rebuild a proper world end (keep the turret Y)
        Vector3 extendedWorld = new Vector3(extendedXZ.x, startPos.y, extendedXZ.y);

        // Steps sized to the actual length so the line reaches far enemies
        float cell = Mathf.Max(0.01f, GridManager.Instance._cellSize);
        int steps = Mathf.CeilToInt((planarDist + extra) / cell) + 2;

        _pathCells = GridRaycaster.GetCellsAlongLine(
            rayStart,
            rayEnd,
            maxSteps: steps
        );


        List<Enemy> enemiesInPath = _pathCells
            .SelectMany(cell => GridManager.Instance.GetEnemiesInGrid(cell))
            .ToList();

        enemiesInPath = enemiesInPath
            .OrderBy(e => DistanceFromBulletLineSegmentXZ(e.transform.position, startPos, extendedWorld))
            .ToList();

        int hitIndex = 0;

        foreach (Enemy enemy in enemiesInPath)
        {
            if (enemy == null) continue;

            float distance = DistanceFromBulletLineSegmentXZ(
                enemy.transform.position,
                startPos,
                extendedWorld   
            );



            if (distance > _bulletWidth)
                continue;

            if (hitIndex > 0)
            {
                float roll = Random.Range(0f, 100f);
                if (roll > stats.PierceChance)
                    break;
            }

            // Set pierce hit index for scaling
            if (turret.PierceDamageEffectRef != null)
                turret.PierceDamageEffectRef.SetHitIndex(hitIndex);

            turret.DamageEffects.ApplyAll(enemy, stats);
            hitIndex++;
        }
    }

    // Distance from P to the SEGMENT AB (all on XZ plane)
    public float DistanceFromBulletLineSegmentXZ(Vector3 point, Vector3 lineStart, Vector3 lineEnd)
    {
        Vector3 a = new Vector3(lineStart.x, 0f, lineStart.Depth());
        Vector3 b = new Vector3(lineEnd.x, 0f, lineEnd.Depth());
        Vector3 p = new Vector3(point.x, 0f, point.Depth());

        Vector3 ab = b - a;
        float abLenSq = Mathf.Max(1e-6f, ab.sqrMagnitude);
        float t = Vector3.Dot(p - a, ab) / abLenSq;
        t = Mathf.Clamp01(t);                       // clamp to segment

        Vector3 closest = a + t * ab;
        return Vector3.Distance(p, closest);
    }

#if UNITY_EDITOR
    private readonly float _cellSizeDebug = 1f;

    private void OnDrawGizmosSelected()
    {
        // Draw the traced segment (muzzle - extended)
        if (_pathCells != null && _pathCells.Count > 0)
        {
            Gizmos.color = Color.cyan;

            // Rebuild the world points from cells to visualize the path
            foreach (var cell in _pathCells)
            {
                var center = GridManager.Instance.GetWorldPosition(cell, 0f);
                var size = new Vector3(_cellSizeDebug * 0.9f, 0.05f, _cellSizeDebug * 0.9f);
                Gizmos.DrawWireCube(center, size);
            }
        }

        // Also draw the current frame’s “ideal” shot from startPos to last extendedPos
        // (Recompute quickly if needed)
        if (Application.isPlaying)
        {
            Gizmos.color = Color.yellow;
            // Try to visualize a short forward ray from the muzzle for orientation
            Gizmos.DrawSphere(startPos, 0.05f);
        }
    }
#endif


}
