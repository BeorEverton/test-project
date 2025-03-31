using Assets.Scripts.SO;
using Assets.Scripts.Systems;
using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Assets.Scripts.Enemies
{
    public class Enemy : MonoBehaviour
    {
        public event EventHandler OnMaxHealthChanged;
        public event EventHandler OnCurrentChanged;
        public event EventHandler OnDeath;

        [SerializeField] private EnemyInfoSO _info;
        public EnemyInfoSO Info => _info;

        public float MaxHealth { get; private set; }
        public bool IsDead => CurrentHealth <= 0;
        public float CurrentHealth { get; private set; }

        private float _timeSinceLastAttack = 0f;
        private bool CanAttack = false;
        private float _movementSpeed;
        private Vector2Int lastGridPos;

        private void OnEnable()
        {
            ResetEnemy();
            lastGridPos = GridManager.Instance.GetGridPosition(transform.position);
            GridManager.Instance.AddEnemy(this);
        }

        private void OnDisable()
        {
            GridManager.Instance.RemoveEnemy(this, lastGridPos);
        }

        private void Update()
        {
            if (!CanAttack)
            {
                MoveTowardsPlayer();

                Vector2Int currentGridPos = GridManager.Instance.GetGridPosition(transform.position);

                if (currentGridPos != lastGridPos)
                {
                    GridManager.Instance.RemoveEnemy(this, lastGridPos);
                    GridManager.Instance.AddEnemy(this);
                    lastGridPos = currentGridPos;
                }
            }

            TryAttacking();
        }

        private void MoveTowardsPlayer()
        {
            gameObject.transform.position += Vector3.down * _movementSpeed * Time.deltaTime;

            if (gameObject.transform.position.y <= _info.AttackRange)
                CanAttack = true;
        }

        private void TryAttacking()
        {
            if (!CanAttack)
                return;

            _timeSinceLastAttack += Time.deltaTime;
            if (_timeSinceLastAttack < _info.AttackSpeed)
                return;

            Attack();
            _timeSinceLastAttack = 0f;
        }

        protected virtual void Attack()
        {
            Debug.Log("Attacking");
        }

        public void TakeDamage(float amount)
        {
            CurrentHealth -= amount;

            OnCurrentChanged?.Invoke(this, EventArgs.Empty);

            CheckIfDead();
        }

        private void CheckIfDead()
        {
            if (CurrentHealth <= 0)
            {
                OnDeath?.Invoke(this, EventArgs.Empty);
            }
        }

        private void ResetEnemy()
        {
            CanAttack = false;
            MaxHealth = _info.MaxHealth + _info.AddMaxHealth;
            OnMaxHealthChanged?.Invoke(this, EventArgs.Empty);
            _timeSinceLastAttack = 0f;
            CurrentHealth = MaxHealth;
            SetRandomMovementSpeed();
        }

        private void SetRandomMovementSpeed()
        {
            _movementSpeed = Random.Range(_info.MovementSpeed - _info.MovementSpeedDifference, _info.MovementSpeed + _info.MovementSpeedDifference);
        }
    }
}