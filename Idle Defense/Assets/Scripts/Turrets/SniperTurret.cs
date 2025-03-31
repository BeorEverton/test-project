using Assets.Scripts.Enemies;
using Assets.Scripts.Systems;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts.Turrets
{
    public class SniperTurret : BaseTurret
    {
        public List<Vector2Int> pathCells = new();
        private float _cellSize = 1f;
        private float _bulletWidth = 0.5f;

        protected override void Shoot()
        {
            int enemiesHit = 0;
            // Get dmg bonus from GameManager and calculate effective damage
            float baseDamage = _turretInfo.Damage * (1f + GameManager.Instance.dmgBonus / 100f);
            float pierceDamageFalloff = 1f - _turretInfo.PierceDamageFalloff / 100f;

            float currentDamage = baseDamage;

            pathCells = GridRaycaster.GetCellsAlongLine(
                transform.position,
                _targetEnemy.transform.position
            );

            List<Enemy> enemiesInPath = pathCells
                .SelectMany(cell => GridManager.Instance.GetEnemiesInGrid(cell))
                .ToList();

            foreach (Enemy enemy in enemiesInPath)
            {
                float distance = DistanceFromRay(
                    enemy.transform.position,         //The point we measure the distance from.
                    transform.position,               //First point on the line (turret position).
                    _targetEnemy.transform.position   //Second point on the line (target enemy's position).
                );

                if (enemiesHit >= _turretInfo.PierceCount)
                    break;

                if (distance > _bulletWidth)
                    continue;                         //Given the enemy is equal scale on X & Y


                Debug.Log($"Enemy hit: {enemy.name}");
                enemy.TakeDamage(currentDamage);

                enemiesHit++;
                currentDamage *= pierceDamageFalloff;
            }

            _timeSinceLastShot = 0f;
        }

        public float DistanceFromRay(Vector2 point, Vector2 linePoint1, Vector2 linePoint2)
        {
            float A = linePoint2.y - linePoint1.y;
            float B = linePoint1.x - linePoint2.x;
            float C = (linePoint2.x * linePoint1.y) - (linePoint1.x * linePoint2.y);

            float numerator = Mathf.Abs(A * point.x + B * point.y + C);
            float denominator = Mathf.Sqrt(A * A + B * B);

            return numerator / denominator;
        }

        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();

            // Highlight occupied grid cells
            Gizmos.color = Color.red;
            foreach (Vector3 center in pathCells
                         .Select(gridCell => new Vector3(gridCell.x * _cellSize + _cellSize / 2, gridCell.y * _cellSize + _cellSize / 2, 0)))
            {
                Gizmos.DrawWireCube(center, Vector3.one * _cellSize * 0.9f);
            }
        }
    }
}