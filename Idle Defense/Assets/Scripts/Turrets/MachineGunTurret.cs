using Assets.Scripts.Enemies;
using Assets.Scripts.Systems;
using UnityEngine;

namespace Assets.Scripts.Turrets
{
    public class MachineGunTurret : BaseTurret
    {
        // Track Recoil
        private Vector3 _barrelOriginalLocalPos;
        private float _recoilTimer = 0f;
        private const float _recoilDuration = 0.1f;
        private const float _recoilDistance = 0.15f;

        private void Start()
        {
            _barrelOriginalLocalPos = _barrel.localPosition;
        }

        protected override void Update()
        {
            base.Update();
            ApplyBarrelRecoil();
        }

        private void ApplyBarrelRecoil()
        {
            if (!(_recoilTimer > 0f))
                return;

            _recoilTimer -= Time.deltaTime;

            float time = 1f - (_recoilTimer / _recoilDuration);
            _barrel.localPosition = Vector3.Lerp(
                _barrelOriginalLocalPos + Vector3.up * _recoilDistance,
                _barrelOriginalLocalPos,
                time
            );
        }

        protected override void Shoot()
        {
            base.Shoot();

            _recoilTimer = _recoilDuration;
            _barrel.localPosition = _barrelOriginalLocalPos + Vector3.up * _recoilDistance;

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