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


            float pierceDamageMultiplier = 1f - (_stats.PierceDamageFalloff / 100f);
            float currentDamage = _damage;
            bool firstHit = true;
            bool stop = false;

            Vector2 startPos = _muzzleFlashPosition.position;
            Vector2 dir = (_targetEnemy.transform.position - (Vector3)startPos).normalized;

            // Extend the line: e.g. 20 more units, or your entire screen height
            float extraDistance = 20f;
            float distanceToTarget = Vector2.Distance(startPos, _targetEnemy.transform.position);
            Vector2 extendedPos = startPos + dir * (distanceToTarget + extraDistance);

            pathCells = GridRaycaster.GetCellsAlongLine(
                startPos,
                extendedPos,
                maxSteps: 100 // or however many steps you need
            );

            HashSet<Enemy> hitEnemies = new();

            foreach (Vector2Int cell in pathCells)
            {
                if (stop)
                {
                    break;
                }

                var enemiesInCell = GridManager.Instance.GetEnemiesInGrid(cell);
                // Make a copy to not modify the original list
                var enemiesInCellCopy = new List<Enemy>(GridManager.Instance.GetEnemiesInGrid(cell));

                // Log each cell & number of enemies
                if (enemiesInCellCopy == null || enemiesInCellCopy.Count == 0)
                {
                    continue;
                }

                foreach (Enemy enemy in enemiesInCellCopy)
                {
                    if (enemy == null)
                    {
                        continue;
                    }

                    if (hitEnemies.Contains(enemy))
                    {
                        continue;
                    }

                    // First hit always succeeds
                    if (!firstHit)
                    {
                        float roll = Random.Range(0f, 100f);
                        if (roll > _stats.PierceChance)
                        {
                            stop = true;
                            break;
                        }
                    }

                    // Deal damage
                    enemy.TakeDamage(currentDamage);

                    // Damage falloff
                    currentDamage *= pierceDamageMultiplier;

                    // Mark that we’ve now hit at least once
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