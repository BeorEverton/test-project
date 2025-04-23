using Assets.Scripts.Enemies;
using Assets.Scripts.Systems.Audio;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

namespace Assets.Scripts.Turrets
{
    public class LaserTurret : BaseTurret
    {
        private float _timeOnSameTarget = 0f;
        private float _rampedDamageBonus = 0f;
        private GameObject _lastTarget;

        private float _bonusDmgPerSec;

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

            if (_lastTarget != null)
                DrawLaser();
            else
                RemoveLaser();

            RampDamageOverTime();
        }

        protected override void Shoot()
        {
            base.Shoot();

            Enemy enemy = _targetEnemy.GetComponent<Enemy>();
            enemy.TakeDamage(_damage + _rampedDamageBonus);

            if (!enemy.IsSlowed)
                enemy.ReduceMovementSpeed(_stats.SlowEffect);

            _timeSinceLastShot = 0f;
        }

        protected override void AimTowardsTarget(float bonusMultiplier)
        {
            base.AimTowardsTarget(bonusMultiplier);

            //if (!_targetInRange)
            //    _laserLine.enabled = false; // Disable the laser when not shooting
        }

        private void DrawLaser()
        {
            if (!_targetInAim)
            {
                if (_laserLine.enabled)
                    RemoveLaser();
                return;
            }

            _laserLine.enabled = true;

            _laserLine.SetPosition(0, _laserLine.transform.position); // always start from barrel
            _laserLine.SetPosition(1, _lastTarget.transform.position); // always aim at the target
        }

        private void RemoveLaser()
        {
            _laserLine.enabled = false;
        }

        private void RampDamageOverTime()
        {
            if (_targetEnemy == null)
                return;

            _timeOnSameTarget += Time.deltaTime;
            _rampedDamageBonus = _stats.Damage * _bonusDmgPerSec * _timeOnSameTarget;
        }

        protected override void Enemy_OnDeath(object sender, EventArgs _)
        {
            base.Enemy_OnDeath(sender, _);

            AudioManager.Instance.Stop(_currentShotSound);
        }
    }
}