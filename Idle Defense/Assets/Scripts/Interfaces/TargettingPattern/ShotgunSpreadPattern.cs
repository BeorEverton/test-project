using Assets.Scripts.Enemies;
using Assets.Scripts.Systems;
using Assets.Scripts.Turrets;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Tooltip("A targeting pattern that targets multiple enemies simultaneously, if there are more pellets than enemies, remaining pellets hit the same enemy")]
public class ShotgunSpreadPattern : MonoBehaviour, ITargetingPattern
{
    [SerializeField] private float spreadAngle = 30f;
    [SerializeField] private float maxRange = 15f;

    private Dictionary<Vector2, List<Vector2Int>> _cellsInPathToTarget = new();
    private List<Vector2> _pelletTargetPositions = new();
    private List<Vector2Int> _pathCells = new();

    private float _cellSize = 1f;
    private float _pelletWidth = 0.5f;

    TurretStatsInstance RuntimeStats;

    public void ExecuteAttack(BaseTurret turret, TurretStatsInstance stats, GameObject primaryTarget)
    {
        RuntimeStats = stats;

        _pathCells.Clear();
        _pelletTargetPositions.Clear();
        _cellsInPathToTarget.Clear();

        Vector2 baseDir = new Vector2(
            primaryTarget.transform.position.x - transform.position.x,
            primaryTarget.transform.position.Depth() - transform.position.Depth()
        ).normalized;


        if (stats.PelletCount % 2 == 1)
        {
            FireWithUnevenPelletCount(baseDir);
        }
        else
        {
            FirePellet(baseDir, 0f); //Shoot at the initial target
            FireWithEvenPelletCount(baseDir);
        }

        Enemy[] enemyBuffer = new Enemy[64]; // Reuse a fixed array buffer
        int enemyCount = 0;

        float[] enemyDistances = new float[64];
        int[] sortedIndices = new int[64];
        int sortedCount = 0;

        // Collect enemies once from all pellets
        foreach (Vector2 targetPos in _pelletTargetPositions)
        {
            var path = _cellsInPathToTarget[targetPos];
            foreach (var cell in path)
            {
                var enemies = GridManager.Instance.GetEnemiesInGrid(cell);
                foreach (var enemy in enemies)
                {
                    if (enemy == null)
                        continue;

                    // Avoid duplicates using manual check
                    bool alreadyAdded = false;
                    for (int i = 0; i < enemyCount; i++)
                    {
                        if (enemyBuffer[i] == enemy)
                        {
                            alreadyAdded = true;
                            break;
                        }
                    }
                    if (alreadyAdded)
                        continue;

                    float distance = DistanceFromLine(
                        new Vector2(enemy.transform.position.x, enemy.transform.position.Depth()),
                        new Vector2(transform.position.x, transform.position.Depth()),
                        targetPos
                    );

                    if (distance <= _pelletWidth)
                    {
                        float distToEnemy = Vector2.Distance(
                            new Vector2(transform.position.x, transform.position.Depth()),
                            new Vector2(enemy.transform.position.x, enemy.transform.position.Depth())
                        );


                        enemyBuffer[enemyCount] = enemy;
                        enemyDistances[enemyCount] = distToEnemy;
                        sortedIndices[sortedCount++] = enemyCount;

                        enemyCount++;
                        if (enemyCount >= enemyBuffer.Length)
                            break;
                    }
                }
            }
        }

        // Sort indices by distance (Insertion Sort for small arrays)
        for (int i = 1; i < sortedCount; i++)
        {
            int key = sortedIndices[i];
            int j = i - 1;

            while (j >= 0 && enemyDistances[sortedIndices[j]] > enemyDistances[key])
            {
                sortedIndices[j + 1] = sortedIndices[j];
                j--;
            }
            sortedIndices[j + 1] = key;
        }

        // Distribute pellets
        int pelletsRemaining = stats.PelletCount;
        int index = 0;

        while (pelletsRemaining > 0 && sortedCount > 0)
        {
            Enemy enemy = enemyBuffer[sortedIndices[index]];
            float distToEnemy = enemyDistances[sortedIndices[index]];
            float damage = stats.Damage - GetDamageFalloff(distToEnemy);

            turret.DamageEffects.ApplyAll(enemy, stats);
            pelletsRemaining--;            

            index = (index + 1) % sortedCount;
        }
    }

    private void FirePellet(Vector2 baseDir, float angleOffset)
    {
        Vector2 pelletDir = RotateVector2(baseDir, angleOffset);
        Vector2 start = new Vector2(transform.position.x, transform.position.Depth());
        Vector2 pelletTarget = start + pelletDir * maxRange;

        _pelletTargetPositions.Add(pelletTarget);

        List<Vector2Int> pelletPathCells = GridRaycaster.GetCellsAlongLine(start, pelletTarget);
        _cellsInPathToTarget.Add(pelletTarget, pelletPathCells);
    }

    /*
    private void FirePellet(Vector2 baseDir, float angleOffset)
    {
        Vector2 pelletDir = RotateVector2(baseDir, angleOffset);

        Vector2 pelletTarget = (Vector2)transform.position + pelletDir * maxRange;
        _pelletTargetPositions.Add(pelletTarget);

        List<Vector2Int> pelletPathCells = GridRaycaster.GetCellsAlongLine(transform.position, pelletTarget);
        _pathCells.AddRange(pelletPathCells);

        _cellsInPathToTarget.Add(pelletTarget, pelletPathCells);
    }*/

    private void FireWithUnevenPelletCount(Vector2 baseDir)
    {
        for (int i = 0; i < RuntimeStats.PelletCount; i++)
        {
            float angleOffset = RuntimeStats.PelletCount > 1
                ? Mathf.Lerp(-spreadAngle / 2f, spreadAngle / 2f, (float)i / (RuntimeStats.PelletCount - 1))
                : 0f;

            FirePellet(baseDir, angleOffset);
        }
    }

    private void FireWithEvenPelletCount(Vector2 baseDir)
    {
        int pelletsRemaining = RuntimeStats.PelletCount - 1;
        int leftPellets = pelletsRemaining / 2;
        int rightPellets = pelletsRemaining - leftPellets;

        FireSidePellets(baseDir, leftPellets, -1);
        FireSidePellets(baseDir, rightPellets, 1);
    }
    private void FireSidePellets(Vector2 baseDir, int pelletAmount, int directionSign) //directionSign -1 = left, 1 = right
    {
        for (int i = 0; i < pelletAmount; i++)
        {
            float pellet = ((float)i + 1) / (pelletAmount);
            float angleOffset = directionSign * Mathf.Lerp(0, spreadAngle / 2f, pellet);
            FirePellet(baseDir, angleOffset);
        }
    }

    private Vector2 RotateVector2(Vector2 v, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);

        return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
    }
    public float DistanceFromLine(Vector2 point, Vector2 lineStart, Vector2 lineEnd)
    {
        float numerator = Mathf.Abs((lineEnd.y - lineStart.y) * point.x - (lineEnd.x - lineStart.x) * point.y + lineEnd.x * lineStart.y - lineEnd.y * lineStart.x);
        float denominator = Vector2.Distance(lineStart, lineEnd);

        return numerator / denominator;
    }

    private float GetDamageFalloff(float distance)
    {
        float minFalloffDistance = 3f;
        float maxDamageFalloff = RuntimeStats.Damage * 0.9f; // cap at 90% reduction

        if (distance <= minFalloffDistance)
            return 0f;

        float effectiveDistance = distance - minFalloffDistance;
        float damageFalloff = RuntimeStats.Damage * effectiveDistance * RuntimeStats.DamageFalloffOverDistance / 100f;

        return Mathf.Min(damageFalloff, maxDamageFalloff);
    }

    protected void OnDrawGizmosSelected()
    {       

        // For debugging: Draw all grid cells traversed by pellets.
        Gizmos.color = Color.yellow;
        float cellSize = 1f;

        foreach (var cell in _cellsInPathToTarget.Values.SelectMany(path => path))
        {
            Vector3 center = GridManager.Instance.GetWorldPosition(cell, transform.position.Depth());
            Gizmos.DrawWireCube(center, Vector3.one * cellSize * 0.9f);
        }

        // Draw damage falloff zones in the XZ plane
        Vector3 turretPosition = transform.position;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(turretPosition, 3f);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(turretPosition, 6f);

        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(turretPosition, 11f);
    }

}
