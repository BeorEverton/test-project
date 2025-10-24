using Assets.Scripts.Enemies;
using Assets.Scripts.Systems;
using Assets.Scripts.Systems.Audio;
using UnityEngine;

namespace Assets.Scripts.Turrets
{
    public class MachineGunTurret : BaseTurret
    {


        protected override void Start()
        {
            base.Start();
        }

        protected override void Update()
        {
            base.Update();
        }

        protected override void Shoot()
        {
            base.Shoot();


            float finalDamage = RuntimeStats.Damage;
            bool isCritical = IsCriticalHit();

            if (isCritical)
            {
                AudioManager.Instance.Play("Critical");
                finalDamage *= 1f + (RuntimeStats.CriticalDamageMultiplier / 100f);
            }

            _targetEnemy.GetComponent<Enemy>().TakeDamage(finalDamage, isCritical);

            StatsManager.Instance.AddTurretDamage(_turretInfo.TurretType, finalDamage);
            _timeSinceLastShot = 0f;
        }

        private bool IsCriticalHit()
        {
            return Random.Range(0, 100) < RuntimeStats.CriticalChance;
        }
    }
}