using Assets.Scripts.SO;
using Assets.Scripts.Systems.Audio;
using Assets.Scripts.UI;
using System;
using UnityEngine;

namespace Assets.Scripts.Systems
{
    public class PlayerBaseManager : MonoBehaviour
    {
        public static PlayerBaseManager Instance { get; private set; }

        public event EventHandler OnWaveFailed;
        public event Action<float, float> OnHealthChanged; // (currentHealth, maxHealth)
        public event Action<float, float> OnMaxHealthChanged; // (newMaxHealth, currentHealth)


        [SerializeField] private PlayerBaseSO _baseInfo;  // The original SO from the inspector
        public PlayerBaseSO Info { get; private set; }    // The runtime clone

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

        [Tooltip("Visuals for the player base upgrades. Assign 3 objects.")]
        [SerializeField] private GameObject[] upgradeVisuals;
        private readonly int[] _unlockThresholds = { 50, 100, 250 };

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);

            Info = Instantiate(_baseInfo); // Copy the SO

            InitializeGame();
        }

        public void InitializeGame(bool startTime = false)
        {
            // Recalculate values based on upgrade levels
            _runtimeMaxHealth = Info.MaxHealth + Info.MaxHealthLevel * Info.MaxHealthUpgradeAmount;
            _runtimeRegenAmount = Info.RegenAmount + Info.RegenAmountLevel * Info.RegenAmountUpgradeAmount;
            _runtimeRegenDelay = Info.RegenDelay; // This one is fixed
            _runtimeRegenInterval = Mathf.Max(
                MinRegenInterval,
                Info.RegenInterval - Info.RegenIntervalLevel * Info.RegenIntervalUpgradeAmount
            );

            _currentHealth = _runtimeMaxHealth;
            _regenDelayTimer = 0f;
            _regenTickTimer = 0f;

            OnHealthChanged?.Invoke(_currentHealth, _runtimeMaxHealth);

            if (startTime)
                Time.timeScale = UIManager.Instance.timeSpeedOnDeath; // Resume the game only used in case of death
        }

        public void TakeDamage(float amount)
        {
            if (_isDead)
                return;

            _currentHealth = Mathf.Max(0f, _currentHealth - amount);
            _regenDelayTimer = 0f;
            _regenTickTimer = 0f;

            OnHealthChanged?.Invoke(_currentHealth, _runtimeMaxHealth);
            StatsManager.Instance.TotalDamageTaken += amount;
            AudioManager.Instance.Play("Take Damage");

            if (_currentHealth <= 0f)
            {
                OnWaveFailed?.Invoke(this, EventArgs.Empty);
                StatsManager.Instance.MissionsFailed++;
                AudioManager.Instance.Play("Player Death");
                AudioManager.Instance.Stop("Laser V2");
                AudioManager.Instance.StopAllMusics();
                AudioManager.Instance.PlayMusic("Main");
                UIManager.Instance.ShowDeathCountdown();
            }
        }

        private void Update()
        {
            if (_isDead || _currentHealth >= _runtimeMaxHealth)
                return;

            _regenDelayTimer += Time.deltaTime;

            if (!(_regenDelayTimer >= _runtimeRegenDelay))
                return;

            _regenTickTimer += Time.deltaTime;

            if (!(_regenTickTimer >= _runtimeRegenInterval))
                return;

            RepairPlayerBase();

            OnHealthChanged?.Invoke(_currentHealth, _runtimeMaxHealth);
        }

        private void RepairPlayerBase()
        {
            _currentHealth = Mathf.Min(_currentHealth + _runtimeRegenAmount, _runtimeMaxHealth);
            _regenTickTimer = 0f;

            StatsManager.Instance.TotalHealthRepaired += _runtimeRegenAmount;

            if (_currentHealth >= _runtimeMaxHealth)
            {
                // Play full health sound
                AudioManager.Instance.Play("Full Health");
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
            float level = Info.MaxHealthLevel;
            float cost = Info.MaxHealthUpgradeBaseCost * Mathf.Pow(1.1f, level);
            if (!TrySpend(cost))
                return;

            Info.MaxHealthLevel += 1f;
            _runtimeMaxHealth += Info.MaxHealthUpgradeAmount;

            // Heal by the upgraded amount
            _currentHealth += Info.MaxHealthUpgradeAmount;
            _currentHealth = Mathf.Min(_currentHealth, _runtimeMaxHealth);

            OnMaxHealthChanged?.Invoke(_runtimeMaxHealth, _currentHealth);
            OnHealthChanged?.Invoke(_currentHealth, _runtimeMaxHealth);

            UpdatePlayerBaseAppearance();
            AudioManager.Instance.Play("Upgrade");
        }

        public void UpgradeRegenAmount()
        {
            float level = Info.RegenAmountLevel;
            float cost = Info.RegenAmountUpgradeBaseCost * Mathf.Pow(1.1f, level);
            if (!TrySpend(cost))
                return;

            Info.RegenAmountLevel += 1f;
            _runtimeRegenAmount += Info.RegenAmountUpgradeAmount;
            UpdatePlayerBaseAppearance();
            AudioManager.Instance.Play("Upgrade");
        }

        public void UpgradeRegenInterval()
        {
            if (_runtimeRegenInterval <= MinRegenInterval)
                return;

            float level = Info.RegenIntervalLevel;
            float cost = Info.RegenIntervalUpgradeBaseCost * Mathf.Pow(1.1f, level);

            if (!TrySpend(cost))
                return;

            Info.RegenIntervalLevel += 1f;
            _runtimeRegenInterval = Mathf.Max(MinRegenInterval, _runtimeRegenInterval - Info.RegenIntervalUpgradeAmount);
            UpdatePlayerBaseAppearance();
            AudioManager.Instance.Play("Upgrade");
        }

        public void UpdateMaxHealthDisplay(PlayerUpgradeButton button)
        {
            float current = _runtimeMaxHealth;
            float bonus = Info.MaxHealthUpgradeAmount;
            float cost = Info.MaxHealthUpgradeBaseCost * Mathf.Pow(1.1f, Info.MaxHealthLevel);
            button.UpdateStats($"{current:F0}", $"+{bonus:F0}", $"${UIManager.AbbreviateNumber(cost)}");
        }

        public void UpdateRegenAmountDisplay(PlayerUpgradeButton button)
        {
            float current = _runtimeRegenAmount;
            float bonus = Info.RegenAmountUpgradeAmount;
            float cost = Info.RegenAmountUpgradeBaseCost * Mathf.Pow(1.1f, Info.RegenAmountLevel);
            button.UpdateStats($"{current:F1}", $"+{bonus:F1}", $"${UIManager.AbbreviateNumber(cost)}");
        }

        public void UpdateRegenIntervalDisplay(PlayerUpgradeButton button)
        {
            float current = _runtimeRegenInterval;
            float bonus = Info.RegenIntervalUpgradeAmount;
            float cost = Info.RegenIntervalUpgradeBaseCost * Mathf.Pow(1.1f, Info.RegenIntervalLevel);

            if (_runtimeRegenInterval <= 0.5f)
                button.UpdateStats($"{current:F2}s", "Max", "");
            else
                button.UpdateStats($"{current:F2}s", $"-{bonus:F2}s", $"${UIManager.AbbreviateNumber(cost)}");
        }

        public void LoadPlayerBase(PlayerBaseSO savedStats)
        {
            Info = savedStats;
            UpdatePlayerBaseAppearance();
            InitializeGame();
        }

        public void ResetPlayerBase()
        {
            Info = Instantiate(_baseInfo); // Copy the SO
            InitializeGame();
        }

        private void UpdatePlayerBaseAppearance()
        {
            if (upgradeVisuals == null || upgradeVisuals.Length == 0)
                return;

            float totalLevel = Info.MaxHealthLevel + Info.RegenAmountLevel + Info.RegenIntervalLevel;

            for (int i = 0; i < upgradeVisuals.Length; i++)
            {
                if (upgradeVisuals[i] != null)
                    upgradeVisuals[i].SetActive(totalLevel >= _unlockThresholds[i]);
            }
        }
    }

    public enum PlayerUpgradeType
    {
        MaxHealth,
        RegenAmount,
        RegenInterval
    }
}