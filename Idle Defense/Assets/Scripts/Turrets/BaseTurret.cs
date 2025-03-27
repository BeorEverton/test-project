using Assets.Scripts.SO;
using Assets.Scripts.WaveSystem;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Assets.Scripts.Turrets
{
    public abstract class BaseTurret : MonoBehaviour
    {
        protected GameObject _targetEnemy;
        [SerializeField] protected TurretInfoSO _turretInfo;
        [SerializeField] protected Transform _rotationPoint;

        protected virtual void Attack()
        {
            TargetFirst();
            AimTowardsTarget();
            Shoot();
        }

        protected virtual void Shoot()
        {
            //Debug.LogWarning($"[BASETURRET] Shoot not implemented");
        }

        protected virtual void TargetFirst()
        {
            _targetEnemy = EnemySpawner.Instance.EnemiesCurrentWave
                .OrderBy(enemy => enemy.transform.position.y)
                .FirstOrDefault();
        }

        protected virtual void AimTowardsTarget()
        {
            if (_targetEnemy == null)
                return;

            if (Vector3.Distance(transform.position, _targetEnemy.transform.position) > _turretInfo.Range)
                return;

            Vector3 direction = _targetEnemy.transform.position - _rotationPoint.position;

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            Quaternion targetRotation = Quaternion.Euler(0, 0, angle - 90);

            _rotationPoint.localRotation = Quaternion.Slerp(
                _rotationPoint.rotation, targetRotation, _turretInfo.RotationSpeed * Time.deltaTime);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Handles.DrawWireArc(transform.position, Vector3.forward, Vector3.right, 360, _turretInfo.Range);
        }
#endif
    }
}