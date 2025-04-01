using Assets.Scripts.Enemies;
using Assets.Scripts.SO;
using Assets.Scripts.Systems;
using Assets.Scripts.WaveSystem;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Assets.Scripts.Turrets
{
    public abstract class BaseTurret : MonoBehaviour
    {
        [SerializeField] protected TurretInfoSO _turretInfo;

        [SerializeField] protected Transform _rotationPoint, _barrel, _barrelEnd;
        [SerializeField] private List<Sprite> _mussleFlashSprites;

        protected GameObject _targetEnemy;
        protected float _timeSinceLastShot = 0f;
        protected bool _targetInRange;
        protected float _bonusSpdMultiplier;
        protected float _bonusDmgMultiplier;

        protected float _damage;
        protected float _atkSpeed;

        private bool _targetInAim;
        private float aimSize = .3f;

        protected virtual void Update()
        {
            // Get spd & dmg bonus from GameManager and calculate effective fire rate
            _bonusSpdMultiplier = 1f + GameManager.Instance.spdBonus / 100f;
            _bonusDmgMultiplier = 1f + GameManager.Instance.dmgBonus / 100f;

            // Calculate attack speed and damage
            _damage = _turretInfo.Damage * _bonusDmgMultiplier;
            _atkSpeed = _turretInfo.FireRate / _bonusSpdMultiplier;

            _timeSinceLastShot += Time.deltaTime;
            Attack();
        }

        protected virtual void Attack()
        {
            if (_targetEnemy == null)
            {
                TargetFirst();
            }

            AimTowardsTarget(_bonusSpdMultiplier);

            if (_timeSinceLastShot < _atkSpeed)
                return;

            if (_targetInAim && _targetInRange)
                Shoot();
        }

        protected virtual void Shoot()
        {
            StartCoroutine(ShowMuzzleFlash());
        }

        protected virtual void TargetFirst()
        {
            _targetEnemy = EnemySpawner.Instance.EnemiesAlive
                .OrderBy(enemy => enemy.transform.position.y)
                .FirstOrDefault(y => y.transform.position.y <= 7.5f);

            if (_targetEnemy != null)
                _targetEnemy.GetComponent<Enemy>().OnDeath += Enemy_OnDeath;
        }

        private void Enemy_OnDeath(object sender, EventArgs _)
        {
            if (sender is not Enemy enemy)
                return;

            enemy.GetComponent<Enemy>().OnDeath -= Enemy_OnDeath;
            _targetEnemy = null;
        }

        protected virtual void AimTowardsTarget(float bonusMultiplier)
        {
            if (_targetEnemy == null)
            {
                _targetInRange = false;
                return;
            }

            _targetInRange = true;

            Vector3 direction = _targetEnemy.transform.position - _rotationPoint.position;

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90;

            Quaternion targetRotation = Quaternion.Euler(0, 0, angle);

            _rotationPoint.localRotation = Quaternion.Slerp(
                _rotationPoint.rotation, targetRotation, _turretInfo.RotationSpeed * bonusMultiplier * Time.deltaTime);

            IsAimingOnTarget(angle);
        }

        private void IsAimingOnTarget(float targetAngle)
        {
            if (_targetEnemy == null)
            {
                _targetInAim = false;
                return;
            }

            float currentAngle = _rotationPoint.localRotation.eulerAngles.z;

            float angleDifference = Mathf.Abs(Mathf.DeltaAngle(currentAngle, targetAngle));

            _targetInAim = angleDifference <= _turretInfo.AngleThreshold;
        }

        private IEnumerator ShowMuzzleFlash()
        {
            if (_mussleFlashSprites.Count == 0)
                yield break;

            Sprite randomMuzzleFlash = _mussleFlashSprites[Random.Range(0, _mussleFlashSprites.Count)];
            _barrelEnd.GetComponent<SpriteRenderer>().sprite = randomMuzzleFlash;

            yield return new WaitForSeconds(0.05f);
            _barrelEnd.GetComponent<SpriteRenderer>().sprite = null;
        }

        protected virtual void OnDrawGizmosSelected()
        {
            if (_targetEnemy == null)
                return;

            Vector3 position = _targetEnemy.transform.position;

            Gizmos.color = Color.red;
            Gizmos.DrawLine(position + new Vector3(-aimSize, -aimSize, 0), position + new Vector3(aimSize, aimSize, 0));
            Gizmos.DrawLine(position + new Vector3(-aimSize, aimSize, 0), position + new Vector3(aimSize, -aimSize, 0));
        }
    }
}