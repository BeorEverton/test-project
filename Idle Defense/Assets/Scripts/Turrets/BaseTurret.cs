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
using UnityEngine;
using Random = UnityEngine.Random;

namespace Assets.Scripts.Turrets
{
    public class BaseTurret : MonoBehaviour
    {
        public EnemyTarget EnemyTargetChoice = EnemyTarget.First;

        /// <summary>
        /// Reference to the TurretInfoSO that contains the base stats and info for this turret. DONT CHANGE AT RUNTIME!
        /// </summary>
        public TurretInfoSO _turretInfo;
        //[NonSerialized]
        public TurretStatsInstance RuntimeStats; // For session upgrades        
        //[NonSerialized]
        public TurretStatsInstance PermanentStats;   // Saved base

        public Transform _rotationPoint, _muzzleFlashPosition;
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
        private ScreenBounds screenBounds;

        // How far from the top the enemy needs to be for the turrets to shoot
        private const float _topSpawnMargin = 1f;

        public GameObject upgradePulseFX;

        private Recoil _recoil;

        // Gunner variables
        [NonSerialized] public int SlotIndex = -1;            // set by SlotWorldButton on spawn
        private TurretStatsInstance _effectiveScratch;        // reused buffer (turret + gunner)                                                              
        private int _effectsSignature = -1; // Tracks which effects are active
        private bool _boundToGunnerEvents = false;

        // Interfaces
        [SerializeField] private MonoBehaviour targetingPatternBehaviour;
        private ITargetingPattern targetingPattern;
        [NonSerialized] public DamageEffectHandler DamageEffects;
        [NonSerialized] public BounceDamageEffect BounceDamageEffectRef;
        [NonSerialized] public PierceDamageEffect PierceDamageEffectRef;
        [NonSerialized] public SplashDamageEffect SplashDamageEffectRef;

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

            targetingPattern = targetingPatternBehaviour as ITargetingPattern;
            InitializeEffects();
        }

        /// <summary>Overwrite the whole stats, preserving level counts, called on load.</summary>
        public void SetPermanentStats(TurretStatsInstance stats)
        {
            PermanentStats = stats;
        }

        protected virtual void OnEnable()
        {
            if (RuntimeStats == null)
            {
                if (PermanentStats != null && PermanentStats.TurretType == _turretInfo.TurretType)
                {
                    RuntimeStats = CloneStatsWithoutLevels(PermanentStats);
                }
                else
                {
                    // PermanentStats is null or wrong turret type → rebuild from SO
                    PermanentStats = new TurretStatsInstance(_turretInfo);
                    RuntimeStats = CloneStatsWithoutLevels(PermanentStats);
                }
            }

            if (ReferenceEquals(PermanentStats, RuntimeStats))
            {
                Debug.LogError($"{name}  PermanentStats and RuntimeStats are sharing the same reference! This will break upgrades.");
            }
        }

        protected virtual void Update()
        {
            // Get click speed bonus and fire-rate LB multiplier from LimitBreakManager
            float clickPct = (LimitBreakManager.Instance != null) ? LimitBreakManager.Instance.ClickSpeedBonusPct : 0f;
            float lbFRMult = (LimitBreakManager.Instance != null) ? LimitBreakManager.Instance.FireRateMultiplier : 1f;

            _bonusSpdMultiplier = 1f + clickPct / 100f;

            // Final fire interval = base interval / (click multiplier * LB multiplier)
            _atkSpeed = (1f / RuntimeStats.FireRate) / (_bonusSpdMultiplier * lbFRMult);

            _timeSinceLastShot += Time.deltaTime;
            Attack();
        }

        protected virtual void Attack()
        {
            /*if (Screen.width != screenBounds.Width || Screen.height != screenBounds.Height)
            {
                // Useful for the grid, here's probably the best place to update the screen bounds
                screenBounds = GridManager.Instance.UpdateScreenBounds();
                //UpdateAttackRange();
            }*/

            if (targetingPattern is TrapPattern trap && trap.randomPlacement)
            {
                if (_timeSinceLastShot >= _atkSpeed)
                    Shoot(); // Fire even without a target

                return; // Skip normal targeting logic
            }

            // if goes out of range
            if (_targetEnemy != null)
            {
                float ty = _targetEnemy.transform.position.Depth();
                if (ty > _effectiveScratch.Range)
                {
                    _targetEnemy.GetComponent<Enemy>().DelayRemoveDeathEvent(Enemy_OnDeath);
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

            if (_targetInAim && _targetInRange)// && IsTargetVisibleOnScreen())
                Shoot();
            else
                TargetEnemy();
        }

        protected virtual void Shoot()
        {
            StartCoroutine(ShowMuzzleFlash());
            _currentShotSound = _shotSounds[Random.Range(0, _shotSounds.Length)];
            AudioManager.Instance.PlayWithVariation(_currentShotSound, 0.8f, 1f);

            if (targetingPattern is PiercingLinePattern plp)
                plp.startPos = _muzzleFlashPosition.position;

            targetingPattern?.ExecuteAttack(this, _effectiveScratch, _targetEnemy);

            // Cache once; also find it if it's on a child
            if (_recoil == null)
                _recoil = GetComponentInChildren<Recoil>(true);

            _recoil?.AddRecoil();

            // Light combat chatter (small chance per shot)
            if (GunnerManager.Instance != null && Random.value < 0.05f)
            {
                string gid = GunnerManager.Instance.GetEquippedGunnerId(SlotIndex);
                var so = GunnerManager.Instance.GetSO(gid);
                if (so != null)
                    GunnerChatterSystem.TryForceCombat(so);
            }

            _timeSinceLastShot = 0f;
        }

        protected virtual void TargetEnemy()
        {
            if (_targetEnemy != null)
                _targetEnemy.GetComponent<Enemy>().OnDeath -= Enemy_OnDeath;

            List<GameObject> enemiesAlive = EnemySpawner.Instance.EnemiesAlive;
            float maxDepth = _effectiveScratch != null ? _effectiveScratch.Range : RuntimeStats.Range;

            _targetEnemy = EnemyTargetChoice switch
            {
                EnemyTarget.First => enemiesAlive
                    .OrderBy(enemy => enemy.transform.position.Depth())
                    .FirstOrDefault(y => y.transform.position.Depth() <= maxDepth),

                EnemyTarget.Strongest => enemiesAlive
                    .OrderByDescending(enemy => enemy.GetComponent<Enemy>().MaxHealth)
                    .FirstOrDefault(y => y.transform.position.Depth() <= maxDepth),

                EnemyTarget.Random => enemiesAlive
                    .OrderBy(_ => Random.value)
                    .FirstOrDefault(y => y.transform.position.Depth() <= maxDepth),

                _ => enemiesAlive
                    .OrderBy(enemy => enemy.transform.position.Depth())
                    .FirstOrDefault(y => y.transform.position.Depth() <= maxDepth)
            };


            if (_targetEnemy != null)
                _targetEnemy.GetComponent<Enemy>().OnDeath += Enemy_OnDeath;
        }

        private bool IsTargetVisibleOnScreen()
        {
            if (!_targetEnemy.activeInHierarchy)
                return false;

            Vector3 pos = _targetEnemy.transform.position;
            return pos.x >= screenBounds.Left && pos.x <= screenBounds.Right &&
                   pos.y >= screenBounds.Bottom && pos.y <= (screenBounds.Top - _topSpawnMargin);
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

            // Work in the parent's local space so the slot's 30° pitch stays intact.
            Transform basis = _rotationPoint.parent != null ? _rotationPoint.parent : _rotationPoint;

            Vector3 toTargetWorld = _targetEnemy.transform.position - _rotationPoint.position;
            Vector3 toTargetLocal = basis.InverseTransformDirection(toTargetWorld);

            // Decide which plane and axis to use based on your configured Depth.
            Vector3 depthWorld = Axes.Forward(1f);
            bool depthIsZ = Mathf.Abs(depthWorld.z) > 0.5f;
            bool depthIsY = !depthIsZ && Mathf.Abs(depthWorld.y) > 0.5f;

            float targetAngle;
            Quaternion targetLocal;

            if (depthIsZ)
            {
                // Depth = Z  -> rotate around local Z, aim in local XY plane
                toTargetLocal.z = 0f;
                if (toTargetLocal.sqrMagnitude < 1e-6f) { _targetInAim = false; return; }

                targetAngle = Mathf.Atan2(toTargetLocal.y, toTargetLocal.x) * Mathf.Rad2Deg - 90f;
                targetLocal = Quaternion.Euler(0f, 0f, targetAngle);
            }
            else if (depthIsY)
            {
                // Depth = Y -> rotate around local Y, aim in local XZ plane
                toTargetLocal.y = 0f;
                if (toTargetLocal.sqrMagnitude < 1e-6f) { _targetInAim = false; return; }

                // Standard yaw: +Z forward, +X right
                targetAngle = Mathf.Atan2(toTargetLocal.x, toTargetLocal.z) * Mathf.Rad2Deg;
                targetLocal = Quaternion.Euler(0f, targetAngle, 0f);
            }
            else
            {
                // Fallback: treat as Z-depth.
                toTargetLocal.z = 0f;
                if (toTargetLocal.sqrMagnitude < 1e-6f) { _targetInAim = false; return; }
                targetAngle = Mathf.Atan2(toTargetLocal.y, toTargetLocal.x) * Mathf.Rad2Deg - 90f;
                targetLocal = Quaternion.Euler(0f, 0f, targetAngle);
            }

            // Slerp strictly in LOCAL space; no world/local mixing.
            _rotationPoint.localRotation = Quaternion.Slerp(
                _rotationPoint.localRotation,
                targetLocal,
                RuntimeStats.RotationSpeed * bonusMultiplier * Time.deltaTime
            );

            IsAimingOnTarget(targetLocal);
        }

        protected virtual void IsAimingOnTarget(Quaternion targetLocal)
        {
            if (_targetEnemy == null)
            {
                _targetInAim = false;
                return;
            }

            // Compare local rotations directly.
            float diff = Quaternion.Angle(_rotationPoint.localRotation, targetLocal);
            _targetInAim = diff <= RuntimeStats.AngleThreshold;
        }

        private IEnumerator ShowMuzzleFlash()
        {
            if (_muzzleFlashSprites.Count == 0)
                yield break;

            Sprite randomMuzzleFlash = _muzzleFlashSprites[Random.Range(0, _muzzleFlashSprites.Count)];

            SpriteRenderer spriteRenderer = _muzzleFlashPosition.GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
                yield break;
            else spriteRenderer.sprite = randomMuzzleFlash;

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

        /*
        private void UpdateAttackRange()
        {
            _attackRange = screenBounds.Top - _topSpawnMargin;
        }*/

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
                // Identity / base
                IsUnlocked = src.IsUnlocked,
                TurretType = src.TurretType,

                // Base (do not touch at runtime)
                BaseDamage = src.BaseDamage,
                BaseFireRate = src.BaseFireRate,
                BaseCritChance = src.BaseCritChance,
                BaseCritDamage = src.BaseCritDamage,

                // Core combat
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

                // Aiming / reach
                RotationSpeed = src.RotationSpeed,
                RotationSpeedLevel = 0,
                RotationSpeedUpgradeAmount = src.RotationSpeedUpgradeAmount,
                RotationSpeedUpgradeBaseCost = src.RotationSpeedUpgradeBaseCost,
                RotationSpeedCostExponentialMultiplier = src.RotationSpeedCostExponentialMultiplier,
                AngleThreshold = src.AngleThreshold,

                Range = src.Range,
                RangeLevel = 0,
                RangeUpgradeAmount = src.RangeUpgradeAmount,
                RangeUpgradeBaseCost = src.RangeUpgradeBaseCost,
                RangeCostExponentialMultiplier = src.RangeCostExponentialMultiplier,


                // Crits
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

                // AOE / splash
                ExplosionRadius = src.ExplosionRadius,
                ExplosionRadiusLevel = 0,
                ExplosionRadiusUpgradeAmount = src.ExplosionRadiusUpgradeAmount,
                ExplosionRadiusUpgradeBaseCost = src.ExplosionRadiusUpgradeBaseCost,

                SplashDamage = src.SplashDamage,
                SplashDamageLevel = 0,
                SplashDamageUpgradeAmount = src.SplashDamageUpgradeAmount,
                SplashDamageUpgradeBaseCost = src.SplashDamageUpgradeBaseCost,

                // Pierce / sniper
                PierceChance = src.PierceChance,
                PierceChanceLevel = 0,
                PierceChanceUpgradeAmount = src.PierceChanceUpgradeAmount,
                PierceChanceUpgradeBaseCost = src.PierceChanceUpgradeBaseCost,

                PierceDamageFalloff = src.PierceDamageFalloff,
                PierceDamageFalloffLevel = 0,
                PierceDamageFalloffUpgradeAmount = src.PierceDamageFalloffUpgradeAmount,
                PierceDamageFalloffUpgradeBaseCost = src.PierceDamageFalloffUpgradeBaseCost,

                // Shotgun / multi
                PelletCount = src.PelletCount,
                PelletCountLevel = 0,
                PelletCountUpgradeAmount = src.PelletCountUpgradeAmount,
                PelletCountUpgradeBaseCost = src.PelletCountUpgradeBaseCost,

                // Distance falloff
                DamageFalloffOverDistance = src.DamageFalloffOverDistance,
                DamageFalloffOverDistanceLevel = 0,
                DamageFalloffOverDistanceUpgradeAmount = src.DamageFalloffOverDistanceUpgradeAmount,
                DamageFalloffOverDistanceUpgradeBaseCost = src.DamageFalloffOverDistanceUpgradeBaseCost,

                // Utility effects
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

                // Bounce pattern
                BounceCount = src.BounceCount,
                BounceCountLevel = 0,
                BounceCountUpgradeAmount = src.BounceCountUpgradeAmount,
                BounceCountUpgradeBaseCost = src.BounceCountUpgradeBaseCost,
                BounceCountCostExponentialMultiplier = src.BounceCountCostExponentialMultiplier,

                BounceRange = src.BounceRange,
                BounceRangeLevel = 0,
                BounceRangeUpgradeAmount = src.BounceRangeUpgradeAmount,
                BounceRangeUpgradeBaseCost = src.BounceRangeUpgradeBaseCost,
                BounceRangeCostExponentialMultiplier = src.BounceRangeCostExponentialMultiplier,

                BounceDelay = src.BounceDelay,
                BounceDelayLevel = 0,
                BounceDelayUpgradeAmount = src.BounceDelayUpgradeAmount,
                BounceDelayUpgradeBaseCost = src.BounceDelayUpgradeBaseCost,
                BounceDelayCostExponentialMultiplier = src.BounceDelayCostExponentialMultiplier,

                BounceDamagePct = src.BounceDamagePct,
                BounceDamagePctLevel = 0,
                BounceDamagePctUpgradeAmount = src.BounceDamagePctUpgradeAmount,
                BounceDamagePctUpgradeBaseCost = src.BounceDamagePctUpgradeBaseCost,
                BounceDamagePctCostExponentialMultiplier = src.BounceDamagePctCostExponentialMultiplier,

                // Cone pattern
                ConeAngle = src.ConeAngle,
                ConeAngleLevel = 0,
                ConeAngleUpgradeAmount = src.ConeAngleUpgradeAmount,
                ConeAngleUpgradeBaseCost = src.ConeAngleUpgradeBaseCost,
                ConeAngleCostExponentialMultiplier = src.ConeAngleCostExponentialMultiplier,

                // Trap pattern
                AheadDistance = src.AheadDistance,
                AheadDistanceLevel = 0,
                AheadDistanceUpgradeAmount = src.AheadDistanceUpgradeAmount,
                AheadDistanceUpgradeBaseCost = src.AheadDistanceUpgradeBaseCost,
                AheadDistanceCostExponentialMultiplier = src.AheadDistanceCostExponentialMultiplier,

                TrapPrefab = src.TrapPrefab,

                MaxTrapsActive = src.MaxTrapsActive,
                MaxTrapsActiveLevel = 0,
                MaxTrapsActiveUpgradeAmount = src.MaxTrapsActiveUpgradeAmount,
                MaxTrapsActiveUpgradeBaseCost = src.MaxTrapsActiveUpgradeBaseCost,
                MaxTrapsActiveCostExponentialMultiplier = src.MaxTrapsActiveCostExponentialMultiplier,

                // Delayed AOE
                ExplosionDelay = src.ExplosionDelay,
                ExplosionDelayLevel = 0,
                ExplosionDelayUpgradeAmount = src.ExplosionDelayUpgradeAmount,
                ExplosionDelayUpgradeBaseCost = src.ExplosionDelayUpgradeBaseCost,
                ExplosionDelayCostExponentialMultiplier = src.ExplosionDelayCostExponentialMultiplier,


                // flight and armor 
                CanHitFlying = src.CanHitFlying,
                ArmorPenetration = src.ArmorPenetration,
                ArmorPenetrationLevel = 0,
                ArmorPenetrationUpgradeAmount = src.ArmorPenetrationUpgradeAmount,
                ArmorPenetrationUpgradeBaseCost = src.ArmorPenetrationUpgradeBaseCost,
                ArmorPenetrationCostExponentialMultiplier = src.ArmorPenetrationCostExponentialMultiplier,

            };
        }

        public TurretStatsInstance GetUpgradeableStats(Currency currency)
        {
            return currency == Currency.BlackSteel ? PermanentStats : RuntimeStats;
        }

        private void InitializeEffects()
        {
            // Build once from current RuntimeStats; next frames will compare and rebuild from _effectiveScratch if needed.
            RebuildEffectsFromStats(RuntimeStats);
        }

        private void RebuildEffectsFromStats(TurretStatsInstance stats)
        {
            DamageEffects = new DamageEffectHandler();

            // Baseline
            if (stats.Damage > 0 || stats.DamageUpgradeAmount > 0)
                DamageEffects.AddEffect(new FlatDamageEffect());

            // Crit
            if (stats.CriticalChance > 0 || stats.CriticalChanceUpgradeAmount > 0)
                DamageEffects.AddEffect(new CriticalHitEffect());

            // Knockback
            if (stats.KnockbackStrength > 0 || stats.KnockbackStrengthUpgradeAmount > 0)
                DamageEffects.AddEffect(new KnockbackEffect());

            // Bounce chain damage
            if (stats.BounceCount > 0 || stats.BounceCountUpgradeAmount > 0)
            {
                var bounceEffect = new BounceDamageEffect(stats.BounceDamagePct);
                BounceDamageEffectRef = bounceEffect;
                DamageEffects.AddEffect(bounceEffect);
            }

            // Damage ramp (DoT-like ramp over time on target)
            if (stats.PercentBonusDamagePerSec > 0 || stats.PercentBonusDamagePerSecUpgradeAmount > 0)
                DamageEffects.AddEffect(new RampDamageOverTimeEffect());

            // Slow
            if (stats.SlowEffect > 0 || stats.SlowEffectUpgradeAmount > 0)
                DamageEffects.AddEffect(new SlowEffect());

            // Pierce
            if (stats.PierceChance > 0 || stats.PierceChanceUpgradeAmount > 0)
            {
                var pierceEffect = new PierceDamageEffect(stats.PierceDamageFalloff);
                DamageEffects.AddEffect(pierceEffect);
                PierceDamageEffectRef = pierceEffect;
            }

            // Splash (secondary targets)
            if (stats.SplashDamage > 0 || stats.SplashDamageUpgradeAmount > 0)
                SplashDamageEffectRef = new SplashDamageEffect();

            // Always-on final multiplier from LimitBreakManager (LB damage + click damage %)
            DamageEffects.AddEffect(new LimitBreakDamageEffect());

            _effectsSignature = ComputeEffectsSignature(stats);
        }

        private int ComputeEffectsSignature(TurretStatsInstance s)
        {
            int sig = 0;
            if (s.Damage > 0 || s.DamageUpgradeAmount > 0) sig |= 1 << 0;
            if (s.CriticalChance > 0 || s.CriticalChanceUpgradeAmount > 0) sig |= 1 << 1;
            if (s.KnockbackStrength > 0 || s.KnockbackStrengthUpgradeAmount > 0) sig |= 1 << 2;
            if (s.BounceCount > 0 || s.BounceCountUpgradeAmount > 0) sig |= 1 << 3;
            if (s.PercentBonusDamagePerSec > 0 || s.PercentBonusDamagePerSecUpgradeAmount > 0) sig |= 1 << 4;
            if (s.SlowEffect > 0 || s.SlowEffectUpgradeAmount > 0) sig |= 1 << 5;
            if (s.PierceChance > 0 || s.PierceChanceUpgradeAmount > 0) sig |= 1 << 6;
            if (s.SplashDamage > 0 || s.SplashDamageUpgradeAmount > 0) sig |= 1 << 7;
            return sig;
        }

        #region GUNNER BINDING
        public void BindToGunnerManager(int slotIndex)
        {
            SlotIndex = slotIndex;
            if (!_boundToGunnerEvents && GunnerManager.Instance != null)
            {
                GunnerManager.Instance.OnSlotGunnerChanged += HandleGunnerSlotChanged;
                GunnerManager.Instance.OnSlotGunnerStatsChanged += HandleGunnerSlotChanged;
                _boundToGunnerEvents = true;
            }
            RecomputeEffectiveFromGunner(); // initial build
        }

        private void OnDisable()
        {
            if (_boundToGunnerEvents && GunnerManager.Instance != null)
            {
                GunnerManager.Instance.OnSlotGunnerChanged -= HandleGunnerSlotChanged;
                GunnerManager.Instance.OnSlotGunnerStatsChanged -= HandleGunnerSlotChanged;
                _boundToGunnerEvents = false;
            }
        }

        // Event callback
        private void HandleGunnerSlotChanged(int changedSlot)
        {
            if (changedSlot != SlotIndex) return;
            RecomputeEffectiveFromGunner();
        }

        // Made public to call from upgrade manager in case gunner is dead
        public void RecomputeEffectiveFromGunner()
        {
            if (_effectiveScratch == null) _effectiveScratch = new TurretStatsInstance();
            // Merge turret + current gunner into scratch
            GunnerManager.Instance.ApplyTo(RuntimeStats, SlotIndex, _effectiveScratch);

            // Prestige global bonuses (damage, fire rate, crits, pierce, etc.)
            if (PrestigeManager.Instance != null)
                PrestigeManager.Instance.ApplyToTurretStats(_effectiveScratch);

            // Rebuild damage effects from the effective stats (adds/removes Slow/Knockback/etc.)
            RebuildEffectsFromStats(_effectiveScratch);
        }

        #endregion

    }

    public enum EnemyTarget
    {
        First,
        Strongest,
        Random
    }
}