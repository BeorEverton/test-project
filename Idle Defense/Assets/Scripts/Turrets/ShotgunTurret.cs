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
        private int _pelletCount;

        private Recoil _recoil;

        protected override void Update()
        {
            base.Update();
            _recoil.ApplyBarrelRecoil();
        }

        private void Start()
        {
            _pelletCount = _turretInfo.PelletCount;
            _recoil = GetComponent<Recoil>();
        }

        protected override void Shoot()
        {
            _pathCells.Clear();
            _pelletTargetPositions.Clear();
            _cellsInPathToTarget.Clear();

            base.Shoot();

            _recoil.AddRecoil();

            Vector2 baseDir = ((_targetEnemy.transform.position - transform.position)).normalized;

            if (_pelletCount % 2 == 1)
            {
                FireWithUnevenPelletCount(baseDir);
            }
            else
            {
                FirePellet(baseDir, 0f); //Shoot at the initial target
                FireWithEvenPelletCount(baseDir);
            }

            foreach (Vector2 targetPos in _pelletTargetPositions)
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
            }

            _timeSinceLastShot = 0f;
        }

        private void FireWithUnevenPelletCount(Vector2 baseDir)
        {
            for (int i = 0; i < _pelletCount; i++)
            {
                float angleOffset = _pelletCount > 1
                    ? Mathf.Lerp(-spreadAngle / 2f, spreadAngle / 2f, (float)i / (_pelletCount - 1))
                    : 0f;

                FirePellet(baseDir, angleOffset);
            }
        }

        private void FireWithEvenPelletCount(Vector2 baseDir)
        {
            int pelletsRemaining = _pelletCount - 1;
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
            foreach (Vector3 center in _pathCells
                         .Select(cell => new Vector3(cell.x * _cellSize + _cellSize / 2, cell.y * _cellSize + _cellSize / 2, 0)))
            {
                Gizmos.DrawWireCube(center, Vector3.one * _cellSize * 0.9f);
            }
        }
    }
}