using Assets.Scripts.PlayerBase;
using Assets.Scripts.SO;
using Assets.Scripts.Systems.Audio;
using Assets.Scripts.UI;
using Assets.Scripts.WaveSystem;
using DG.Tweening;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

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
        public GameObject upgradePulseFX;
        private Vector3 originalPulseScale;

        private readonly int[] _unlockThresholds = { 50, 100, 250 };

        public Transform baseVisual; // The transform of the player base visual
        Vector3 originalScale;

        [Tooltip("Used to change the gameplay style to pause everything on death and start from wave 1")]
        public bool stopOnDeath = true; 

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
            originalPulseScale = upgradePulseFX.transform.localScale;
            originalScale = baseVisual.localScale;

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
            AnimateBaseDamage();

            if (_currentHealth > 0f)
                return;

            OnWaveFailed?.Invoke(this, EventArgs.Empty);
            StatsManager.Instance.MissionsFailed++;
            AudioManager.Instance.Play("Player Death");
            AudioManager.Instance.Stop("Laser V2");
            AudioManager.Instance.StopAllMusics();
            AudioManager.Instance.PlayMusic("Main");

            if (stopOnDeath)
            {
                UIManager.Instance.StopOnDeath();
            }
            else
                UIManager.Instance.ShowDeathCountdown();

            // Analytics event for player death
            AnalyticsManager.Instance.SendCustomEvent("PlayerDeath", new Dictionary<string, string>()
            {
                { "WaveOfDeath", WaveManager.Instance.GetCurrentWaveIndex().ToString() },
                { "DeathNumber", StatsManager.Instance.MissionsFailed.ToString() },
                { "MaxHealth", MaxHealth.ToString() },
                { "RegenAmount", Stats.RegenAmount.ToString() },
                { "RegenDelay", Stats.RegenInterval.ToString() }
            });
        }

        public void Heal(float amount)
        {
            if (_isDead)
                return;
            _currentHealth = Mathf.Min(_currentHealth + amount, Stats.MaxHealth);
            OnHealthChanged?.Invoke(_currentHealth, Stats.MaxHealth);
            StatsManager.Instance.TotalHealthRepaired += amount;
            if (_currentHealth >= Stats.MaxHealth)
            {
                AudioManager.Instance.Play("Full Health");
            }
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

            // Weird call because it needs the base gfx
            AnimateBaseUpgrade(upgradeVisuals[0].transform.parent.transform);
            PlayUpgradePulse(upgradePulseFX, upgradePulseFX.transform.parent.transform.localScale);
            float totalLevel = Stats.MaxHealthLevel + Stats.RegenAmountLevel + Stats.RegenIntervalLevel;

            for (int i = 0; i < upgradeVisuals.Length; i++)
            {
                if (upgradeVisuals[i] != null)
                    upgradeVisuals[i].SetActive(totalLevel >= _unlockThresholds[i]);
            }
        }

        public void AnimateBaseUpgrade(Transform baseGfx)
        {
            if (baseGfx == null)
                return;

            baseGfx.DOKill(); // Cancel any active tweens

            Vector3 originalScale = baseGfx.localScale;
            float popMultiplier = 1.2f;

            // Pop out and return to original scale
            Sequence seq = DOTween.Sequence();
            seq.Append(baseGfx.DOScale(originalScale * popMultiplier, 0.1f).SetEase(Ease.OutQuad));
            seq.Append(baseGfx.DOScale(originalScale, 0.1f).SetEase(Ease.InOutSine));
        }

        public void PlayUpgradePulse(GameObject upgradePulseFX, Vector3 baseScale)
        {
            if (upgradePulseFX == null)
            {
                Debug.LogWarning("upgradePulseFX GameObject is missing.");
                return;
            }

            SpriteRenderer sr = upgradePulseFX.GetComponent<SpriteRenderer>();
            if (sr == null)
            {
                Debug.LogWarning("upgradePulseFX is missing a SpriteRenderer.");
                return;
            }

            Vector3 defaultVisualScale = originalPulseScale;

            float duration = 0.4f;
            float scaleMultiplier = 6.0f;
            float startAlpha = 0.8f;

            Vector3 startScale = defaultVisualScale * 0.9f;
            Vector3 targetScale = defaultVisualScale * scaleMultiplier;

            upgradePulseFX.SetActive(true);
            upgradePulseFX.transform.localScale = startScale;
            sr.color = new Color(1f, 1f, 1f, startAlpha);

            DOTween.Kill(upgradePulseFX.transform);
            DOTween.Kill(sr);

            Sequence seq = DOTween.Sequence();
            seq.Join(upgradePulseFX.transform.DOScale(targetScale, duration).SetEase(Ease.OutCubic));
            seq.Join(sr.DOFade(0f, duration).SetEase(Ease.Linear));
            seq.OnComplete(() => upgradePulseFX.SetActive(false));
        }

        public void AnimateBaseDamage()
        {
            if (baseVisual == null) return;

            baseVisual.DOKill(); // cancel any ongoing tweens
            
            Vector3 punchScale = new Vector3(
                originalScale.x * 1.1f,
                originalScale.y * 0.8f,
                originalScale.z
            );

            Sequence seq = DOTween.Sequence();
            seq.Append(baseVisual.DOScale(punchScale, 0.08f).SetEase(Ease.OutCubic))
               .Append(baseVisual.DOScale(originalScale, 0.12f).SetEase(Ease.OutBack));
        }
    }
}