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
                float distance = DistanceFromBulletLine(
                    enemy.transform.position,         //The point we measure the distance from.
                    transform.position,               //First point on the line (turret position).
                    _targetEnemy.transform.position   //Second point on the line (target enemy's position).
                );

                if (enemiesHit >= _turretInfo.PierceCount)
                    break;

                if (distance > _bulletWidth)
                    continue;

                enemy.TakeDamage(currentDamage);

                enemiesHit++;
                currentDamage *= pierceDamageFalloff;
            }

            _timeSinceLastShot = 0f;
        }

        public float DistanceFromBulletLine(Vector2 target, Vector2 turretPos, Vector2 enemyPos)
        {
            float A = enemyPos.y - turretPos.y;
            float B = turretPos.x - enemyPos.x;
            float C = (enemyPos.x * turretPos.y) - (turretPos.x * enemyPos.y);

            float numerator = Mathf.Abs(A * target.x + B * target.y + C);
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