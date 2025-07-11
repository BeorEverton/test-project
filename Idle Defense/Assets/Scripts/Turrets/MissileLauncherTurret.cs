using Assets.Scripts.Enemies;
using Assets.Scripts.Systems;
using Assets.Scripts.Systems.Audio;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts.Turrets
{
    public class MissileLauncherTurret : BaseTurret
    {
        [SerializeField] private MissileController _missile;
        [SerializeField] private string _explosionSound;

        protected override void Start()
        {
            base.Start();

            _missile.OnMissileHit += Missile_OnMissileHit;
            _missile.SetThrustSprites(_muzzleFlashSprites);
        }

        private void Missile_OnMissileHit(object sender, MissileController.MissileHitEventArgs e)
        {
            CreateExplosion(e.HitPosition);
        }

        protected override void Shoot()
        {
            if (!_targetEnemy.TryGetComponent(out Enemy enemy))
                return;

            float travelTime = CalculateMissileTravelTime();

            // Predict position based on speed, impact delay, and enemy attack range
            Vector3 enemyStartPos = _targetEnemy.transform.position;
            float maxTravelDistance = enemy.Info.MovementSpeed * travelTime;
            float disBetweenEnemyPosAndAtkRange = enemyStartPos.y - enemy.Info.AttackRange;

            float actualTravelDistance = Mathf.Min(disBetweenEnemyPosAndAtkRange, maxTravelDistance);

            // Final predicted position (clamped if enemy would stop)
            Vector3 predictedPosition = enemyStartPos + Vector3.down * actualTravelDistance;

            LaunchMissile(predictedPosition, travelTime);

            _timeSinceLastShot = 0f;
        }

        private float CalculateMissileTravelTime()
        {
            return _atkSpeed > 1f ? 1f : _atkSpeed - 0.05f;
        }

        public void LaunchMissile(Vector3 targetPosition, float timeToImpact)
        {
            _missile.Launch(targetPosition, timeToImpact);
        }

        private void CreateExplosion(Vector3 target)
        {
            List<Enemy> enemiesInAdjecentGrids = GridManager.Instance.GetEnemiesInRange(target, Mathf.CeilToInt(RuntimeStats.ExplosionRadius));
            float impactArea = RuntimeStats.ExplosionRadius / 3;
            AudioManager.Instance.PlayWithVariation(_explosionSound, 0.5f, 1f);

            foreach (Enemy enemy in enemiesInAdjecentGrids
                         .Where(enemy => Vector3.Distance(enemy.transform.position, target) <= impactArea))
            {
                enemy.TakeDamage(RuntimeStats.Damage);
                StatsManager.Instance.AddTurretDamage(_turretInfo.TurretType, RuntimeStats.Damage);
            }

            foreach (Enemy enemy in enemiesInAdjecentGrids
                         .Where(enemy => Vector3.Distance(enemy.transform.position, target) > impactArea &&
                             Vector3.Distance(enemy.transform.position, target) <= RuntimeStats.ExplosionRadius))
            {
                enemy.TakeDamage(RuntimeStats.SplashDamage);
                StatsManager.Instance.AddTurretDamage(_turretInfo.TurretType, RuntimeStats.SplashDamage);
            }
        }

        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();
            if (_targetEnemy == null)
                return;

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(_targetEnemy.transform.position, RuntimeStats.ExplosionRadius);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(_targetEnemy.transform.position, RuntimeStats.ExplosionRadius / 3);
        }
    }
}