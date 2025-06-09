using Assets.Scripts.PlayerBase;
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
        public event EventHandler OnStatsLoaded;
        public event Action<float, float> OnHealthChanged; // (currentHealth, maxHealth)
        public event Action<float, float> OnMaxHealthChanged; // (newMaxHealth, currentHealth)

        [SerializeField] private PlayerBaseSO _baseInfo;
        [HideInInspector] public PlayerBaseStatsInstance SavedStats;
        public PlayerBaseStatsInstance Stats { get; private set; }

        private float _currentHealth;
        private float _regenTickTimer;

        public float CurrentHealth => _currentHealth;
        public float MaxHealth => Stats.MaxHealth;

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
        }

        private void Start()
        {
            Stats = IsValidSavedStats(SavedStats) ? SavedStats : new PlayerBaseStatsInstance(_baseInfo);
            OnStatsLoaded?.Invoke(this, EventArgs.Empty);

            InitializeGame();
        }

        private bool IsValidSavedStats(PlayerBaseStatsInstance stats) => stats is { MaxHealth: > 0 };

        public void InitializeGame(bool startTime = false)
        {
            _currentHealth = Stats.MaxHealth;
            _regenTickTimer = 0f;

            UpdatePlayerBaseAppearance();
            OnHealthChanged?.Invoke(_currentHealth, Stats.MaxHealth);

            if (startTime)
                Time.timeScale = UIManager.Instance.timeSpeedOnDeath; // Resume the game only used in case of death
        }

        public void TakeDamage(float amount)
        {
            if (_isDead)
                return;

            _currentHealth = Mathf.Max(0f, _currentHealth - amount);

            OnHealthChanged?.Invoke(_currentHealth, Stats.MaxHealth);
            StatsManager.Instance.TotalDamageTaken += amount;
            AudioManager.Instance.Play("Take Damage");

            if (_currentHealth > 0f)
                return;

            OnWaveFailed?.Invoke(this, EventArgs.Empty);
            StatsManager.Instance.MissionsFailed++;
            AudioManager.Instance.Play("Player Death");
            AudioManager.Instance.Stop("Laser V2");
            AudioManager.Instance.StopAllMusics();
            AudioManager.Instance.PlayMusic("Main");
            UIManager.Instance.ShowDeathCountdown();
        }

        private void Update()
        {
            if (_isDead || _currentHealth >= Stats.MaxHealth)
                return;

            if (_regenTickTimer < Stats.RegenInterval)
            {
                _regenTickTimer += Time.deltaTime;
                return;
            }

            RepairPlayerBase();

            OnHealthChanged?.Invoke(_currentHealth, Stats.MaxHealth);
        }

        private void RepairPlayerBase()
        {
            _currentHealth = Mathf.Min(_currentHealth + Stats.RegenAmount, Stats.MaxHealth);
            _regenTickTimer = 0f;

            StatsManager.Instance.TotalHealthRepaired += Stats.RegenAmount;

            if (_currentHealth >= Stats.MaxHealth)
            {
                AudioManager.Instance.Play("Full Health");
            }

            OnHealthChanged?.Invoke(_currentHealth, MaxHealth);
        }

        public void InvokeHealthChangedEvents()
        {
            OnMaxHealthChanged?.Invoke(Stats.MaxHealth, _currentHealth);
            OnHealthChanged?.Invoke(_currentHealth, Stats.MaxHealth);
        }

        public void ResetPlayerBase()
        {
            Stats = new PlayerBaseStatsInstance(_baseInfo);
            InitializeGame();
        }

        public void UpdatePlayerBaseAppearance()
        {
            if (upgradeVisuals == null || upgradeVisuals.Length == 0)
                return;

            float totalLevel = Stats.MaxHealthLevel + Stats.RegenAmountLevel + Stats.RegenIntervalLevel;

            for (int i = 0; i < upgradeVisuals.Length; i++)
            {
                if (upgradeVisuals[i] != null)
                    upgradeVisuals[i].SetActive(totalLevel >= _unlockThresholds[i]);
            }
        }
    }
}