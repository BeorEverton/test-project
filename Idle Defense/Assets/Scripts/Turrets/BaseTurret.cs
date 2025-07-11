using Assets.Scripts.Enemies;
using Assets.Scripts.SO;
using Assets.Scripts.Systems;
using Assets.Scripts.Systems.Audio;
using Assets.Scripts.WaveSystem;
using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;
using Random = UnityEngine.Random;

namespace Assets.Scripts.Turrets
{
    public abstract class BaseTurret : MonoBehaviour
    {
        public EnemyTarget EnemyTargetChoice = EnemyTarget.First;

        /// <summary>
        /// Reference to the TurretInfoSO that contains the base stats and info for this turret. DONT CHANGE AT RUNTIME!
        /// </summary>
        public TurretInfoSO _turretInfo;
        public TurretStatsInstance RuntimeStats; // For session upgrades
        public TurretStatsInstance PermanentStats;   // Saved base

        [SerializeField] protected Transform _rotationPoint, _muzzleFlashPosition;
        [SerializeField] protected List<Sprite> _muzzleFlashSprites;
        [SerializeField] protected string[] _shotSounds;

        [SerializeField] private SpriteRenderer _turretBodyRenderer;
        public Sprite[] _turretUpgradeSprites;
        private int[] _upgradeThresholds = new int[] { 50, 150, 300 };

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

        public GameObject upgradePulseFX;

        private void OnDestroy() =>
            GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;

        private void HandleGameStateChanged(GameState newState)
        {
            if (newState != GameState.InGame) { return; }

            // new run  discard any previous RuntimeStats and rebuild from Permanent
            RuntimeStats = CloneStatsWithoutLevels(PermanentStats);
            UpdateTurretAppearance();
        }


        protected virtual void Start()
        {
            // If there are no saved permanent stats create a new fresh from the SO
            if (PermanentStats == null)
                PermanentStats = new TurretStatsInstance(_turretInfo);

            // Listen so we can rebuild RuntimeStats every time a new run starts
            GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;

            UpdateTurretAppearance();
        }


        /// <summary>Overwrite the whole stats, preserving level counts, called on load.</summary>
        public void SetPermanentStats(TurretStatsInstance stats)
        {
            PermanentStats = stats;
        }

        protected virtual void OnEnable()
        {
            UpdateScreenBoundsIfNeeded();
            if (RuntimeStats == null && PermanentStats != null)
            {
                RuntimeStats = CloneStatsWithoutLevels(PermanentStats); // New enable, if cleaned runtime, clone the permanent
                Debug.Log("runtime stats was null, cloning from permanent stats on Enable on " + name);
            }
            if (ReferenceEquals(PermanentStats, RuntimeStats))
            {
                Debug.LogError($"{name}  PermanentStats and RuntimeStats are sharing the same reference! This will break upgrades.");
            }
        }

        protected virtual void Update()
        {
            // Get spd & dmg bonus from GameManager and calculate effective fire rate
            _bonusSpdMultiplier = 1f + GameManager.Instance.spdBonus / 100f;

            // Calculate attack speed and damage
            _atkSpeed = (1 / RuntimeStats.FireRate) / _bonusSpdMultiplier;

            _timeSinceLastShot += Time.deltaTime;
            Attack();
        }

        protected virtual void Attack()
        {
            UpdateScreenBoundsIfNeeded();

            // if goes out of range
            if (_targetEnemy != null)
            {
                float ty = _targetEnemy.transform.position.y;
                if (ty > _attackRange)
                {
                    _targetEnemy.GetComponent<Enemy>().OnDeath -= Enemy_OnDeath;
                    _targetEnemy = null;
                    _targetInRange = false;
                }
            }

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
                _rotationPoint.rotation, targetRotation, RuntimeStats.RotationSpeed * bonusMultiplier * Time.deltaTime);

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

            _targetInAim = angleDifference <= RuntimeStats.AngleThreshold;
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

        public bool IsUnlocked() => RuntimeStats.IsUnlocked;
        public void UnlockTurret() => RuntimeStats.IsUnlocked = true;

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
            //Debug.Log("Setting index on " + name + "" + index);
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

            // Called on every upgrade
            AnimateTurretUpgrade(transform);
            PlayUpgradePulse(upgradePulseFX, transform.position);


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
                RuntimeStats.DamageLevel +
                RuntimeStats.FireRateLevel +
                RuntimeStats.CriticalChanceLevel +
                RuntimeStats.CriticalDamageMultiplierLevel +
                RuntimeStats.ExplosionRadiusLevel +
                RuntimeStats.SplashDamageLevel +
                RuntimeStats.PierceChanceLevel +
                RuntimeStats.PierceDamageFalloffLevel +
                RuntimeStats.PelletCountLevel +
                RuntimeStats.DamageFalloffOverDistanceLevel +
                RuntimeStats.PercentBonusDamagePerSecLevel +
                RuntimeStats.SlowEffectLevel +
                RuntimeStats.KnockbackStrengthLevel
            );
        }

        public virtual float GetDPS()
        {
            float baseDamage = RuntimeStats.Damage;
            float fireRate = RuntimeStats.FireRate;
            float critChance = Mathf.Clamp01(RuntimeStats.CriticalChance / 100f);
            float critMultiplier = RuntimeStats.CriticalDamageMultiplier / 100f;
            float bonusDpsPercent = RuntimeStats.PercentBonusDamagePerSec / 100f;

            // Effective damage per shot with crit chance
            float effectiveDamage = baseDamage * (1f + critChance * (critMultiplier - 1f));
            effectiveDamage *= (1f + bonusDpsPercent);

            return effectiveDamage * fireRate;
        }

        public void AnimateTurretUpgrade(Transform turret)
        {
            turret.DOKill(); // Cancel any active tweens

            turret.localScale = Vector3.one; // Ensure starting scale

            // Pop out and return to normal
            Sequence seq = DOTween.Sequence();
            seq.Append(turret.DOScale(1.2f, 0.1f).SetEase(Ease.OutQuad).SetUpdate(true));
            seq.Append(turret.DOScale(1f, 0.1f).SetEase(Ease.InOutSine).SetUpdate(true));
        }

        public void PlayUpgradePulse(GameObject upgradePulseFX, Vector3 position)
        {
            if (upgradePulseFX == null)
            {
                Debug.LogWarning("upgradePulseFX prefab is missing.");
                return;
            }

            GameObject pulse = Instantiate(upgradePulseFX, position, Quaternion.identity);
            SpriteRenderer sr = pulse.GetComponent<SpriteRenderer>();
            if (sr == null)
            {
                Debug.LogWarning("upgradePulseFX is missing a SpriteRenderer.");
                return;
            }

            float duration = 0.4f;
            float targetScale = 2.0f;
            float startAlpha = 0.8f;

            pulse.transform.localScale = Vector3.one * 0.5f;
            sr.color = new Color(1f, 1f, 1f, startAlpha);

            Sequence seq = DOTween.Sequence().SetUpdate(true);
            seq.Join(pulse.transform.DOScale(targetScale, duration).SetEase(Ease.OutCubic));
            seq.Join(sr.DOFade(0f, duration).SetEase(Ease.Linear));
            seq.OnComplete(() => Destroy(pulse));
        }

        public static TurretStatsInstance CloneStatsWithoutLevels(TurretStatsInstance src)
        {
            return new TurretStatsInstance
            {
                IsUnlocked = src.IsUnlocked,
                TurretType = src.TurretType,

                BaseDamage = src.BaseDamage,
                BaseFireRate = src.BaseFireRate,
                BaseCritChance = src.BaseCritChance,
                BaseCritDamage = src.BaseCritDamage,

                Damage = src.Damage,
                DamageLevel = 0,
                DamageUpgradeAmount = src.DamageUpgradeAmount,
                DamageUpgradeBaseCost = src.DamageUpgradeBaseCost,
                DamageCostExponentialMultiplier = src.DamageCostExponentialMultiplier,

                FireRate = src.FireRate,
                FireRateLevel = 0,
                FireRateUpgradeAmount = src.FireRateUpgradeAmount,
                FireRateUpgradeBaseCost = src.FireRateUpgradeBaseCost,
                FireRateCostExponentialMultiplier = src.FireRateCostExponentialMultiplier,

                CriticalChance = src.CriticalChance,
                CriticalChanceLevel = 0,
                CriticalChanceUpgradeAmount = src.CriticalChanceUpgradeAmount,
                CriticalChanceUpgradeBaseCost = src.CriticalChanceUpgradeBaseCost,
                CriticalChanceCostExponentialMultiplier = src.CriticalChanceCostExponentialMultiplier,

                CriticalDamageMultiplier = src.CriticalDamageMultiplier,
                CriticalDamageMultiplierLevel = 0,
                CriticalDamageMultiplierUpgradeAmount = src.CriticalDamageMultiplierUpgradeAmount,
                CriticalDamageMultiplierUpgradeBaseCost = src.CriticalDamageMultiplierUpgradeBaseCost,
                CriticalDamageCostExponentialMultiplier = src.CriticalDamageCostExponentialMultiplier,

                ExplosionRadius = src.ExplosionRadius,
                ExplosionRadiusLevel = 0,
                ExplosionRadiusUpgradeAmount = src.ExplosionRadiusUpgradeAmount,
                ExplosionRadiusUpgradeBaseCost = src.ExplosionRadiusUpgradeBaseCost,

                SplashDamage = src.SplashDamage,
                SplashDamageLevel = 0,
                SplashDamageUpgradeAmount = src.SplashDamageUpgradeAmount,
                SplashDamageUpgradeBaseCost = src.SplashDamageUpgradeBaseCost,

                PierceChance = src.PierceChance,
                PierceChanceLevel = 0,
                PierceChanceUpgradeAmount = src.PierceChanceUpgradeAmount,
                PierceChanceUpgradeBaseCost = src.PierceChanceUpgradeBaseCost,

                PierceDamageFalloff = src.PierceDamageFalloff,
                PierceDamageFalloffLevel = 0,
                PierceDamageFalloffUpgradeAmount = src.PierceDamageFalloffUpgradeAmount,
                PierceDamageFalloffUpgradeBaseCost = src.PierceDamageFalloffUpgradeBaseCost,

                PelletCount = src.PelletCount,
                PelletCountLevel = 0,
                PelletCountUpgradeAmount = src.PelletCountUpgradeAmount,
                PelletCountUpgradeBaseCost = src.PelletCountUpgradeBaseCost,

                DamageFalloffOverDistance = src.DamageFalloffOverDistance,
                DamageFalloffOverDistanceLevel = 0,
                DamageFalloffOverDistanceUpgradeAmount = src.DamageFalloffOverDistanceUpgradeAmount,
                DamageFalloffOverDistanceUpgradeBaseCost = src.DamageFalloffOverDistanceUpgradeBaseCost,

                KnockbackStrength = src.KnockbackStrength,
                KnockbackStrengthLevel = 0,
                KnockbackStrengthUpgradeAmount = src.KnockbackStrengthUpgradeAmount,
                KnockbackStrengthUpgradeBaseCost = src.KnockbackStrengthUpgradeBaseCost,
                KnockbackStrengthCostExponentialMultiplier = src.KnockbackStrengthCostExponentialMultiplier,

                PercentBonusDamagePerSec = src.PercentBonusDamagePerSec,
                PercentBonusDamagePerSecLevel = 0,
                PercentBonusDamagePerSecUpgradeAmount = src.PercentBonusDamagePerSecUpgradeAmount,
                PercentBonusDamagePerSecUpgradeBaseCost = src.PercentBonusDamagePerSecUpgradeBaseCost,

                SlowEffect = src.SlowEffect,
                SlowEffectLevel = 0,
                SlowEffectUpgradeAmount = src.SlowEffectUpgradeAmount,
                SlowEffectUpgradeBaseCost = src.SlowEffectUpgradeBaseCost,

                RotationSpeed = src.RotationSpeed,
                AngleThreshold = src.AngleThreshold
            };
        }

        public TurretStatsInstance GetUpgradeableStats(Currency currency)
        {
            return currency == Currency.BlackSteel ? PermanentStats : RuntimeStats;
        }

    }

    public enum EnemyTarget
    {
        First,
        Strongest,
        Random
    }
}