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
        public EnemyTarget EnemyTargetChoice = EnemyTarget.First;

        [SerializeField] protected TurretInfoSO _turretInfo;
        [HideInInspector] public TurretStatsInstance SavedStats;
        protected TurretStatsInstance _stats;

        [SerializeField] protected Transform _rotationPoint, _muzzleFlashPosition;
        [SerializeField] protected List<Sprite> _muzzleFlashSprites;

        protected GameObject _targetEnemy;
        protected float _timeSinceLastShot = 0f;
        protected bool _targetInRange;
        protected float _bonusSpdMultiplier;
        protected float _bonusDmgMultiplier;

        protected float _damage;
        protected float _atkSpeed;

        private bool _targetInAim;
        private float aimSize = .3f;

        // Control the attack range based on the screen size
        private float _screenLeft;
        private float _screenRight;
        private float _screenTop;
        private float _screenBottom;

        private int _lastScreenWidth;
        private int _lastScreenHeight;

        // How far from the top the enemy needs to be for the turrets to shoot
        private const float _topSpawnMargin = 1f;
        private float _attackRange;

        protected virtual void Start()
        {
            _stats = SavedStats is { IsUnlocked: true } ? SavedStats : //If the turret is unlocked, use the saved stats
                new TurretStatsInstance(_turretInfo); //If turret is not yet unlocked, use the default stats from the turret info
        }

        protected virtual void OnEnable()
        {
            UpdateScreenBoundsIfNeeded();
        }

        protected virtual void Update()
        {
            // Get spd & dmg bonus from GameManager and calculate effective fire rate
            _bonusSpdMultiplier = 1f + GameManager.Instance.spdBonus / 100f;
            _bonusDmgMultiplier = 1f + GameManager.Instance.dmgBonus / 100f;

            // Calculate attack speed and damage
            _damage = _stats.Damage * _bonusDmgMultiplier;
            _atkSpeed = _stats.FireRate / _bonusSpdMultiplier;

            _timeSinceLastShot += Time.deltaTime;
            Attack();
        }

        protected virtual void Attack()
        {
            UpdateScreenBoundsIfNeeded();

            if (_targetEnemy == null || !_targetEnemy.activeInHierarchy)
            {
                _targetInRange = false;
                TargetEnemy();
            }

            AimTowardsTarget(_bonusSpdMultiplier);

            if (_timeSinceLastShot < _atkSpeed)
                return;

            if (_targetInAim && _targetInRange && IsTargetVisibleOnScreen())
                Shoot();
        }

        protected virtual void Shoot()
        {
            StartCoroutine(ShowMuzzleFlash());
        }

        protected virtual void TargetEnemy()
        {
            List<GameObject> enemiesAlive = EnemySpawner.Instance.EnemiesAlive;
            _targetEnemy = EnemyTargetChoice switch
            {
                EnemyTarget.First => enemiesAlive
                    .OrderBy(enemy => enemy.transform.position.y)
                    .FirstOrDefault(y => y.transform.position.y <= _attackRange),

                EnemyTarget.Strongest => enemiesAlive
                    .OrderByDescending(enemy => enemy.GetComponent<Enemy>().MaxHealth)
                    .FirstOrDefault(y => y.transform.position.y <= _attackRange),

                EnemyTarget.Random => enemiesAlive
                    .OrderBy(_ => Random.value)
                    .FirstOrDefault(y => y.transform.position.y <= _attackRange),

                _ => enemiesAlive
                    .OrderBy(enemy => enemy.transform.position.y)
                    .FirstOrDefault(y => y.transform.position.y <= _attackRange)
            };

            if (_targetEnemy != null)
                _targetEnemy.GetComponent<Enemy>().OnDeath += Enemy_OnDeath;
        }

        private bool IsTargetVisibleOnScreen()
        {
            if (!_targetEnemy.activeInHierarchy)
                return false;

            Vector3 pos = _targetEnemy.transform.position;
            return pos.x >= _screenLeft && pos.x <= _screenRight &&
                   pos.y >= _screenBottom && pos.y <= (_screenTop - _topSpawnMargin);
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
                _rotationPoint.rotation, targetRotation, _stats.RotationSpeed * bonusMultiplier * Time.deltaTime);

            IsAimingOnTarget(angle);
        }

        protected virtual void IsAimingOnTarget(float targetAngle)
        {
            if (_targetEnemy == null)
            {
                _targetInAim = false;
                return;
            }

            float currentAngle = _rotationPoint.localRotation.eulerAngles.z;

            float angleDifference = Mathf.Abs(Mathf.DeltaAngle(currentAngle, targetAngle));

            _targetInAim = angleDifference <= _stats.AngleThreshold;
        }

        private IEnumerator ShowMuzzleFlash()
        {
            if (_muzzleFlashSprites.Count == 0)
                yield break;

            Sprite randomMuzzleFlash = _muzzleFlashSprites[Random.Range(0, _muzzleFlashSprites.Count)];
            _muzzleFlashPosition.GetComponent<SpriteRenderer>().sprite = randomMuzzleFlash;

            yield return new WaitForSeconds(0.03f);
            _muzzleFlashPosition.GetComponent<SpriteRenderer>().sprite = null;
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

        public TurretStatsInstance GetStats() => _stats;
        public void UnlockTurret() => _stats.IsUnlocked = true;

        private void UpdateScreenBoundsIfNeeded()
        {
            if (Screen.width == _lastScreenWidth && Screen.height == _lastScreenHeight)
                return;

            Camera cam = Camera.main;
            _screenLeft = cam.ViewportToWorldPoint(new Vector3(0, 0, 0)).x;
            _screenRight = cam.ViewportToWorldPoint(new Vector3(1, 0, 0)).x;
            _screenBottom = cam.ViewportToWorldPoint(new Vector3(0, 0, 0)).y;
            _screenTop = cam.ViewportToWorldPoint(new Vector3(0, 1, 0)).y;

            _lastScreenWidth = Screen.width;
            _lastScreenHeight = Screen.height;

            UpdateAttackRange();
        }

        private void UpdateAttackRange()
        {
            _attackRange = _screenTop - _topSpawnMargin;
        }

        public void SetTarget(int index)
        {
            Debug.Log("Setting index on " + name + "" + index);
            switch (index)
            {
                case 0:
                    EnemyTargetChoice = EnemyTarget.First;
                    break;
                case 1:
                    EnemyTargetChoice = EnemyTarget.Strongest;
                    break;
                case 2:
                    EnemyTargetChoice = EnemyTarget.Random;
                    break;
                default:
                    EnemyTargetChoice = EnemyTarget.First;
                    break;

            }
        }
    }

    public enum EnemyTarget
    {
        First,
        Strongest,
        Random
    }
}