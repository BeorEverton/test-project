using Assets.Scripts.SO;
using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Assets.Scripts.Enemies
{
    public class Enemy : MonoBehaviour
    {
        public event EventHandler OnDeath;

        [SerializeField] private EnemyStatsSO _stats;
        public EnemyStatsSO Stats => _stats;
        public float MaxHealth { get; private set; }

        private float _currentHealth;
        private float _timeSinceLastAttack = 0f;
        private bool CanAttack = false;

        private void Awake()
        {
            MaxHealth = _stats.MaxHealth + _stats.AddMaxHealth;
        }

        private void Update()
        {
            if (!CanAttack)
                MoveTowardsPlayer();

            TryAttacking();

            if (Input.GetKeyDown(KeyCode.Space))
                TakeDamage(100);
        }

        private void MoveTowardsPlayer()
        {
            gameObject.transform.position += Vector3.down * _stats.Speed * Time.deltaTime;

            if (gameObject.transform.position.y <= _stats.AttackRange)
                CanAttack = true;
        }

        private void TryAttacking()
        {
            if (!CanAttack)
                return;

            _timeSinceLastAttack += Time.deltaTime;
            if (_timeSinceLastAttack < _stats.AttackSpeed)
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
            _currentHealth -= amount;
            Debug.Log($"[ENEMY] Damage taken. Current health {_currentHealth}");

            CheckIfDead();
        }

        private void CheckIfDead()
        {
            if (_currentHealth <= 0)
            {
                OnDeath?.Invoke(this, EventArgs.Empty);
                gameObject.SetActive(false);
            }
        }
    }
}