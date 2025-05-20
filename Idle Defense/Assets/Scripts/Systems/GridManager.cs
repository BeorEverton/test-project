using Assets.Scripts.Enemies;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Systems
{
    public class GridManager : MonoBehaviour
    {
        public static GridManager Instance { get; private set; }

        private Dictionary<Vector2Int, List<Enemy>> enemyGrid = new();
        [SerializeField] private float _cellSize = 1f;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        public Vector2Int GetGridPosition(Vector3 position)
        {
            return new Vector2Int(
                Mathf.FloorToInt(position.x / _cellSize),
                Mathf.FloorToInt(position.y / _cellSize)
            );
        }

        public void AddEnemy(Enemy enemy)
        {
            Vector2Int gridPos = GetGridPosition(enemy.transform.position);

            if (!enemyGrid.ContainsKey(gridPos))
                enemyGrid[gridPos] = new List<Enemy>();

            enemyGrid[gridPos].Add(enemy);
        }

        public void RemoveEnemy(Enemy enemy, Vector2Int lastGridPos)
        {
            Vector3 oldGridPos = new(lastGridPos.x, lastGridPos.y, 0);
            Vector2Int gridPos = GetGridPosition(oldGridPos);

            if (!enemyGrid.ContainsKey(gridPos))
                return;

            enemyGrid[gridPos].Remove(enemy);

            if (enemyGrid[gridPos].Count == 0)
                enemyGrid.Remove(gridPos);
        }

        public List<Enemy> GetEnemiesInGrid(Vector2Int gridPos)
        {
            return enemyGrid.TryGetValue(gridPos, out List<Enemy> enemies) ? enemies : new List<Enemy>();
        }

        public List<Enemy> GetEnemiesInRange(Vector3 position, int gridRange)
        {
            List<Enemy> enemiesInRange = new();
            Vector2Int centerGridPos = GetGridPosition(position);

            for (int x = -gridRange; x <= gridRange; x++)
            {
                for (int y = 0; y <= gridRange - Mathf.Abs(x); y++)
                {
                    Vector2Int checkPosUp = new(centerGridPos.x + x, centerGridPos.y + y);
                    Vector2Int checkPosDown = new(centerGridPos.x + x, centerGridPos.y - y);
                    if (checkPosUp == checkPosDown)
                        if (enemyGrid.TryGetValue(checkPosUp, out List<Enemy> enemiesCenter))
                            enemiesInRange.AddRange(enemiesCenter);
                        else
                            continue;
                    else
                    {
                        if (enemyGrid.TryGetValue(checkPosUp, out List<Enemy> enemiesUp))
                            enemiesInRange.AddRange(enemiesUp);
                        if (enemyGrid.TryGetValue(checkPosDown, out List<Enemy> enemiesDown))
                            enemiesInRange.AddRange(enemiesDown);
                    }
                }
            }

            return enemiesInRange;
        }


        private void OnDrawGizmosSelected()
        {
            // Highlight occupied grid cells
            Gizmos.color = Color.red;
            foreach (Vector2Int gridCell in enemyGrid.Keys)
            {
                Vector3 center = new(gridCell.x * _cellSize + _cellSize / 2, gridCell.y * _cellSize + _cellSize / 2, 0);
                Gizmos.DrawWireCube(center, Vector3.one * _cellSize * 0.9f);
            }
        }
    }
}