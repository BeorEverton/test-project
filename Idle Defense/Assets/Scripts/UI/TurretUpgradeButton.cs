using Assets.Scripts.Systems;
using Assets.Scripts.Turrets;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI
{
    public class TurretUpgradeButton : MonoBehaviour
    {
        [Header("Set in Runtime")]
        private TurretUpgradeManager _upgradeManager;

        [Header("Assigned in Inspector")]
        public BaseTurret _baseTurret;
        [SerializeField] private TurretStatsInstance _turret;

        [Header("UI Elements (Auto-Assigned)")]
        [SerializeField] private TextMeshProUGUI _statName, _statValue, _statUpgradeAmount, _statUpgradeCost;

        [Header("Upgrade Type")]
        [SerializeField] private TurretUpgradeType _upgradeType;

        private void Awake()
        {
            // Auto-assign the first two TextMeshProUGUI components in children
            TextMeshProUGUI[] tmpros = GetComponentsInChildren<TextMeshProUGUI>();

            if (tmpros.Length >= 4)
            {
                _statName = tmpros[0];
                _statValue = tmpros[1];
                _statUpgradeAmount = tmpros[2];
                _statUpgradeCost = tmpros[3];
            }
            else
            {
                Debug.LogWarning($"[TurretUpgradeButton] Couldn't auto-assign TextMeshProUGUI on {name}");
            }
        }

        public void Init()
        {
            _upgradeManager = FindFirstObjectByType<TurretUpgradeManager>();
            _turret = _baseTurret.GetStats();

            // Update the initial data
            _statName.SetText(GetDisplayNameForUpgrade(_upgradeType));
            UpdateDisplay();
        }

        private void OnEnable()
        {
            GameManager.Instance.OnMoneyChanged += HandleMoneyChanged;
            UpdateInteractableState(); // Run once on enable
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnMoneyChanged -= HandleMoneyChanged;
        }

        private void HandleMoneyChanged(ulong _)
        {
            UpdateInteractableState();
        }

        public void OnClick()
        {
            if (_upgradeManager == null)
                _upgradeManager = FindFirstObjectByType<TurretUpgradeManager>();

            _upgradeManager.UpgradeStat(_turret, _upgradeType, this);
        }

        public void EnableTooltip()
        {
            string description = GetUpgradeDescription(_upgradeType);
            TooltipManager.Instance.ShowTooltip(description);
        }

        public void DisableTooltip()
        {
            TooltipManager.Instance.HideTooltip();
        }

        public void UpdateStats(string value, string upgradeAmount, string upgradeCost)
        {
            _statValue.SetText(value);
            _statUpgradeAmount.SetText(upgradeAmount);
            _statUpgradeCost.SetText(upgradeCost);
        }

        private string GetDisplayNameForUpgrade(TurretUpgradeType type)
        {
            return type switch
            {
                TurretUpgradeType.Damage => "Damage",
                TurretUpgradeType.FireRate => "Fire Rate",
                TurretUpgradeType.CriticalChance => "Critical Chance",
                TurretUpgradeType.CriticalDamageMultiplier => "Critical Damage",
                TurretUpgradeType.ExplosionRadius => "Explosion Radius",
                TurretUpgradeType.SplashDamage => "Splash Damage",
                TurretUpgradeType.PierceChance => "Pierce Chance",
                TurretUpgradeType.PierceDamageFalloff => "Pierce Damage",
                TurretUpgradeType.PelletCount => "Pellet Count",
                TurretUpgradeType.DamageFalloffOverDistance => "Range Falloff",
                TurretUpgradeType.PercentBonusDamagePerSec => "Bonus Dmg/s",
                TurretUpgradeType.SlowEffect => "Slow Effect",
                TurretUpgradeType.KnockbackStrength => "Knockback",
                _ => type.ToString()
            };
        }

        public void UpdateDisplay()
        {
            _upgradeManager.UpdateUpgradeDisplay(_turret, _upgradeType, this);
        }

        private string GetUpgradeDescription(TurretUpgradeType type)
        {
            return type switch
            {
                TurretUpgradeType.Damage => "Increases turret base damage.",
                TurretUpgradeType.FireRate => "Increases shots per second. Faster firing speed.",
                TurretUpgradeType.CriticalChance => "Raises chance for critical hits. Max 50%.",
                TurretUpgradeType.CriticalDamageMultiplier => "Increases damage multiplier for critical hits.",
                TurretUpgradeType.ExplosionRadius => "Enlarges explosion area of missiles.",
                TurretUpgradeType.SplashDamage => "Damages nearby enemies hit by explosions.",
                TurretUpgradeType.PierceChance => "Adds chance for shots to pierce through enemies.",
                TurretUpgradeType.PierceDamageFalloff => "Reduces the damage lost on each pierced enemy.",
                TurretUpgradeType.PelletCount => "Adds more pellets per shotgun shot.",
                TurretUpgradeType.DamageFalloffOverDistance => "Reduces damage lost over distance for shotgun.",
                TurretUpgradeType.PercentBonusDamagePerSec => "Damage increases the longer it hits the same target.",
                TurretUpgradeType.SlowEffect => "Applies a slowing effect on enemies hit.",
                TurretUpgradeType.KnockbackStrength => "Pushes enemies back on hit. Stronger knockback per level.",
                _ => "Upgrade effect not documented."
            };
        }

        public void UpdateInteractableState()
        {
            if (_baseTurret == null || _upgradeManager == null)
                return;

            float cost = _upgradeManager.GetUpgradeCost(_turret, _upgradeType);
            Button button = GetComponentInChildren<Button>();

            if (button != null)
                button.interactable = GameManager.Instance.Money >= (ulong)cost;
        }
    }
}