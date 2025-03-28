using Assets.Scripts.Enemies;
using UnityEngine;

namespace Assets.Scripts.Turrets
{
    public class MachineGunTurret : BaseTurret
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

            float damage = _turretInfo.Damage;
            if (IsCriticalHit())
                damage *= (1 + (_turretInfo.CriticalDamageMultiplier / 100));

            _targetEnemy.GetComponent<Enemy>().TakeDamage(damage);
            _timeSinceLastShot = 0f;
        }

        private bool IsCriticalHit()
        {
            return Random.Range(0, 100) < _turretInfo.CriticalChance;
        }
    }
}