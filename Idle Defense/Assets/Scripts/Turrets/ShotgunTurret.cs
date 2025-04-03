using Assets.Scripts.Enemies;
using Assets.Scripts.Systems;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts.Turrets
{
    public class ShotgunTurret : BaseTurret
    {
        [SerializeField] private float spreadAngle = 30f;
        [SerializeField] private float maxRange = 15f;

        private List<Vector2Int> pathCells = new();
        private float _cellSize = 1f;
        private float _pelletWidth = 0.5f;
        private int pelletCount = 3;

        private void Start()
        {
            pelletCount = _turretInfo.PelletCount;
        }

        protected override void Shoot()
        {
            base.Shoot();

            pathCells.Clear();

            Vector2 baseDir = ((_targetEnemy.transform.position - transform.position)).normalized;

            for (int i = 0; i < pelletCount; i++)
            {
                float angleOffset = pelletCount > 1
                    ? Mathf.Lerp(-spreadAngle / 2f, spreadAngle / 2f, (float)i / (pelletCount - 1))
                    : 0f;

                Vector2 pelletDir = RotateVector2(baseDir, angleOffset);

                Vector2 pelletTarget = (Vector2)transform.position + pelletDir * maxRange;

                List<Vector2Int> pelletPathCells = GridRaycaster.GetCellsAlongLine(transform.position, pelletTarget);

                pathCells.AddRange(pelletPathCells);

                List<Enemy> enemiesInPath = pelletPathCells
                    .SelectMany(cell => GridManager.Instance.GetEnemiesInGrid(cell))
                    .Distinct()
                    .ToList();

                enemiesInPath.Sort((a, b) =>
                    Vector3.Distance(transform.position, a.transform.position)
                        .CompareTo(Vector3.Distance(transform.position, b.transform.position))
                );

                foreach (Enemy enemy in enemiesInPath)
                {
                    float distance = DistanceFromLine(enemy.transform.position, transform.position, pelletTarget);
                    if (distance <= _pelletWidth)
                    {
                        float distanceToEnemy = Vector2.Distance(transform.position, enemy.transform.position);
                        float damage = _damage - GetDamageFalloff(distanceToEnemy);
                        Debug.Log($"[SHOTGUN] Distance to {enemy}: {distanceToEnemy} resulted in {damage} damage");
                        enemy.TakeDamage(damage);
                        break; // Only hit the first enemy in the path
                    }
                }
            }

            _timeSinceLastShot = 0f;
        }

        private float GetDamageFalloff(float distance)
        {
            float damageFalloff = _damage * distance * _turretInfo.DamageFalloffOverDistance / 100;
            float maxDamageFalloff = _damage * 0.9f; // maximum damage falloff set to 90%
            return damageFalloff < maxDamageFalloff ? damageFalloff : maxDamageFalloff;
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

        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();

            // For debugging: Draw all grid cells traversed by pellets.
            Gizmos.color = Color.yellow;
            foreach (Vector3 center in pathCells
                         .Select(cell => new Vector3(cell.x * _cellSize + _cellSize / 2, cell.y * _cellSize + _cellSize / 2, 0)))
            {
                Gizmos.DrawWireCube(center, Vector3.one * _cellSize * 0.9f);
            }
        }
    }
}
