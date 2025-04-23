using Assets.Scripts.Enemies;
using Assets.Scripts.Systems;
using Assets.Scripts.Systems.Audio;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts.Turrets
{
    public class MissileLauncherTurret : BaseTurret
    {
        [SerializeField] private MissileController _missile;
        [SerializeField] private string _explosionSound;

        protected override void Shoot()
        {
            //base.Shoot(); has custom muzzle flash

            if (_timeSinceLastShot < _stats.FireRate)
                return;

            if (_targetEnemy == null)
                return;

            Enemy enemy = _targetEnemy.GetComponent<Enemy>();
            if (enemy == null)
                return;

            float timeToImpact = _stats.FireRate / _bonusSpdMultiplier;

            // Predict position based on speed, impact delay, and enemy attack range
            Vector3 enemyStartPos = _targetEnemy.transform.position;
            float enemyMovementSpeed = enemy.Info.MovementSpeed;
            float disBetweenEnemyPosAndAtkRange = enemyStartPos.y - enemy.Info.AttackRange;
            float maxTravelDistance = enemyMovementSpeed;

            float actualTravelDistance = Mathf.Min(disBetweenEnemyPosAndAtkRange, maxTravelDistance);

            // Final predicted position (clamped if enemy would stop)
            Vector3 predictedPosition = enemyStartPos + Vector3.down * actualTravelDistance;

            float travelTime = timeToImpact / 4f;

            LaunchMissile(predictedPosition, travelTime);

            StartCoroutine(PlayMuzzleFlashWhileMissileFlying(travelTime));

            // Trigger explosion when missile "arrives"
            StartCoroutine(DelayedExplosion(predictedPosition, travelTime));

            _timeSinceLastShot = 0f;
        }

        public void LaunchMissile(Vector3 targetPosition, float timeToImpact)
        {
            _missile.Launch(targetPosition, timeToImpact);
        }

        private IEnumerator PlayMuzzleFlashWhileMissileFlying(float duration)
        {
            SpriteRenderer sr = _muzzleFlashPosition.GetComponent<SpriteRenderer>();
            float timer = 0f;
            float flashInterval = 0.06f;

            while (timer < duration)
            {
                if (_muzzleFlashSprites.Count > 0)
                {
                    Sprite randomMuzzleFlash = _muzzleFlashSprites[Random.Range(0, _muzzleFlashSprites.Count)];
                    sr.sprite = randomMuzzleFlash;
                }

                yield return new WaitForSeconds(flashInterval);
                timer += flashInterval;
            }

            // Clear the muzzle flash when missile hits
            sr.sprite = null;
        }


        private IEnumerator DelayedExplosion(Vector3 target, float delay)
        {
            yield return new WaitForSeconds(delay);

            CreateExplosion(target);
        }

        private void CreateExplosion(Vector3 target)
        {
            List<Enemy> enemiesInAdjecentGrids = GridManager.Instance.GetEnemiesInRange(target, Mathf.CeilToInt(_stats.ExplosionRadius));
            float impactArea = _stats.ExplosionRadius / 3;
            AudioManager.Instance.PlayWithVariation(_explosionSound, 0.5f, 1f);

            foreach (Enemy enemy in enemiesInAdjecentGrids
                         .Where(enemy => Vector3.Distance(enemy.transform.position, target) <= impactArea))
            {
                enemy.TakeDamage(_damage);
            }

            foreach (Enemy enemy in enemiesInAdjecentGrids
                         .Where(enemy => Vector3.Distance(enemy.transform.position, target) > impactArea &&
                             Vector3.Distance(enemy.transform.position, target) <= _stats.ExplosionRadius))
            {
                enemy.TakeDamage(_stats.SplashDamage * _bonusDmgMultiplier);
            }
        }

        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();
            if (_targetEnemy == null)
                return;

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(_targetEnemy.transform.position, _stats.ExplosionRadius);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(_targetEnemy.transform.position, _stats.ExplosionRadius / 3);
        }
    }
}