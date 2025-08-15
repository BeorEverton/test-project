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
    private readonly float _bulletWidth = 0.1f;
    public Vector3 startPos;

    public void ExecuteAttack(BaseTurret turret, TurretStatsInstance stats, GameObject primaryTarget)
    {
        float pierceDamageMultiplier = stats.PierceDamageFalloff / 100f;
        float currentDamage = stats.Damage;
        bool firstHit = true;

        Vector3 targetPos = primaryTarget.transform.position.WithDepth(primaryTarget.transform.position.Depth());
        Vector3 dir = (targetPos - startPos).normalized;
        float distanceToTarget = Vector3.Distance(startPos, targetPos);
        Vector3 extendedPos = startPos + dir * (distanceToTarget);


        _pathCells = GridRaycaster.GetCellsAlongLine(
            startPos,
            extendedPos,
            maxSteps: 30 // or however many steps you need
        );

        List<Enemy> enemiesInPath = _pathCells
            .SelectMany(cell => GridManager.Instance.GetEnemiesInGrid(cell))
            .ToList();

        int hitIndex = 0;

        foreach (Enemy enemy in enemiesInPath)
        {
            if (enemy == null) continue;

            float distance = DistanceFromBulletLine(
                enemy.transform.position,
                transform.position,
                extendedPos
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

    public float DistanceFromBulletLine(Vector3 point, Vector3 lineStart, Vector3 lineEnd)
    {
        // Project positions onto XZ plane
        Vector3 a = new Vector3(lineStart.x, 0f, lineStart.Depth());
        Vector3 b = new Vector3(lineEnd.x, 0f, lineEnd.Depth());
        Vector3 p = new Vector3(point.x, 0f, point.Depth());

        Vector3 ab = b - a;
        Vector3 ap = p - a;

        float projected = Vector3.Dot(ap, ab.normalized);
        Vector3 projectedPoint = a + ab.normalized * projected;

        return Vector3.Distance(p, projectedPoint);
    }

}
