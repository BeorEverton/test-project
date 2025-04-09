using Assets.Scripts.SO;
using System;
using UnityEngine;

namespace Assets.Scripts.Systems
{
    public class PlayerBaseManager : MonoBehaviour
    {
        public static PlayerBaseManager Instance { get; private set; }
        public event EventHandler OnWaveFailed; // TO-DO, Used to roll back 10 waves

        [SerializeField] private PlayerBaseSO _info;

        private float _currentHealth;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);

            InitializeGame();
        }

        private void InitializeGame()
        {
            _currentHealth = _info.MaxHealth;
        }

        public void TakeDamage(float amount)
        {
            if (_currentHealth > 0)
            {
                _currentHealth -= amount;
            }
            else
            {
                OnWaveFailed?.Invoke(this, EventArgs.Empty);

                InitializeGame();
            }
        }
    }
}