using Assets.Scripts.Enemies;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Systems
{
    public class GridManager : MonoBehaviour
    {
        public static GridManager Instance { get; private set; }

        private Dictionary<Vector2Int, List<Enemy>> enemyGrid = new();
        public float _cellSize = 1f;
        public float NearZ;   // 0 at player base
        public float FarZ;    // horizon spawn point

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        /* previous fixed cell size, now can be set in inspector
        public Vector2Int GetGridPosition(Vector3 position)
        {
            return new Vector2Int(
                Mathf.FloorToInt(position.x / _cellSize),
                Mathf.FloorToInt(position.y / _cellSize)
            );
        }

        public Vector3 GetWorldPosition(Vector2Int gridPos)
        {
            return new Vector3(
                gridPos.x * _cellSize + _cellSize / 2f,
                gridPos.y * _cellSize + _cellSize / 2f,
                0f
            );
        }*/

        public Vector2Int GetGridPosition(Vector3 position)
        {
            // X - grid.x, Depth() - grid.y
            
            return new Vector2Int(
                Mathf.FloorToInt(position.x / _cellSize),
                Mathf.FloorToInt(position.Depth() / _cellSize)
            );
        }

        public Vector3 GetWorldPosition(Vector2Int gridPos, float yPosition)
        {
            // grid.x - world.x, yPosition - world.y, grid.y - world.z
            return new Vector3(
                gridPos.x * _cellSize + _cellSize * 0.5f,
                yPosition,
                gridPos.y * _cellSize + _cellSize * 0.5f
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
            if (!enemyGrid.ContainsKey(lastGridPos)) return;
            enemyGrid[lastGridPos].Remove(enemy);
            if (enemyGrid[lastGridPos].Count == 0)
                enemyGrid.Remove(lastGridPos);
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
            Gizmos.color = Color.red;
            foreach (Vector2Int gridCell in enemyGrid.Keys)
            {
                // center the tile on the XZ plane
                Vector3 center = new Vector3(
                    gridCell.x * _cellSize + _cellSize * 0.5f,
                    0f,
                    gridCell.y * _cellSize + _cellSize * 0.5f
                );

                // flat wireframe cube
                Vector3 size = new Vector3(_cellSize * 0.9f, 0.1f, _cellSize * 0.9f);
                Gizmos.DrawWireCube(center, size);
            }
        }

        /* This was the previous version with fixed cell size
        private void OnDrawGizmosSelected()
        {
            // Highlight occupied grid cells
            Gizmos.color = Color.red;
            foreach (Vector2Int gridCell in enemyGrid.Keys)
            {
                Vector3 center = new(gridCell.x * _cellSize + _cellSize / 2, gridCell.y * _cellSize + _cellSize / 2, 0);
                Gizmos.DrawWireCube(center, Vector3.one * _cellSize * 0.9f);
            }
        }*/
    }
}

public struct ScreenBounds
{
    public float Left;
    public float Right;
    public float Top;
    public float Bottom;
    public float Width;
    public float Height;
    public ScreenBounds(float left, float right, float top, float bottom, float width, float height)
    {
        Left = left;
        Right = right;
        Top = top;
        Bottom = bottom;
        Width = width;
        Height = height;
    }
}

public static class Axes
{
    public static float Depth(this Vector3 v) => v.z;            // use z
    public static Vector3 WithDepth(this Vector3 v, float d)
        => new Vector3(v.x, v.y, d);
    public static Vector3 Forward(float d = 1f) => new Vector3(0, 0, d);
}

public static class EnemyConfig
{
    public const float BaseXArea = 2.5f;
    public static float EnemySpawnDepth = 50f;
}
