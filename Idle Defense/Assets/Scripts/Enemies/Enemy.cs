using Assets.Scripts.SO;
using Assets.Scripts.Systems;
using Assets.Scripts.Systems.Audio;
using Assets.Scripts.WaveSystem;
using DamageNumbersPro;
using System;
using UnityEngine;
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

        [SerializeField] private DamageNumber damageNumber, damageNumberCritical;
        [SerializeField] private Transform _body;

        // Laser targeting
        private float _baseMovementSpeed;

#if UNITY_EDITOR //To display coinDrop and damage in the inspector debug mode
        private ulong _coinDropAmount;
        private float _damage;
#endif

        private void Start()
        {
            EnemyDeathEffect = GetComponent<EnemyDeathEffect>();
        }

        private void OnEnable()
        {
            ResetEnemy();
            LastGridPos = GridManager.Instance.GetGridPosition(transform.position);
            GridManager.Instance.AddEnemy(this);
#if UNITY_EDITOR
            _coinDropAmount = _info.CoinDropAmount;
            _damage = _info.Damage;
#endif
        }

        private void OnDisable()
        {
            IsBossInstance = false;
            GridManager.Instance.RemoveEnemy(this, LastGridPos);
        }

        public void TakeDamage(float amount, bool isCritical = false)
        {
            CurrentHealth -= amount;

            if (SettingsManager.Instance.AllowPopups)
            {
                if (damageNumberCritical && isCritical)
                    damageNumberCritical.Spawn(transform.position, amount);
                else if (damageNumber)
                    damageNumber.Spawn(transform.position, amount);
            }

            OnCurrentHealthChanged?.Invoke(this, EventArgs.Empty);

            CheckIfDead();
        }

        private void CheckIfDead()
        {
            if (!(CurrentHealth <= 0))
                return;

            IsAlive = false;

            OnDeath?.Invoke(this, new OnDeathEventArgs
            {
                CoinDropAmount = _info.CoinDropAmount
            });
        }

        private void ResetEnemy()
        {
            // Reset visual state if modified
            if (_originalScale.HasValue)
            {
                _body.localScale = _originalScale.Value;
            }

            if (_originalColor.HasValue && TryGetBodySpriteRenderer(out var sr))
                sr.color = _originalColor.Value;

            _originalScale = null;
            _originalColor = null;

            // Only reset info if not overridden (i.e., not a boss clone)
            if (!IsBossInstance && WaveManager.Instance.GetCurrentWave().WaveEnemies.ContainsKey(Info.EnemyClass))
            {
                Info = WaveManager.Instance.GetCurrentWave().WaveEnemies[Info.EnemyClass];
            }

            ApplyBodySpriteFromInfo();

            CanAttack = false;
            IsAlive = true;
            MaxHealth = Info.MaxHealth;
            CurrentHealth = MaxHealth;
            OnMaxHealthChanged?.Invoke(this, EventArgs.Empty);
            SetRandomMovementSpeed();

            // Apply boss visuals only after reset
            if (_applyBossVisualsAfterReset)
            {
                _applyBossVisualsAfterReset = false;
                SetAsBoss(_isMiniBoss);
            }

            KnockbackTime = 0f;
            KnockbackVelocity = Vector2.zero;

        }

        public void SetAsBoss(bool isMini)
        {
            if (_body == null)
            {
                Debug.LogWarning("Body not found on " + name);
                return;
            }

            // Store original state
            if (_originalScale == null)
                _originalScale = _body.localScale;

            if (TryGetBodySpriteRenderer(out SpriteRenderer sr) && _originalColor == null)
                _originalColor = sr.color;

            // Apply boss scaling + color
            if (isMini)
            {
                _body.localScale = _originalScale.Value * 2f;
                sr.color = Color.red;
            }
            else
            {
                _body.localScale = _originalScale.Value * 4f;
                sr.color = Color.black;
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

        public void ReduceMovementSpeed(float percent)
        {
            // Don't reduce speed again if already slowed with equal or stronger effect
            float newSpeed = _baseMovementSpeed * (1f - percent / 100f);
            if (IsSlowed && newSpeed >= MovementSpeed)
                return;

            MovementSpeed = newSpeed;
            IsSlowed = true;
        }
    }
}