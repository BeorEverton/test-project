using Assets.Scripts.Enemies;
using Assets.Scripts.Systems;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts.Turrets
{
    public class MissileLauncherTurret : BaseTurret
    {
        [SerializeField] private MissileController _missile;

        private void Update()
        {
            _timeSinceLastShot += Time.deltaTime;
            Attack();
        }

        protected override void Shoot()
        {
            if (_timeSinceLastShot < _turretInfo.FireRate)
                return;

            if (_targetEnemy == null)
                return;

            Enemy enemy = _targetEnemy.GetComponent<Enemy>();
            if (enemy == null)
                return;

            float bonusMultiplier = 1f + GameManager.Instance.spdBonus / 100f;
            float timeToImpact = _turretInfo.FireRate / bonusMultiplier;

            // Predict position based on speed, impact delay, and enemy attack range
            Vector3 startPos = _targetEnemy.transform.position;
            float movementSpeed = enemy.Info.MovementSpeed;
            float distanceToAttackRange = startPos.y - enemy.Info.AttackRange;
            float maxTravelDistance = movementSpeed * timeToImpact;

            float actualTravelDistance = Mathf.Min(distanceToAttackRange, maxTravelDistance);

            // Final predicted position (clamped if enemy would stop)
            Vector3 predictedPosition = startPos + Vector3.down * actualTravelDistance;

            float travelTime = timeToImpact / 2f;
            float fadeTime = timeToImpact / 2f;

            LaunchMissile(predictedPosition, travelTime);

            // Trigger explosion when missile "arrives"
            StartCoroutine(DelayedExplosion(predictedPosition, travelTime, enemy));

            _timeSinceLastShot = 0f;
        }

        public void LaunchMissile(Vector3 targetPosition, float timeToImpact)
        {
            _missile.Launch(targetPosition, timeToImpact);

        }

        private IEnumerator DelayedExplosion(Vector3 target, float delay, Enemy enemy)
        {
            yield return new WaitForSeconds(delay);

            if (target == null)
                yield break;

            CreateExplosion(target, enemy);
        }     
        
        private void CreateExplosion(Vector3 target, Enemy initialTarget)
        {
            
            if (initialTarget != null)
                initialTarget.TakeDamage(_turretInfo.Damage);

            List<Enemy> enemiesInAdjecentGrids = GridManager.Instance.GetEnemiesInRange(target, Mathf.CeilToInt(_turretInfo.ExplosionRadius));

            foreach (Enemy enemy in enemiesInAdjecentGrids
                         .Where(enemy => Vector3.Distance(enemy.transform.position, target) <= _turretInfo.ExplosionRadius))
            {
                enemy.TakeDamage(_turretInfo.SplashDamage);
            }
        }

        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(_targetEnemy != null ? _targetEnemy.transform.position : transform.position, _turretInfo.ExplosionRadius);
        }
    }
}