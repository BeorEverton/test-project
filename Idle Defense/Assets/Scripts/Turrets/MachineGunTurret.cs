using Assets.Scripts.Enemies;
using Assets.Scripts.Systems;
using UnityEngine;

namespace Assets.Scripts.Turrets
{
    public class MachineGunTurret : BaseTurret
    {
        private Recoil recoil;

        private void Start()
        {
            recoil = GetComponent<Recoil>();
        }

        protected override void Update()
        {
            base.Update();
            recoil.ApplyBarrelRecoil();
        }

        protected override void Shoot()
        {
            base.Shoot();

            recoil.AddRecoil();
            // Get dmg bonus from GameManager and calculate effective damage
            float damage = _turretInfo.Damage * (1f + GameManager.Instance.dmgBonus / 100f);

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