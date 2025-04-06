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

            float finalDamage = _damage;
            bool isCritical = IsCriticalHit();

            if (isCritical)
            {
                finalDamage *= 1f + (_turretInfo.CriticalDamageMultiplier / 100f);
            }

            _targetEnemy.GetComponent<Enemy>().TakeDamage(finalDamage, isCritical);
            _timeSinceLastShot = 0f;
        }

        private bool IsCriticalHit()
        {
            return Random.Range(0, 100) < _turretInfo.CriticalChance;
        }
    }
}