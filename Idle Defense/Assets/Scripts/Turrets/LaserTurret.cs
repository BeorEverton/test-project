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

        private bool _isShooting;

        [SerializeField] private LineRenderer _laserLine;

        protected override void Start()
        {
            base.Start();

            _bonusDmgPerSec = 1f + _stats.PercentBonusDamagePerSec / 100f;
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

            if (_isShooting)
            {
                if (_targetEnemy != null)
                {
                    _laserLine.enabled = true;

                    _laserLine.SetPosition(0, _laserLine.transform.position); // always start from barrel
                    _laserLine.SetPosition(1, _targetEnemy.transform.position); // always aim at live enemy
                }
                else
                {
                    _laserLine.enabled = false;
                    _isShooting = false;
                }
            }

            RampDamageOverTime();
        }

        protected override void AimTowardsTarget(float bonusMultiplier)
        {
            _laserLine.enabled = false;

            base.AimTowardsTarget(bonusMultiplier);
        }

        protected override void Shoot()
        {
            base.Shoot();

            Enemy enemy = _targetEnemy.GetComponent<Enemy>();
            enemy.TakeDamage(_damage + _rampedDamageBonus);

            if (!enemy.IsSlowed)
                enemy.ReduceMovementSpeed(_stats.SlowEffect);

            _isShooting = true;


            _timeSinceLastShot = 0f;
        }

        private void RampDamageOverTime()
        {
            if (_targetEnemy == null)
            {
                _laserLine.enabled = false;
                _isShooting = false;
                return;
            }

            _timeOnSameTarget += Time.deltaTime;
            _rampedDamageBonus = _stats.Damage * _bonusDmgPerSec * _timeOnSameTarget;
        }
    }
}