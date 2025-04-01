using Assets.Scripts.Enemies;
using Assets.Scripts.Systems;
using UnityEngine;

namespace Assets.Scripts.Turrets
{
    public class LaserTurret : BaseTurret
    {
        private float _timeOnSameTarget = 0f;
        private GameObject _lastTarget;

        protected override void Shoot()
        {
            base.Shoot();

            _targetEnemy.GetComponent<Enemy>().TakeDamage(_damage);
            _timeSinceLastShot = 0f;
        }
    }
}