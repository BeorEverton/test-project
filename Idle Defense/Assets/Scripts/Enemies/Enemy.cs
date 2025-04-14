using Assets.Scripts.SO;
using Assets.Scripts.Systems;
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
        public EnemyInfoSO Info
        {
            get => _info;
            set => _info = value;
        }

        public float MaxHealth { get; private set; }
        public float CurrentHealth { get; private set; }
        public bool IsSlowed { get; private set; }
        public bool CanAttack;
        public float TimeSinceLastAttack = 0f;
        public float MovementSpeed;
        public Vector2Int LastGridPos;

        [SerializeField] private DamageNumber damageNumber, damageNumberCritical;

        // Laser targeting
        private float _baseMovementSpeed;

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

            if (damageNumberCritical && isCritical)
                damageNumberCritical.Spawn(transform.position, amount);
            else if (damageNumber)
                damageNumber.Spawn(transform.position, amount);

            OnCurrentHealthChanged?.Invoke(this, EventArgs.Empty);

            CheckIfDead();
        }

        private void CheckIfDead()
        {
            if (CurrentHealth <= 0)
            {
                OnDeath?.Invoke(this, new OnDeathEventArgs
                {
                    CoinDropAmount = _info.CoinDropAmount
                });
            }
        }

        private void ResetEnemy()
        {
            Info = WaveManager.Instance.GetCurrentWave().WaveEnemies[Info.EnemyClass];
            CanAttack = false;
            MaxHealth = _info.MaxHealth;
            OnMaxHealthChanged?.Invoke(this, EventArgs.Empty);
            TimeSinceLastAttack = 0f;
            CurrentHealth = MaxHealth;
            SetRandomMovementSpeed();
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