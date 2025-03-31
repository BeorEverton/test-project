using Assets.Scripts.Enemies;
using Assets.Scripts.Systems;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts.Turrets
{
    public class MissileLauncherTurret : BaseTurret
    {
        private void Update()
        {
            _timeSinceLastShot += Time.deltaTime;
            Attack();
        }

        protected override void Shoot()
        {
            if (_timeSinceLastShot < _turretInfo.FireRate)
                return;

            _targetEnemy.GetComponent<Enemy>().TakeDamage(_turretInfo.Damage);

            CreateExplosion(_targetEnemy.transform.position);

            _timeSinceLastShot = 0f;
        }

        private void CreateExplosion(Vector3 target)
        {
            List<Enemy> enemiesInAdjecentGrids = GridManager.Instance.GetEnemiesInRange(target, Mathf.CeilToInt(_turretInfo.ExplosionRadius));

            foreach (Enemy enemy in enemiesInAdjecentGrids
                         .Where(enemy => Vector3.Distance(enemy.transform.position, target) <= _turretInfo.ExplosionRadius))
            {
                enemy.TakeDamage(_turretInfo.SplashDamage);
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(_targetEnemy != null ? _targetEnemy.transform.position : transform.position, _turretInfo.ExplosionRadius);
        }
#endif
    }
}