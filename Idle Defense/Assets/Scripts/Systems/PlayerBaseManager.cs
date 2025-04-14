using Assets.Scripts.SO;
using Assets.Scripts.UI;
using System;
using UnityEngine;

namespace Assets.Scripts.Systems
{
    public class PlayerBaseManager : MonoBehaviour
    {
        public static PlayerBaseManager Instance { get; private set; }
        public event EventHandler OnWaveFailed; // TO-DO, Used to roll back 10 waves
        public event Action<float, float> OnHealthChanged; // (currentHealth, maxHealth)

        [SerializeField] private PlayerBaseSO _baseInfo;  // The original SO from the inspector
        private PlayerBaseSO _info;                      // The runtime clone

        private float _currentHealth;
        private float _regenDelayTimer;
        private float _regenTickTimer;

        private float _runtimeMaxHealth;
        private float _runtimeRegenAmount;
        private float _runtimeRegenDelay;
        private float _runtimeRegenInterval;
        private const float MinRegenInterval = 0.5f;

        public float CurrentHealth => _currentHealth;
        public float MaxHealth => _runtimeMaxHealth;

        private bool _isDead => _currentHealth <= 0f;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);

            _info = Instantiate(_baseInfo); // Copy the SO

            InitializeGame();
        }

        private void InitializeGame()
        {
            // Recalculate values based on upgrade levels
            _runtimeMaxHealth = _info.MaxHealth + _info.MaxHealthLevel * _info.MaxHealthUpgradeAmount;
            _runtimeRegenAmount = _info.RegenAmount + _info.RegenAmountLevel * _info.RegenAmountUpgradeAmount;
            _runtimeRegenDelay = _info.RegenDelay; // This one is fixed
            _runtimeRegenInterval = Mathf.Max(
                MinRegenInterval,
                _info.RegenInterval - _info.RegenIntervalLevel * _info.RegenIntervalUpgradeAmount
            );

            _currentHealth = _runtimeMaxHealth;
            _regenDelayTimer = 0f;
            _regenTickTimer = 0f;

            OnHealthChanged?.Invoke(_currentHealth, _runtimeMaxHealth);
        }

        private void Start()
        {
            OnHealthChanged?.Invoke(_currentHealth, _runtimeMaxHealth);
        }

        public void TakeDamage(float amount)
        {
            if (_isDead)
                return;

            _currentHealth = Mathf.Max(0f, _currentHealth - amount);
            _regenDelayTimer = 0f;
            _regenTickTimer = 0f;

            OnHealthChanged?.Invoke(_currentHealth, _runtimeMaxHealth);

            if (_currentHealth <= 0f)
            {
                OnWaveFailed?.Invoke(this, EventArgs.Empty);
                InitializeGame();
            }
        }

        private void Update()
        {
            if (_isDead || _currentHealth >= _runtimeMaxHealth)
                return;

            _regenDelayTimer += Time.deltaTime;

            if (_regenDelayTimer >= _runtimeRegenDelay)
            {
                _regenTickTimer += Time.deltaTime;
                if (_regenTickTimer >= _runtimeRegenInterval)
                {
                    _currentHealth = Mathf.Min(_currentHealth + _runtimeRegenAmount, _runtimeMaxHealth);
                    _regenTickTimer = 0f;

                    OnHealthChanged?.Invoke(_currentHealth, _runtimeMaxHealth);

                }
            }
        }

        private bool TrySpend(float cost)
        {
            if (GameManager.Instance.Money >= cost)
            {
                GameManager.Instance.SpendMoney((ulong)cost);
                return true;
            }

            Debug.Log("Not enough money.");
            return false;
        }

        public void UpgradeMaxHealth()
        {
            float level = _info.MaxHealthLevel;
            float cost = _info.MaxHealthUpgradeBaseCost * Mathf.Pow(1.1f, level);
            if (!TrySpend(cost))
                return;

            _info.MaxHealthLevel += 1f;
            _runtimeMaxHealth += _info.MaxHealthUpgradeAmount;

            // Heal by the upgraded amount
            _currentHealth += _info.MaxHealthUpgradeAmount;
            _currentHealth = Mathf.Min(_currentHealth, _runtimeMaxHealth);

            OnHealthChanged?.Invoke(_currentHealth, _runtimeMaxHealth);
        }

        public void UpgradeRegenAmount()
        {
            float level = _info.RegenAmountLevel;
            float cost = _info.RegenAmountUpgradeBaseCost * Mathf.Pow(1.1f, level);
            if (!TrySpend(cost))
                return;

            _info.RegenAmountLevel += 1f;
            _runtimeRegenAmount += _info.RegenAmountUpgradeAmount;
        }

        public void UpgradeRegenInterval()
        {
            if (_runtimeRegenInterval <= MinRegenInterval)
                return;

            float level = _info.RegenIntervalLevel;
            float cost = _info.RegenIntervalUpgradeBaseCost * Mathf.Pow(1.1f, level);

            if (!TrySpend(cost))
                return;

            _info.RegenIntervalLevel += 1f;
            _runtimeRegenInterval = Mathf.Max(MinRegenInterval, _runtimeRegenInterval - _info.RegenIntervalUpgradeAmount);
        }

        public void UpdateMaxHealthDisplay(PlayerUpgradeButton button)
        {
            float current = _runtimeMaxHealth;
            float bonus = _info.MaxHealthUpgradeAmount;
            float cost = _info.MaxHealthUpgradeBaseCost * Mathf.Pow(1.1f, _info.MaxHealthLevel);
            button.UpdateStats($"{current:F0}", $"+{bonus:F0}", $"${UIManager.AbbreviateNumber(cost)}");
        }

        public void UpdateRegenAmountDisplay(PlayerUpgradeButton button)
        {
            float current = _runtimeRegenAmount;
            float bonus = _info.RegenAmountUpgradeAmount;
            float cost = _info.RegenAmountUpgradeBaseCost * Mathf.Pow(1.1f, _info.RegenAmountLevel);
            button.UpdateStats($"{current:F1}", $"+{bonus:F1}", $"${UIManager.AbbreviateNumber(cost)}");
        }

        public void UpdateRegenIntervalDisplay(PlayerUpgradeButton button)
        {
            float current = _runtimeRegenInterval;
            float bonus = _info.RegenIntervalUpgradeAmount;
            float cost = _info.RegenIntervalUpgradeBaseCost * Mathf.Pow(1.1f, _info.RegenIntervalLevel);

            if (_runtimeRegenInterval <= 0.5f)
                button.UpdateStats($"{current:F2}s", "Max", "");
            else
                button.UpdateStats($"{current:F2}s", $"-{bonus:F2}s", $"${UIManager.AbbreviateNumber(cost)}");
        }
    }

    public enum PlayerUpgradeType
    {
        MaxHealth,
        RegenAmount,
        RegenInterval
    }
}