using Assets.Scripts.Enemies;
using Assets.Scripts.SO;
using Assets.Scripts.Systems;
using Assets.Scripts.Systems.Audio;
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
        [SerializeField] protected string[] _shotSounds;

        [SerializeField] private SpriteRenderer _turretBodyRenderer;
        public Sprite[] _turretUpgradeSprites;
        private int[] _upgradeThresholds = new int[] { 50, 100, 200 };


        protected GameObject _targetEnemy;
        protected float _timeSinceLastShot = 0f;
        protected bool _targetInRange;
        protected bool _targetInAim;
        protected float _bonusSpdMultiplier;

        protected float _atkSpeed;
        protected string _currentShotSound = "";

        private float _aimSize = .3f;

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
            _stats = SavedStats ?? new TurretStatsInstance(_turretInfo);

            UpdateTurretAppearance();
        }

        protected virtual void OnEnable()
        {
            UpdateScreenBoundsIfNeeded();
        }

        protected virtual void Update()
        {
            // Get spd & dmg bonus from GameManager and calculate effective fire rate
            _bonusSpdMultiplier = 1f + GameManager.Instance.spdBonus / 100f;

            // Calculate attack speed and damage
            _atkSpeed = (1 / _stats.FireRate) / _bonusSpdMultiplier;

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

                if (_targetEnemy == null) //If no enemies are alive and in range, stop attacking
                    return;
            }

            AimTowardsTarget(_bonusSpdMultiplier);

            if (_timeSinceLastShot < _atkSpeed)
                return;

            if (_targetInAim && _targetInRange && IsTargetVisibleOnScreen())
                Shoot();
            else
                TargetEnemy();
        }

        protected virtual void Shoot()
        {
            StartCoroutine(ShowMuzzleFlash());
            _currentShotSound = _shotSounds[Random.Range(0, _shotSounds.Length)];
            AudioManager.Instance.PlayWithVariation(_currentShotSound, 0.8f, 1f);
        }

        protected virtual void TargetEnemy()
        {
            if (_targetEnemy != null)
                _targetEnemy.GetComponent<Enemy>().OnDeath -= Enemy_OnDeath;

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

        protected virtual void Enemy_OnDeath(object sender, EventArgs _)
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
            Gizmos.DrawLine(position + new Vector3(-_aimSize, -_aimSize, 0), position + new Vector3(_aimSize, _aimSize, 0));
            Gizmos.DrawLine(position + new Vector3(-_aimSize, _aimSize, 0), position + new Vector3(_aimSize, -_aimSize, 0));
        }

        public TurretStatsInstance GetStats() => _stats;
        public bool IsUnlocked() => _stats.IsUnlocked;
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

            TargetEnemy();

        }

        public void UpdateTurretAppearance()
        {
            if (_turretBodyRenderer == null || _turretUpgradeSprites == null || _turretUpgradeSprites.Length == 0)
                return;

            int totalLevel = GetTotalUpgradeLevel();
            int spriteIndex = 0;

            if (totalLevel >= _upgradeThresholds[2])
                spriteIndex = 3;
            else if (totalLevel >= _upgradeThresholds[1])
                spriteIndex = 2;
            else if (totalLevel >= _upgradeThresholds[0])
                spriteIndex = 1;
            else
                spriteIndex = 0;

            if (spriteIndex < _turretUpgradeSprites.Length)
                _turretBodyRenderer.sprite = _turretUpgradeSprites[spriteIndex];
        }

        private int GetTotalUpgradeLevel()
        {
            return Mathf.FloorToInt(
                _stats.DamageLevel +
                _stats.FireRateLevel +
                _stats.CriticalChanceLevel +
                _stats.CriticalDamageMultiplierLevel +
                _stats.ExplosionRadiusLevel +
                _stats.SplashDamageLevel +
                _stats.PierceChanceLevel +
                _stats.PierceDamageFalloffLevel +
                _stats.PelletCountLevel +
                _stats.DamageFalloffOverDistanceLevel +
                _stats.PercentBonusDamagePerSecLevel +
                _stats.SlowEffectLevel +
                _stats.KnockbackStrengthLevel
            );
        }

        public virtual float GetDPS()
        {
            float baseDamage = _stats.Damage;
            float fireRate = _stats.FireRate;
            float critChance = Mathf.Clamp01(_stats.CriticalChance / 100f);
            float critMultiplier = _stats.CriticalDamageMultiplier / 100f;
            float bonusDpsPercent = _stats.PercentBonusDamagePerSec / 100f;

            // Effective damage per shot with crit chance
            float effectiveDamage = baseDamage * (1f + critChance * (critMultiplier - 1f));
            effectiveDamage *= (1f + bonusDpsPercent);

            return effectiveDamage * fireRate;
        }

    }

    public enum EnemyTarget
    {
        First,
        Strongest,
        Random
    }
}