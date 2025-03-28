using Assets.Scripts.Enemies;
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
            Collider2D[] explosionHits = Physics2D.OverlapCircleAll(target, _turretInfo.ExplosionRadius);

            //Do some grid calculations to find the enemies in the explosion radius, without running through all enemies
            foreach (Collider2D hit in explosionHits)
            {
                Debug.Log($"Explosion hit {hit.transform.gameObject.name}");
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
