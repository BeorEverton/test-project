using Assets.Scripts.Enemies;
using UnityEngine;

namespace Assets.Scripts.Turrets
{
    public class LaserTurret : BaseTurret
    {
        private float _timeOnSameTarget = 0f;
        private float _rampedDamageBonus = 0f;
        private GameObject _lastTarget;

        private float _bonusDmgPerSec;

        private void Awake()
        {
            _bonusDmgPerSec = 1f + _turretInfo.ProcentBonusDamagePerSec / 100f;
        }

        protected override void Update()
        {
            base.Update();
            if (_targetEnemy != _lastTarget)
            {
                _lastTarget = _targetEnemy;
                _timeOnSameTarget = 0f;
                _rampedDamageBonus = 0f;
            }

            RampDamageOverTime();
        }
        protected override void Shoot()
        {
            base.Shoot();

            _targetEnemy.GetComponent<Enemy>().TakeDamage(_damage + _rampedDamageBonus);
            _timeSinceLastShot = 0f;
        }

        private void RampDamageOverTime()
        {
            if (_targetEnemy == null)
                return;

            _timeOnSameTarget += Time.deltaTime;
            _rampedDamageBonus = _turretInfo.Damage * _bonusDmgPerSec * _timeOnSameTarget;
        }
    }
}