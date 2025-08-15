using Assets.Scripts.Enemies;
using Assets.Scripts.Systems;
using Assets.Scripts.Turrets;
using Assets.Scripts.WaveSystem;
using UnityEngine;

public class TrapPattern : MonoBehaviour, ITargetingPattern
{
    [Header("Trap Settings")]
    [Tooltip("0 = Explode immediately, >0 = delay before explosion")]
    [SerializeField] private float explosionDelay;

    [Tooltip("0 = Only single target, >0 = area radius")]
    [SerializeField] private float explosionRadius;

    [Tooltip("If true, trap spawns in random screen location. If false, tries to spawn in front of target.")]
    public bool randomPlacement = true;

    [Tooltip("Distance ahead of enemy to place trap, only used if randomPlacement is false.")]
    [SerializeField] private float aheadDistance;

    private bool poolReady = false;

    /// <summary>
    /// Triggers only trap placement logic, trap pool manager handles the actual placement in map and enemy manager handles explosion and damage
    /// </summary>
    /// <param name="turret"></param>
    /// <param name="stats"></param>
    /// <param name="primaryTarget"></param>
    public void ExecuteAttack(BaseTurret turret, TurretStatsInstance stats, GameObject primaryTarget)
    {
        if (!poolReady)
        {
            TrapPoolManager.Instance.InitializePool(
                stats.TrapPrefab,
                stats.MaxTrapsActive
            );
            poolReady = true;
        }
        explosionDelay = stats.ExplosionDelay;
        explosionRadius = stats.ExplosionRadius;
        aheadDistance = stats.AheadDistance;

        Vector2Int trapCell;
        float cellWorldY;

        if (randomPlacement || primaryTarget == null)
        {/*
            // Pick a random location within screen bounds
            float randX = Random.Range(screenBounds.Left + 1f, screenBounds.Right - 1f);
            float randY = Random.Range(screenBounds.Bottom + 2f, screenBounds.Top);

            trapCell = GridManager.Instance.GetGridPosition(new Vector3(randX, randY, 0f));
            cellWorldY = randY;*/
            float randX = Random.Range(-EnemyConfig.BaseXArea, EnemyConfig.BaseXArea);
            float randZ = Random.Range(GridManager.Instance.NearZ + GridManager.Instance._cellSize, stats.Range);
            trapCell = GridManager.Instance.GetGridPosition(new Vector3(randX, 0f, randZ));
            cellWorldY = 0f;

        }
        else
        {
            Enemy enemy = primaryTarget.GetComponent<Enemy>();
            if (enemy == null) return;

            // Current enemy position
            Vector3 enemyPos = enemy.transform.position;
            float stopZ = enemy.Info.AttackRange;
            float desiredZ = Mathf.Max(stopZ, enemyPos.Depth() - aheadDistance);
            if (desiredZ < stopZ)
                desiredZ = stopZ;

            Vector3 predictedPos = new Vector3(enemyPos.x, 0f, desiredZ);


            // Convert to grid cell
            trapCell = GridManager.Instance.GetGridPosition(predictedPos);

            // Clamp inside screen bounds
            cellWorldY = predictedPos.y;
            trapCell = ClampCellToScreen(turret, trapCell, cellWorldY);
        }

        Vector3 trapWorldPos = GridManager.Instance.GetWorldPosition(trapCell, cellWorldY);

        TrapPoolManager.Instance.PlaceTrap(
            trapWorldPos,
            trapCell,
            stats.Damage,
            explosionDelay,
            explosionRadius,
            turret,
            cellWorldY
            );

    }

    private Vector2Int ClampCellToScreen(BaseTurret turret, Vector2Int cell, float yPos)
    {
        // Clamp by grid indices, not camera plane bounds.
        float cellSize = GridManager.Instance._cellSize;

        // Allowed X range in world units (centered around 0), then to grid indices
        int minCellX = Mathf.FloorToInt(-EnemyConfig.BaseXArea / cellSize);
        int maxCellX = Mathf.FloorToInt(EnemyConfig.BaseXArea / cellSize);

        // Allowed Z range in world units from GridManager Near/Far Z, then to grid indices
        int minCellZ = Mathf.FloorToInt(GridManager.Instance.NearZ / cellSize);
        int maxCellZ = Mathf.FloorToInt(GridManager.Instance.FarZ / cellSize);

        cell.x = Mathf.Clamp(cell.x, minCellX, maxCellX);
        cell.y = Mathf.Clamp(cell.y, minCellZ, maxCellZ);
        return cell;
    }

}
