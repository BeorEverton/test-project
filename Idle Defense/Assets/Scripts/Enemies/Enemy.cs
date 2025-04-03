using Assets.Scripts.SO;
using Assets.Scripts.Systems;
using System;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;
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
        public bool IsDead => CurrentHealth <= 0;
        public float CurrentHealth { get; private set; }
        public bool IsSlowed { get; private set; }
        public bool CanAttack;
        public float TimeSinceLastAttack = 0f;
        public float MovementSpeed;
        public Vector2Int LastGridPos;

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

        public void TakeDamage(float amount)
        {
            CurrentHealth -= amount;

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
            CanAttack = false;
            MaxHealth = _info.MaxHealth;
            OnMaxHealthChanged?.Invoke(this, EventArgs.Empty);
            TimeSinceLastAttack = 0f;
            CurrentHealth = MaxHealth;
            SetRandomMovementSpeed();
        }

        private void SetRandomMovementSpeed()
        {
            MovementSpeed = Random.Range(_info.MovementSpeed - _info.MovementSpeedDifference, _info.MovementSpeed + _info.MovementSpeedDifference);
        }

        public void ReduceMovementSpeed(float procent)
        {
            MovementSpeed -= MovementSpeed * (procent / 100f);
            IsSlowed = true;
        }

        public void SetNewStats(EnemyInfoSO newStats)
        {
            _info = newStats;
            MaxHealth = _info.MaxHealth;
            OnMaxHealthChanged?.Invoke(this, EventArgs.Empty);
            TimeSinceLastAttack = 0f;
            CurrentHealth = MaxHealth;
            SetRandomMovementSpeed();
        }

        //private void Update()
        //{
        //    if (!CanAttack)
        //    {
        //        MoveTowardsPlayer();

        //        Vector2Int currentGridPos = GridManager.Instance.GetGridPosition(transform.position);

        //        if (currentGridPos != LastGridPos)
        //        {
        //            GridManager.Instance.RemoveEnemy(this, LastGridPos);
        //            GridManager.Instance.AddEnemy(this);
        //            LastGridPos = currentGridPos;
        //        }
        //    }
        //    else
        //    {
        //        TryAttacking();
        //    }
        //}

        //private void MoveTowardsPlayer()
        //{
        //    gameObject.transform.position += Vector3.down * MovementSpeed * Time.deltaTime;

        //    if (gameObject.transform.position.y <= _info.AttackRange)
        //        CanAttack = true;
        //}

        //private void TryAttacking()
        //{
        //    TimeSinceLastAttack += Time.deltaTime;
        //    if (TimeSinceLastAttack < _info.AttackSpeed)
        //        return;

        //    Attack();
        //    TimeSinceLastAttack = 0f;
        //}

        //protected virtual void Attack()
        //{
        //    Debug.Log("Attacking");
        //}
    }
}