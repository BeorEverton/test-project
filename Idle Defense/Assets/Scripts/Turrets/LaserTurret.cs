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

            // Get dmg bonus from GameManager and calculate effective damage
            float damage = _turretInfo.Damage * (1f + GameManager.Instance.dmgBonus / 100f);

            _targetEnemy.GetComponent<Enemy>().TakeDamage(damage);
            _timeSinceLastShot = 0f;
        }
    }
}