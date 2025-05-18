using Assets.Scripts.Enemies;
using Assets.Scripts.Systems;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts.Turrets
{
    public class SniperTurret : BaseTurret
    {
        private List<Vector2Int> _pathCells = new();
        private readonly float _cellSize = 1f;
        private readonly float _bulletWidth = 0.1f;

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
            base.Shoot();
            _timeSinceLastShot = 0f;

            _recoil.AddRecoil();

            float pierceDamageMultiplier = _stats.PierceDamageFalloff / 100f;
            float currentDamage = _stats.Damage;
            bool firstHit = true;

            Vector2 startPos = _muzzleFlashPosition.position;
            Vector2 dir = (_targetEnemy.transform.position - (Vector3)startPos).normalized;

            // Extend the line: e.g. 20 more units, or your entire screen height
            float extraDistance = 0f;
            float distanceToTarget = Vector2.Distance(startPos, _targetEnemy.transform.position);
            Vector2 extendedPos = startPos + dir * (distanceToTarget + extraDistance);

            _pathCells = GridRaycaster.GetCellsAlongLine(
                startPos,
                extendedPos,
                maxSteps: 30 // or however many steps you need
            );

            List<Enemy> enemiesInPath = _pathCells
                .SelectMany(cell => GridManager.Instance.GetEnemiesInGrid(cell))
                .ToList();

            foreach (Enemy enemy in enemiesInPath)
            {
                if (enemy == null)
                    continue;

                float distance = DistanceFromBulletLine(
                    enemy.transform.position,           //The point we measure the distance from.
                    transform.position,                 //First point on the line (turret position).
                    extendedPos                         //Second point on the line (target enemy's position).
                );

                if (distance > _bulletWidth)
                    continue;

                if (firstHit)
                    firstHit = false;
                else
                {
                    float roll = Random.Range(0f, 100f);
                    if (roll > _stats.PierceChance)
                        break;
                }

                enemy.TakeDamage(currentDamage);
                currentDamage *= pierceDamageMultiplier;
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
            foreach (Vector3 center in _pathCells
                         .Select(gridCell => new Vector3(gridCell.x * _cellSize + _cellSize / 2, gridCell.y * _cellSize + _cellSize / 2, 0)))
            {
                Gizmos.DrawWireCube(center, Vector3.one * _cellSize * 0.9f);
            }
        }

        public override float GetDPS()
        {
            float baseDamage = _stats.Damage;
            float fireRate = _stats.FireRate;
            float critChance = Mathf.Clamp01(_stats.CriticalChance / 100f);
            float critMultiplier = _stats.CriticalDamageMultiplier / 100f;
            float pierceChance = Mathf.Clamp01(_stats.PierceChance / 100f);
            float pierceFalloff = _stats.PierceDamageFalloff / 100f;
            float bonusDpsPercent = _stats.PercentBonusDamagePerSec / 100f;

            float damage = baseDamage * (1f + critChance * (critMultiplier - 1f));
            damage *= (1f + bonusDpsPercent);

            // Estimate 1.5 hits per shot on average for piercing
            float averageHits = 1f + pierceChance * 0.5f;
            float effectiveDamage = damage * averageHits;

            return effectiveDamage * fireRate;
        }

    }
}