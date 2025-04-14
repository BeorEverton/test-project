using Assets.Scripts.Enemies;
using UnityEngine;

namespace Assets.Scripts.Turrets
{
    public class MachineGunTurret : BaseTurret
    {
        private Recoil _recoil;

        private void Start()
        {
            _recoil = GetComponent<Recoil>();
        }

        protected override void Update()
        {
            base.Update();
            _recoil.ApplyBarrelRecoil();
        }

        protected override void Shoot()
        {
            base.Shoot();

            _recoil.AddRecoil();

            float finalDamage = _damage;
            bool isCritical = IsCriticalHit();

            if (isCritical)
            {
                finalDamage *= 1f + (_stats.CriticalDamageMultiplier / 100f);
            }

            _targetEnemy.GetComponent<Enemy>().TakeDamage(finalDamage, isCritical);
            _timeSinceLastShot = 0f;
        }

        private bool IsCriticalHit()
        {
            return Random.Range(0, 100) < _stats.CriticalChance;
        }
    }
}