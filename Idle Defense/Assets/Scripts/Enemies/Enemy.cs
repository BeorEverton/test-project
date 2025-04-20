using Assets.Scripts.SO;
using Assets.Scripts.Systems;
using Assets.Scripts.WaveSystem;
using DamageNumbersPro;
using System;
using System.Collections;
using System.Collections.Generic;
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

        private Vector3? _originalScale;
        private Color? _originalColor;


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


        private void Start()
        {
            EnemyDeathEffect = GetComponent<EnemyDeathEffect>();
        }

        private void OnEnable()
        {
            ResetEnemy();
            LastGridPos = GridManager.Instance.GetGridPosition(transform.position);
            GridManager.Instance.AddEnemy(this);
        }

        private void OnDisable()
        {
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
                transform.localScale = _originalScale.Value;

            if (_originalColor.HasValue && TryGetSpriteRendererInChildren(out var sr))
                sr.color = _originalColor.Value;

            if (Camera.main != null)
                Camera.main.backgroundColor = Color.black;

            _originalScale = null;
            _originalColor = null;

            // Regular reset logic
            Info = WaveManager.Instance.GetCurrentWave().WaveEnemies[Info.EnemyClass];
            CanAttack = false;
            IsAlive = true;
            MaxHealth = Info.MaxHealth;
            CurrentHealth = MaxHealth;
            OnMaxHealthChanged?.Invoke(this, EventArgs.Empty);
            SetRandomMovementSpeed();
        }


        public void SetAsBoss(bool isMini)
        {
            Debug.Log("SetAsBoss called on " + name);
            if (_originalScale == null)
                _originalScale = transform.localScale;

            var sr = GetComponentInChildren<SpriteRenderer>();
            if (_originalColor == null && sr)
                _originalColor = sr.color;

            if (isMini)
            {
                transform.localScale = _originalScale.Value * 2f;
                sr.color = Color.red;
            }
            else
            {
                transform.localScale = _originalScale.Value * 4f;
                sr.color = Color.black;
                Camera.main.backgroundColor = new Color(0.3f, 0, 0); // dark red
            }
        }

        private bool TryGetSpriteRendererInChildren(out SpriteRenderer sr)
        {
            sr = GetComponentInChildren<SpriteRenderer>();
            return sr != null;
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