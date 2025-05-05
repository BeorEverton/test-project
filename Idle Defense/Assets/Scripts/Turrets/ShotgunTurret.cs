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

        private Dictionary<Vector2, List<Vector2Int>> _cellsInPathToTarget = new();
        private List<Vector2> _pelletTargetPositions = new();
        private List<Vector2Int> _pathCells = new();

        private float _cellSize = 1f;
        private float _pelletWidth = 0.5f;

        private Recoil _recoil;

        protected override void Start()
        {
            base.Start();
            _recoil = GetComponent<Recoil>();
        }

        protected override void Update()
        {
            base.Update();
            _recoil.ApplyBarrelRecoil();
        }

        protected override void Shoot()
        {
            _pathCells.Clear();
            _pelletTargetPositions.Clear();
            _cellsInPathToTarget.Clear();

            base.Shoot();

            _recoil.AddRecoil();

            Vector2 baseDir = ((_targetEnemy.transform.position - transform.position)).normalized;

            if (_stats.PelletCount % 2 == 1)
            {
                FireWithUnevenPelletCount(baseDir);
            }
            else
            {
                FirePellet(baseDir, 0f); //Shoot at the initial target
                FireWithEvenPelletCount(baseDir);
            }

            /*foreach (Vector2 targetPos in _pelletTargetPositions)
            {
                List<Enemy> enemiesInPath = GetEnemiesInPathToTarget(targetPos);
                SortListByDistance(enemiesInPath);

                foreach (Enemy enemy in enemiesInPath)
                {
                    float distance = DistanceFromLine(enemy.transform.position, transform.position, targetPos);
                    if (distance <= _pelletWidth)
                    {
                        float distanceToEnemy = Vector2.Distance(transform.position, enemy.transform.position);
                        float damage = _damage - GetDamageFalloff(distanceToEnemy);

                        enemy.TakeDamage(damage);
                        break; // Only hit the first enemy in the path
                    }
                }
            }*/

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

                        float distance = DistanceFromLine(enemy.transform.position, transform.position, targetPos);
                        if (distance <= _pelletWidth)
                        {
                            float distToEnemy = Vector2.Distance(transform.position, enemy.transform.position);

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
            int pelletsRemaining = _stats.PelletCount;
            int index = 0;

            while (pelletsRemaining > 0 && sortedCount > 0)
            {
                Enemy enemy = enemyBuffer[sortedIndices[index]];
                float distToEnemy = enemyDistances[sortedIndices[index]];
                float damage = _damage - GetDamageFalloff(distToEnemy);

                enemy.TakeDamage(damage);
                pelletsRemaining--;

                Vector2 knockDir = (enemy.transform.position - transform.position).normalized;
                float knockback = _stats.KnockbackStrength;
                enemy.KnockbackVelocity = new Vector2(0f, knockback * 1f);

                enemy.KnockbackTime = 0.2f;


                index = (index + 1) % sortedCount;
            }

            _timeSinceLastShot = 0f;
        }

        private void FireWithUnevenPelletCount(Vector2 baseDir)
        {
            for (int i = 0; i < _stats.PelletCount; i++)
            {
                float angleOffset = _stats.PelletCount > 1
                    ? Mathf.Lerp(-spreadAngle / 2f, spreadAngle / 2f, (float)i / (_stats.PelletCount - 1))
                    : 0f;

                FirePellet(baseDir, angleOffset);
            }
        }

        private void FireWithEvenPelletCount(Vector2 baseDir)
        {
            int pelletsRemaining = _stats.PelletCount - 1;
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

        private List<Enemy> GetEnemiesInPathToTarget(Vector2 targetPos)
        {
            List<Vector2Int> path = _cellsInPathToTarget[targetPos];
            return path.SelectMany(cell => GridManager.Instance.GetEnemiesInGrid(cell))
                .Distinct()
                .ToList();
        }

        private void SortListByDistance(List<Enemy> list)
        {
            list.Sort((a, b) =>
                Vector3.Distance(transform.position, a.transform.position)
                    .CompareTo(Vector3.Distance(transform.position, b.transform.position))
            );
        }

        private void FirePellet(Vector2 baseDir, float angleOffset)
        {
            Vector2 pelletDir = RotateVector2(baseDir, angleOffset);

            Vector2 pelletTarget = (Vector2)transform.position + pelletDir * maxRange;
            _pelletTargetPositions.Add(pelletTarget);

            List<Vector2Int> pelletPathCells = GridRaycaster.GetCellsAlongLine(transform.position, pelletTarget);
            _pathCells.AddRange(pelletPathCells);

            _cellsInPathToTarget.Add(pelletTarget, pelletPathCells);
        }

        private float GetDamageFalloff(float distance)
        {
            float minFalloffDistance = 3f;
            float maxDamageFalloff = _damage * 0.9f; // cap at 90% reduction

            if (distance <= minFalloffDistance)
                return 0f;

            float effectiveDistance = distance - minFalloffDistance;
            float damageFalloff = _damage * effectiveDistance * _stats.DamageFalloffOverDistance / 100f;

            return Mathf.Min(damageFalloff, maxDamageFalloff);
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
            foreach (Vector3 center in _pathCells
                         .Select(cell => new Vector3(cell.x * _cellSize + _cellSize / 2, cell.y * _cellSize + _cellSize / 2, 0)))
            {
                Gizmos.DrawWireCube(center, Vector3.one * _cellSize * 0.9f);
            }

            // Draw damage falloff zones
            Vector3 turretPosition = transform.position;

            // Red Circle at 3 units - close range (no falloff)
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(turretPosition, 3f);

            // Yellow Circle at 6 units - mid range (some falloff)
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(turretPosition, 6f);

            // White Circle at 11 units - max range (near max falloff)
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(turretPosition, 11f);
        }

        public override float GetDPS()
        {
            float baseDamage = _stats.Damage;
            float fireRate = _stats.FireRate;
            float critChance = Mathf.Clamp01(_stats.CriticalChance / 100f);
            float critMultiplier = _stats.CriticalDamageMultiplier / 100f;
            float bonusDpsPercent = _stats.PercentBonusDamagePerSec / 100f;
            int pelletCount = _stats.PelletCount;

            float pelletDamage = baseDamage * (1f + critChance * (critMultiplier - 1f));
            pelletDamage *= (1f + bonusDpsPercent);

            return pelletDamage * pelletCount * fireRate;
        }

    }
}