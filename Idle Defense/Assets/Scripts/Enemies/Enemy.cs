using Assets.Scripts.SO;
using Assets.Scripts.Systems;
using Assets.Scripts.Systems.Audio;
using Assets.Scripts.UI;
using Assets.Scripts.WaveSystem;
using DamageNumbersPro;
using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;
using Random = UnityEngine.Random;

namespace Assets.Scripts.Enemies
{
    public class Enemy : MonoBehaviour
    {
        public event EventHandler OnMaxHealthChanged;
        public event EventHandler OnCurrentHealthChanged;
        public event EventHandler<OnDeathEventArgs> OnDeath;
        public class OnDeathEventArgs : EventArgs
        {
            public ulong CoinDropAmount;
            public float XPDropAmount;
        }

        [SerializeField] private EnemyInfoSO _info;
        private bool _applyBossVisualsAfterReset = false;
        private bool _isMiniBoss = false;
        private Vector3? _originalScale;
        private Color? _originalColor;

        public Vector2 KnockbackVelocity;
        public float KnockbackTime;
        public EnemyInfoSO Info
        {
            get => _info;
            set => _info = value;
        }

        public float MaxHealth { get; private set; }
        public float CurrentHealth { get; private set; }
        public bool IsSlowed { get; private set; }
        public bool IsBossInstance;
        public EnemyDeathEffect EnemyDeathEffect { get; private set; }

        public bool IsAlive;
        public bool CanAttack;
        public float TimeSinceLastAttack = 0f;
        public float MovementSpeed;
        public Vector2Int LastGridPos;

        public float attackRange;

        [SerializeField] private DamageNumber damageNumber, damageNumberCritical;
        [SerializeField] private Transform _body;

        [SerializeField] private HitFlashOverlay hitFlash;

        // Laser targeting
        private float _baseMovementSpeed;

        // For Damage effects
        public bool tookCriticalHit = false;

        // Runtime specials
        private int shieldChargesRT = 0;
        private bool suppressRewardsOnDeath = false;   // used by suicide explosion
        private bool didKamize = false;              // prevent double-trigger

        // Timer for healer
        private float healerTimer = 0f;

        // Timers for summoner
        private float summonerTimer = 0f;
        private bool hasSummonedOnce = false;

        private bool _deathSequenceStarted = false; // Ensures all death stuff is set

#if UNITY_EDITOR //To display coinDrop and damage in the inspector debug mode
        private ulong _coinDropAmount;
        private float _damage;
#endif

        // For visuals
        private Vector3 _bodyOriginalLocalPos;
        [SerializeField] private Transform _muzzleFlashPoint;
        [SerializeField] private SpriteRenderer _muzzleFlashRenderer;
        [SerializeField] private List<Sprite> _muzzleFlashSprites;

        // Movement
        public Vector3 MoveDirection;
        [SerializeField] private Animator _animator;
        [SerializeField] private string _animMovingBool = "Idle";
        [SerializeField] private string _animAttackTrigger = "Attack";
        [SerializeField] private string _animSkillTrigger = "Skill";

        private int _animMovingHash;
        private int _animAttackHash;
        private int _animSkillHash;

        // Boss unique stuff
        public enum BossDeathExplosionMode { AlliesOnly, GunnersOnly, Both }

        private BossDeathExplosionMode _bossDeathExplosionMode = BossDeathExplosionMode.AlliesOnly;
        public void SetBossDeathExplosionMode(BossDeathExplosionMode mode) => _bossDeathExplosionMode = mode;

        public float MovementLockTimer { get; private set; }
        public bool IsMovementLocked => MovementLockTimer > 0f;

        // Runtime multipliers 
        private float _speedMultRT = 1f;
        private float _damageMultRT = 1f;
        private float _armorDeltaRT = 0f;
        private float _dodgeDeltaRT = 0f;

        private float _buffTimer = 0f;

        // Expose read-only access for manager
        public float SpeedMultRT => _speedMultRT;
        public float DamageMultRT => _damageMultRT;
        public float ArmorDeltaRT => _armorDeltaRT;
        public float DodgeDeltaRT => _dodgeDeltaRT;

        public void ApplyTimedBuff(
            float speedMult,
            float damageMult,
            float armorDelta,
            float dodgeDelta,
            float durationSeconds)
        {
            _speedMultRT = (speedMult <= 0f) ? 1f : speedMult;
            _damageMultRT = (damageMult <= 0f) ? 1f : damageMult;
            _armorDeltaRT = armorDelta;
            _dodgeDeltaRT = dodgeDelta;
            _buffTimer = Mathf.Max(0.01f, durationSeconds);
        }

        public void TickRuntimeBuffs(float dt)
        {
            if (_buffTimer <= 0f) return;

            _buffTimer -= dt;
            if (_buffTimer <= 0f)
            {
                _speedMultRT = 1f;
                _damageMultRT = 1f;
                _armorDeltaRT = 0f;
                _dodgeDeltaRT = 0f;
            }
        }


        private void Awake()
        {
            if (_body != null)
                _bodyOriginalLocalPos = _body.localPosition;

            if (hitFlash == null)
                hitFlash = GetComponentInChildren<HitFlashOverlay>();

            EnemyDeathEffect = GetComponent<EnemyDeathEffect>();

            if (_animator == null)
                _animator = GetComponentInChildren<Animator>(true);

            _animMovingHash = Animator.StringToHash(_animMovingBool);
            _animAttackHash = Animator.StringToHash(_animAttackTrigger);
            _animSkillHash = Animator.StringToHash(_animSkillTrigger);
        }

        public void SetAnimIdle(bool isIdle)
        {
            if (_animator == null) return;
            _animator.SetBool(_animMovingHash, isIdle);
        }

        public void TriggerAnimAttack()
        {
            if (_animator == null) return;
            _animator.ResetTrigger(_animAttackHash);
            _animator.SetTrigger(_animAttackHash);
        }

        public void TriggerAnimSkill()
        {
            if (_animator == null) return;
            _animator.ResetTrigger(_animSkillHash);
            _animator.SetTrigger(_animSkillHash);
        }


        private void OnEnable()
        {
            ResetEnemy();
            LastGridPos = GridManager.Instance.GetGridPosition(transform.position);
            GridManager.Instance.AddEnemy(this);

            // Prewarm the summoner's pool if configured
            if (_info.SummonerEnabled && _info.SummonPrefab != null && _info.SummonPrewarmCount > 0)
            {
                EnemySpawner.Instance.PrewarmPrefab(_info.SummonPrefab, _info.SummonPrewarmCount);
            }

            // Initialize summon timer
            summonerTimer = 0f;
            hasSummonedOnce = false;

#if UNITY_EDITOR
            _coinDropAmount = _info.CoinDropAmount;
            _damage = _info.Damage;
#endif
        }

        private void OnDisable()
        {
            IsBossInstance = false;
            if (EnemyManager.Instance != null)
            {
                EnemyManager.Instance.ClearPendingSkill(this);
                EnemyManager.Instance.ClearPendingAttack(this);
            }


            GridManager.Instance.RemoveEnemy(this, LastGridPos);
        }

        public void TakeDamage(float amount, float armorPenetrationPct = 0, bool isAoe = false, bool isCritical = false)
        {
            if (!IsAlive) return; // ignore stray ticks after death

            // 0) Shield gate (blocks entire *instance* before any dodge/armor)
            if (shieldChargesRT > 0)
            {
                shieldChargesRT--;
                if (SettingsManager.Instance.AllowPopups && damageNumber)
                    damageNumber.Spawn(transform.position, "Block");
                return;
            }

            // 1) Optional Dodge (skip if AOE)
            if (!isAoe && _info != null && _info.DodgeChance + _dodgeDeltaRT > 0f)
            {
                if (Random.value < Mathf.Clamp01(_info.DodgeChance + _dodgeDeltaRT))
                {
                    damageNumber.Spawn(transform.position, "Miss");
                    return; // fully avoided
                }
            }

            // 2) Armor with penetration
            float armor = (_info != null) ? Mathf.Clamp01(_info.Armor + _armorDeltaRT) : 0f;   // 0–0.9 in SO, clamp to [0..1)
            float ap = Mathf.Clamp01(armorPenetrationPct / 100f);           // 0..1
            float effectiveArmor = Mathf.Max(0f, armor * (1f - ap));           // after AP
            float finalDamage = amount * (1f - effectiveArmor);

            // 3) Apply
            CurrentHealth -= finalDamage;

            if (hitFlash != null)
                hitFlash.TriggerFlash();

            isCritical = tookCriticalHit;
            if (SettingsManager.Instance.AllowPopups)
            {
                if (finalDamage >= 0.05f)
                {
                    string txt = finalDamage < 1f
                        ? finalDamage.ToString("0.0")
                        : UIManager.AbbreviateNumber(finalDamage, false, true);

                    if (damageNumberCritical && isCritical)
                        damageNumberCritical.Spawn(transform.position, txt);
                    else if (damageNumber)
                        damageNumber.Spawn(transform.position, txt);
                }
            }

            OnCurrentHealthChanged?.Invoke(this, EventArgs.Empty);
            StatsManager.Instance.TotalDamage += finalDamage;

            tookCriticalHit = false;
            CheckIfDead();
        }

        private void CheckIfDead()
        {
            // Only proceed when health is actually zero or below
            if (CurrentHealth > 0f) return;
            if (_deathSequenceStarted) return; // already dead

            _deathSequenceStarted = true;

            IsAlive = false;

            if (EnemyManager.Instance != null)
            {
                EnemyManager.Instance.ClearPendingSkill(this);
                EnemyManager.Instance.ClearPendingAttack(this);
            }


            // If this enemy explodes on death, run that first; then finish death.
            if (_info.ExploderEnabled)
            {
                StartCoroutine(DeathExploderRoutine());
                return; // death flow will continue inside the routine
            }

            FinishDeathAndRewards();
        }

        private IEnumerator DeathExploderRoutine()
        {
            if (_info.ExploderDelay > 0f)
                yield return new WaitForSeconds(_info.ExploderDelay);

            float dmg = _info.Damage;
            int maxHit = Mathf.Clamp(_info.ExploderMaxGunners, 1, 5);
            float radius = Mathf.Max(0f, _info.ExploderRadius);

            HitNearestGunners(dmg, radius, maxHit);

            // Now complete the normal death (including possible boss FX and rewards)
            FinishDeathAndRewards();
        }

        private void FinishDeathAndRewards()
        {
            // Stat: kill count
            StatsManager.Instance.EnemiesKilled++;

            // Boss special
            if (IsBossInstance)
            {
                TriggerBossExplosionMode();

                StatsManager.Instance.BossesKilled++;

                if (!suppressRewardsOnDeath)
                {
                    ulong baseAmount = (ulong)Mathf.RoundToInt(_info.CoinDropAmount / 2f);
                    float mult = PrestigeManager.Instance != null ? PrestigeManager.Instance.GetBlackSteelGainMultiplier() : 1f;
                    ulong boosted = baseAmount == 0 ? 0 : (ulong)Mathf.Max(1, Mathf.RoundToInt(baseAmount * mult));
                    GameManager.Instance.AddCurrency(Currency.BlackSteel, boosted);
                    damageNumber.Spawn(transform.position, UIManager.ICON_BLACKSTEEL + UIManager.AbbreviateNumber(boosted, false, false));
                }
            }
            else
            {
                // 1/1000 chance: bonus black steel (skip if suppressed)
                if (!suppressRewardsOnDeath && Random.Range(0, 1000) == 0)
                {
                    ulong baseAmount = (ulong)Mathf.RoundToInt(_info.CoinDropAmount / 5f);
                    float mult = PrestigeManager.Instance != null ? PrestigeManager.Instance.GetBlackSteelGainMultiplier() : 1f;
                    ulong boosted = baseAmount == 0 ? 0 : (ulong)Mathf.Max(1, Mathf.RoundToInt(baseAmount * mult));
                    GameManager.Instance.AddCurrency(Currency.BlackSteel, boosted);
                    damageNumber.Spawn(transform.position, UIManager.ICON_BLACKSTEEL + UIManager.AbbreviateNumber(boosted, false, false));
                }
            }

            DebrisPool.Instance.Play(transform.position);

            // Fire death only if not suppressed (suppression used by suicide)
            if (!suppressRewardsOnDeath)
            {
                OnDeath?.Invoke(this, new OnDeathEventArgs
                {
                    CoinDropAmount = _info.CoinDropAmount,
                    XPDropAmount = CalculateXpFrom(_info)
                });
            }
            else
            {
                // no rewards, no XP
                OnDeath?.Invoke(this, new OnDeathEventArgs { CoinDropAmount = 0, XPDropAmount = 0f });
            }
        }

        private float CalculateXpFrom(EnemyInfoSO info)
        {
            float xp = 1f
                     + info.MaxHealth * 0.1f
                     + info.MovementSpeed
                     + info.Damage * 0.5f
                     + info.AttackRange
                     + info.AttackSpeed;

            if (IsBossInstance)
                xp *= 2f;

            return xp;
        }

        private void TriggerBossExplosionMode()
        {
            float radius = _isMiniBoss ? 3f : 5f;
            float explosionDamage = MaxHealth * 0.2f;

            if (_bossDeathExplosionMode == BossDeathExplosionMode.AlliesOnly || _bossDeathExplosionMode == BossDeathExplosionMode.Both)
                DamageNearbyAllies(radius, explosionDamage);

            if (_bossDeathExplosionMode == BossDeathExplosionMode.GunnersOnly || _bossDeathExplosionMode == BossDeathExplosionMode.Both)
                HitNearestGunners(explosionDamage, radius, 3); // or scale by boss type

            AudioManager.Instance.Play("Rocket Impact");
        }

        private void DamageNearbyAllies(float radius, float damage)
        {
            var nearby = GridManager.Instance.GetEnemiesInRange(transform.position, Mathf.CeilToInt(radius));
            for (int i = 0; i < nearby.Count; i++)
            {
                Enemy e = nearby[i];
                if (e == null || e == this || !e.IsAlive) continue;
                if (Vector3.Distance(e.transform.position, transform.position) <= radius)
                    e.TakeDamage(damage);
            }
        }

        private void ResetEnemy()
        {
            // Reset death state
            _deathSequenceStarted = false;

            // Reset visual state if modified
            if (_originalScale.HasValue)
            {
                _body.localScale = _originalScale.Value;
            }

            if (_originalColor.HasValue && TryGetBodySpriteRenderer(out var sr))
                sr.color = _originalColor.Value;

            _originalScale = null;
            _originalColor = null;

            // Only reset info if not overridden
            if (!IsBossInstance && WaveManager.Instance.GetCurrentWave().WaveEnemies.ContainsKey(Info.EnemyId))
            {
                Info = WaveManager.Instance.GetCurrentWave().WaveEnemies[Info.EnemyId];
            }

            // reset animation
            if (EnemyManager.Instance != null)
            {
                EnemyManager.Instance.ClearPendingSkill(this);
                EnemyManager.Instance.ClearPendingAttack(this);
            }


            ApplyBodySpriteFromInfo();
            ResetVisualPosition();

            CanAttack = false;
            SetAnimIdle(false);
            IsAlive = true;
            MaxHealth = Info.MaxHealth;
            CurrentHealth = MaxHealth;
            OnMaxHealthChanged?.Invoke(this, EventArgs.Empty);
            SetRandomMovementSpeed();
            SetRandomAttackRange();
            _muzzleFlashRenderer.enabled = false;

            // Apply boss visuals only after reset
            if (_applyBossVisualsAfterReset)
            {
                _applyBossVisualsAfterReset = false;
                SetAsBoss(_isMiniBoss);
            }

            KnockbackTime = 0f;
            KnockbackVelocity = Vector2.zero;

            // reset runtime
            shieldChargesRT = Mathf.Max(0, _info.ShieldCharges);
            suppressRewardsOnDeath = false;
            didKamize = false;
            healerTimer = 0f;

            _speedMultRT = 1f;
            _damageMultRT = 1f;
            _armorDeltaRT = 0f;
            _dodgeDeltaRT = 0f;
            _buffTimer = 0f;
        }

        public void SetAsBoss(bool isMini)
        {
            if (_body == null)
            {
                Debug.LogWarning("Body not found on " + name);
                return;
            }

            // Store original state
            _originalScale ??= _body.localScale;

            if (TryGetBodySpriteRenderer(out SpriteRenderer sr) && _originalColor == null)
                _originalColor = sr.color;

            // Apply boss scaling + color
            if (isMini)
            {
                _body.localScale = _originalScale.Value * 2f;
                if (sr) sr.color = Color.red; 
            }
            else
            {
                _body.localScale = _originalScale.Value * 4f;
                if (sr) sr.color = Color.black;
            }
            _applyBossVisualsAfterReset = false;
        }

        public void ApplyBossInfo(EnemyInfoSO clone, bool isMini)
        {
            IsBossInstance = true;
            Info = clone;

            _applyBossVisualsAfterReset = true;
            _isMiniBoss = isMini;
        }

        public void ApplyBodySpriteFromInfo()
        {
            if (_body == null)
            {
                Debug.LogWarning("Body transform is not assigned on: " + gameObject.name);
                return;
            }

            if (_info == null || _info.Icon == null)
            {
                Debug.LogWarning("EnemyInfo or Icon is null for: " + gameObject.name);
                return;
            }

            SpriteRenderer sr = _body.GetComponent<SpriteRenderer>();
            if (sr == null)
            {
                Debug.LogWarning("No SpriteRenderer found on _body for: " + gameObject.name);
                return;
            }

            sr.sprite = _info.Icon;
        }

        private bool TryGetBodySpriteRenderer(out SpriteRenderer sr) => _body != null ? sr = _body.GetComponent<SpriteRenderer>() : sr = null;

        private void SetRandomMovementSpeed()
        {
            _baseMovementSpeed = Random.Range(
                _info.MovementSpeed - _info.MovementSpeedDifference,
                _info.MovementSpeed + _info.MovementSpeedDifference
            );

            MovementSpeed = _baseMovementSpeed;
        }

        private void SetRandomAttackRange()
        {
            attackRange = Random.Range(
                _info.AttackRange - _info.AttackRangeDifference,
                _info.AttackRange + _info.AttackRange
            );

            MovementSpeed = _baseMovementSpeed;
        }

        public void ReduceMovementSpeed(float percent)
        {
            // Don't reduce speed again if already slowed with equal or stronger effect
            float newSpeed = _baseMovementSpeed * (1f - percent / 100f);
            if (IsSlowed && newSpeed >= MovementSpeed)
                return;

            MovementSpeed = newSpeed;
            IsSlowed = true;
        }

        public void AnimateAttack()
        {
            if (_animator != null)
            {
                TriggerAnimAttack();

                // muzzle flash for ranged enemies 
                if (_info != null && _info.AttackRange >= 0.5f)
                    StartCoroutine(ShowMuzzleFlash());

                return;
            }
            /* Old 2D stuff
            if (_body == null) return;

            _body.DOKill(); // stop any ongoing animation

            if (_info.AttackRange < 0.5f) // Melee enemy
            {
                Vector3 start = _bodyOriginalLocalPos;
                Vector3 windUp = start + Vector3.up * 0.2f; // anticipate
                Vector3 impact = start + Vector3.down * 0.4f; // attack slam

                Sequence seq = DOTween.Sequence();
                seq.Append(_body.DOLocalMove(windUp, 0.08f).SetEase(Ease.InOutSine))
                   .Append(_body.DOLocalMove(impact, 0.06f).SetEase(Ease.InQuad))
                   .Append(_body.DOLocalMove(start, 0.1f).SetEase(Ease.OutBack));
            }

            else
            {
                // Ranged recoil animation
                Vector3 recoil = _bodyOriginalLocalPos + new Vector3(0, 0.1f, 0); // pushed up
                _body.DOLocalMove(recoil, 0.1f).SetEase(Ease.OutQuad)
                      .OnComplete(() =>
                      {
                          _body.DOLocalMove(_bodyOriginalLocalPos, 0.25f).SetEase(Ease.OutExpo);
                      });
                StartCoroutine(ShowMuzzleFlash());
            }*/
        }

        private IEnumerator ShowMuzzleFlash()
        {
            if (_muzzleFlashSprites == null || _muzzleFlashSprites.Count == 0)
                yield break;

            Sprite flash = _muzzleFlashSprites[Random.Range(0, _muzzleFlashSprites.Count)];
            _muzzleFlashRenderer.sprite = flash;
            _muzzleFlashRenderer.enabled = true;

            yield return new WaitForSeconds(0.03f); // Very short flash

            _muzzleFlashRenderer.enabled = false;
        }

        public void ResetVisualPosition()
        {
            if (_body == null) return;

            _body.DOKill(); // cancel any tweens immediately
            _body.localPosition = _bodyOriginalLocalPos; // snap back to original position
        }

        /// <summary>
        /// Direct calling it may remove and then kill the enemy by a different turret
        /// </summary>
        /// <param name="evt"></param>
        public void DelayRemoveDeathEvent(EventHandler<OnDeathEventArgs> evt)
        {
            // Delay the removal of the death event to ensure it doesn't interfere with ongoing death effects
            StartCoroutine(DelayRemoveDeathEventCoroutine(evt));
        }

        private IEnumerator DelayRemoveDeathEventCoroutine(EventHandler<OnDeathEventArgs> evt)
        {
            if (!IsAlive || OnDeath == null)
            {
                yield break; // No need to delay if already dead or no subscribers
            }
            yield return new WaitForSeconds(0.2f); // Adjust delay as needed
            OnDeath -= evt;
        }

        private void HitNearestGunners(float damage, float radius, int maxTargets)
        {
            Debug.Log($"Enemy Explosion: Damage={damage}, Radius={radius}, MaxTargets={maxTargets}");
            // Build (slot, distance) list for alive gunners within radius
            List<(int slot, float dist)> candidates = new List<(int, float)>(5);
            for (int slot = 0; slot < 5; slot++)
            {
                float z = GunnerManager.Instance.GetSlotAnchorDepth(slot);
                float d = Mathf.Abs(z - transform.position.Depth());

                bool alive = GunnerManager.Instance.IsSlotAlive(slot);

                Debug.Log("Slot depth is " + z);
                if (d <= radius && alive)
                    candidates.Add((slot, d));
            }

            // sort by distance and hit up to N
            candidates.Sort((a, b) => a.dist.CompareTo(b.dist));
            int hits = Mathf.Min(maxTargets, candidates.Count);
            for (int i = 0; i < hits; i++)
            {
                GunnerManager.Instance.ApplyDamageOnSlot(candidates[i].slot, damage);
            }
        }

        // Specials
        public void TriggerKamikazeExplosion()
        {
            if (didKamize) return; // guard
            didKamize = true;
            suppressRewardsOnDeath = true;

            float dmg = Mathf.Max(0f, _info.Damage) * MaxHealth;
            int maxHit = Mathf.Clamp(_info.KamikazeMaxGunners, 1, 5);
            float radius = Mathf.Max(0f, _info.KamikazeRadius);

            HitNearestGunners(dmg, radius, maxHit);

            // Kill self without rewards/XP
            CurrentHealth = 0f;
            CheckIfDead();
        }

        public void Heal(float amount)
        {
            if (amount <= 0f || !IsAlive) return;
            CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + amount);
            OnCurrentHealthChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Returns true exactly when this healer should fire (cooldown elapsed). No allocations.
        /// </summary>
        public bool HealerReady(float dt)
        {
            if (!_info.HealerEnabled || !IsAlive) return false;
            healerTimer += dt;
            float cd = Mathf.Max(0.01f, _info.HealerCooldown);
            if (healerTimer < cd) return false;
            healerTimer = 0f;
            return true;
        }

        /// <summary>
        /// Returns true when a summon should fire. If SummonCooldown <= 0, fires once after FirstDelay.
        /// </summary>
        public bool SummonerReady(float dt)
        {
            if (!_info.SummonerEnabled || !IsAlive) return false;

            summonerTimer += dt;

            // First-time delay
            if (!hasSummonedOnce)
            {
                if (summonerTimer >= Mathf.Max(0f, _info.SummonFirstDelay))
                {
                    summonerTimer = 0f;
                    hasSummonedOnce = true;
                    return true; // first activation
                }
                return false;
            }

            // Repeats only if cooldown > 0
            if (_info.SummonCooldown > 0f && summonerTimer >= _info.SummonCooldown)
            {
                summonerTimer = 0f;
                return true;
            }

            return false;
        }

        public void DoSummon()
        {
            if (!_info.SummonerEnabled || _info.SummonPrefab == null || !IsAlive) return;

            if (_info.SummonType == EnemyInfoSO.SummonMode.Burst)
                DoSummonBurst(_info.SummonCount);
            else
                StartCoroutine(SummonStreamRoutine(_info.SummonCount, Mathf.Max(0.01f, _info.SummonStreamInterval)));
        }

        private void DoSummonBurst(int count)
        {
            int c = Mathf.Max(1, count);
            for (int i = 0; i < c; i++)
            {
                var pos = GetSummonWorldPos(i);
                EnemySpawner.Instance.SpawnSummonedEnemy(_info.SummonPrefab, pos);
            }
        }

        private IEnumerator SummonStreamRoutine(int count, float interval)
        {
            int c = Mathf.Max(1, count);
            for (int i = 0; i < c; i++)
            {
                var pos = GetSummonWorldPos(i);
                EnemySpawner.Instance.SpawnSummonedEnemy(_info.SummonPrefab, pos);
                if (i < c - 1) yield return new WaitForSeconds(interval);
            }
        }

        private Vector3 GetSummonWorldPos(int index)
        {
            // "In front" means toward the base: smaller depth (z)
            float z = transform.position.z - Mathf.Max(0f, _info.SummonForwardDepth);

            // Small lateral jitter so they don't overlap exactly
            float x = transform.position.x + Random.Range(-_info.SummonXJitter, _info.SummonXJitter);

            return new Vector3(x, 0f, z);
        }

        // BOSS
        public void AddShieldChargesRT(int amount)
        {
            if (amount <= 0) return;
            shieldChargesRT += amount;
        }

        public void JumpDepth(float deltaZ)
        {
            transform.DOKill(false);

            Vector3 start = transform.position;
            float targetZ = start.z + deltaZ;
            targetZ = Mathf.Max(targetZ, attackRange);

            Vector3 target = new Vector3(start.x, start.y, targetZ);

            const float duration = 1f;
            const float jumpPower = 0.6f; // visual arc height
            const int numJumps = 1;

            transform.DOJump(target, jumpPower, numJumps, duration)
                .SetEase(Ease.OutQuad)
                .SetUpdate(false);
        }


        public void SpecialHitGunners(float damage, float radius, int maxTargets)
        {
            HitNearestGunners(Mathf.Max(0f, damage), Mathf.Max(0f, radius), Mathf.Clamp(maxTargets, 1, 5));
        }

        // call this from EnemyManager tick (since it already loops enemies) or Enemy.Update if you prefer
        public void TickBuffs(float dt)
        {
            if (_buffTimer <= 0f) return;
            _buffTimer -= dt;
            if (_buffTimer <= 0f)
            {
                _armorDeltaRT = 0f;
                _dodgeDeltaRT = 0f;
                _speedMultRT = 1f;
                _damageMultRT = 1f;
            }
        }


        public void LockMovement(float seconds)
        {
            if (seconds <= 0f) return;
            MovementLockTimer = Mathf.Max(MovementLockTimer, seconds);
        }

        public void TickMovementLock(float dt)
        {
            if (MovementLockTimer > 0f)
                MovementLockTimer -= dt;
        }

    }
}