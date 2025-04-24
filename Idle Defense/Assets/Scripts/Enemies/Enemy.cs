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
        private EnemyInfoSO _originalInfo; // Keeps the reference to the base SO
        private bool _wasBossInstance;
        private bool _applyBossVisualsAfterReset = false;
        private bool _isMiniBoss = false;
        private Vector3? _originalScale;
        private Color? _originalColor;
        private bool activeBoss = false; // Used to change music track when boss is dead


        public EnemyInfoSO Info
        {
            get => _info;
            set => _info = value;
        }

        public float MaxHealth { get; private set; }
        public float CurrentHealth { get; private set; }
        public bool IsSlowed { get; private set; }
        public EnemyDeathEffect EnemyDeathEffect { get; private set; }

        public bool IsAlive;
        public bool CanAttack;
        public float TimeSinceLastAttack = 0f;
        public float MovementSpeed;
        public Vector2Int LastGridPos;

        [SerializeField] private DamageNumber damageNumber, damageNumberCritical;

        // Laser targeting
        private float _baseMovementSpeed;

#if UNITY_EDITOR //To display coinDrop and damage in the inspector debug mode
        private ulong _coinDropAmount;
        private float _damage;
#endif


        private void Start()
        {
            EnemyDeathEffect = GetComponent<EnemyDeathEffect>();
            _originalInfo = _info;
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
            GridManager.Instance.RemoveEnemy(this, LastGridPos);
            if (activeBoss)
            {
                AudioManager.Instance.PlayMusic("Main");
                activeBoss = false;
            }
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
                Transform bodyTransform = transform.Find("Body");
                bodyTransform.localScale = _originalScale.Value;
            }

            if (_originalColor.HasValue && TryGetBodySpriteRenderer(out var sr))
                sr.color = _originalColor.Value;

            _originalScale = null;
            _originalColor = null;

            // Only reset info if not overridden (i.e., not a boss clone)
            if (!_wasBossInstance)
            {
                Info = WaveManager.Instance.GetCurrentWave().WaveEnemies[Info.EnemyClass];
            }

            CanAttack = false;
            IsAlive = true;
            MaxHealth = Info.MaxHealth;
            CurrentHealth = MaxHealth;
            OnMaxHealthChanged?.Invoke(this, EventArgs.Empty);
            SetRandomMovementSpeed();

            SetRandomMovementSpeed();

            // Apply boss visuals only after reset
            if (_applyBossVisualsAfterReset)
            {
                _applyBossVisualsAfterReset = false;
                SetAsBoss(_isMiniBoss);
            }
        }

        public void SetAsBoss(bool isMini)
        {
            _originalInfo = _info;
            activeBoss = true;

            Transform bodyTransform = transform.Find("Body");
            if (bodyTransform == null)
            {
                Debug.LogWarning("Body not found on " + name);
                return;
            }

            // Store original state
            if (_originalScale == null)
                _originalScale = bodyTransform.localScale;

            if (TryGetBodySpriteRenderer(out var sr) && _originalColor == null)
                _originalColor = sr.color;

            // Apply boss scaling + color
            if (isMini)
            {
                bodyTransform.localScale = _originalScale.Value * 2f;
                sr.color = Color.red;
            }
            else
            {
                bodyTransform.localScale = _originalScale.Value * 4f;
                sr.color = Color.black;
                Camera.main.backgroundColor = new Color(0.3f, 0, 0);
            }
            _applyBossVisualsAfterReset = false;
            _wasBossInstance = false;
        }

        public void ApplyBossInfo(EnemyInfoSO clone, bool isMini)
        {
            _wasBossInstance = true;
            Info = clone;

            _applyBossVisualsAfterReset = true;
            _isMiniBoss = isMini;
        }



        private bool TryGetBodySpriteRenderer(out SpriteRenderer sr)
        {
            Transform bodyTransform = transform.Find("Body");
            if (bodyTransform != null)
            {
                sr = bodyTransform.GetComponent<SpriteRenderer>();
                return sr != null;
            }

            sr = null;
            return false;
        }



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