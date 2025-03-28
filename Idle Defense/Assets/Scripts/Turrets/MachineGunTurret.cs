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

            _targetEnemy.GetComponent<Enemy>().TakeDamage(_turretInfo.Damage);
            _timeSinceLastShot = 0f;
        }
    }
}