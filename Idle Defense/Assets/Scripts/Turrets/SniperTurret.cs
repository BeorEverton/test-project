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

        private Recoil recoil;

        private void Start()
        {
            recoil = GetComponent<Recoil>();
        }

        protected override void Update()
        {
            base.Update();
            recoil.ApplyBarrelRecoil();
        }

        protected override void Shoot()
        {
            base.Shoot();

            recoil.AddRecoil();

            int enemiesHit = 0;

            float pierceDamageFalloff = 1f - _stats.PierceDamageFalloff / 100f;
            float currentDamage = _damage;

            pathCells = GridRaycaster.GetCellsAlongLine(
                transform.position,
                _targetEnemy.transform.position
            );

            List<Enemy> enemiesInPath = pathCells
                .SelectMany(cell => GridManager.Instance.GetEnemiesInGrid(cell))
                .ToList();

            foreach (Enemy enemy in enemiesInPath)
            {
                if (enemy == null || _targetEnemy == null)
                    continue;

                float distance = DistanceFromBulletLine(
                    enemy.transform.position,         //The point we measure the distance from.
                    transform.position,               //First point on the line (turret position).
                    _targetEnemy.transform.position   //Second point on the line (target enemy's position).
                );

                if (enemiesHit > _stats.PierceCount)
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