using Assets.Scripts.SO;
using Assets.Scripts.Systems;
using Assets.Scripts.WaveSystem;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts.Turrets
{
    public abstract class BaseTurret : MonoBehaviour
    {
        [SerializeField] protected TurretInfoSO _turretInfo;
        [SerializeField] protected Transform _rotationPoint, _barrel, _barrelEnd;
        [SerializeField] private List<Sprite> _firingSprites;

        protected GameObject _targetEnemy;
        protected float _timeSinceLastShot = 0f;
        protected bool _targetInRange;

        private bool _targetInAim;
        private float aimSize = .3f;

        protected virtual void Update()
        {
            _timeSinceLastShot += Time.deltaTime;
            Attack();
        }

        protected virtual void Attack()
        {
            // Get spd bonus from GameManager and calculate effective fire rate
            float bonusMultiplier = 1f + GameManager.Instance.spdBonus / 100f;

            TargetFirst();
            AimTowardsTarget(bonusMultiplier);

            float effectiveFireRate = _turretInfo.FireRate / bonusMultiplier;

            if (_timeSinceLastShot < effectiveFireRate)
                return;

            if (_targetInAim && _targetInRange)
                Shoot();
        }

        protected virtual void Shoot()
        {
            StartCoroutine(PlayFiringAnimation());
        }

        protected virtual void TargetFirst()
        {
            _targetEnemy = EnemySpawner.Instance.EnemiesAlive
                .OrderBy(enemy => enemy.transform.position.y)
                .FirstOrDefault(y => y.transform.position.y <= 7.5f);
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

        private IEnumerator PlayFiringAnimation()
        {
            if (_firingSprites.Count == 0)
                yield break;

            Sprite randomSprite = _firingSprites[Random.Range(0, _firingSprites.Count)];
            _barrelEnd.GetComponent<SpriteRenderer>().sprite = randomSprite;

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