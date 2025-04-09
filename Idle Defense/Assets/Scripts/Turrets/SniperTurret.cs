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
            _timeSinceLastShot = 0f;

            recoil.AddRecoil();

            float pierceChance = _stats.PierceChance;
            float pierceDamageMultiplier = 1f - (_stats.PierceDamageFalloff / 100f);

            float currentDamage = _damage;
            bool firstHit = true;
            bool stop = false;

            pathCells = GridRaycaster.GetCellsAlongLine(
                transform.position,
                _targetEnemy.transform.position
            );

            HashSet<Enemy> hitEnemies = new();

            foreach (Vector2Int cell in pathCells)
            {
                if (stop) break;

                var enemies = GridManager.Instance.GetEnemiesInGrid(cell);
                if (enemies == null) continue;

                foreach (Enemy enemy in enemies)
                {
                    if (enemy == null || hitEnemies.Contains(enemy))
                        continue;

                    // First hit always succeeds
                    if (!firstHit)
                    {
                        float roll = Random.Range(0f, 100f);
                        if (roll > pierceChance)
                        {
                            stop = true;
                            break;
                        }
                    }

                    enemy.TakeDamage(currentDamage);
                    currentDamage *= pierceDamageMultiplier;
                    hitEnemies.Add(enemy);
                    firstHit = false;
                }
            }
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