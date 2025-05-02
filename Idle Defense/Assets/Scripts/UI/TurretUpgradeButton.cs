using Assets.Scripts.Systems;
using Assets.Scripts.Turrets;
using System;
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
            var tmpros = GetComponentsInChildren<TextMeshProUGUI>();

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
            SetTurret();
            _statName.SetText(GetDisplayNameForUpgrade(_upgradeType));
            UpdateDisplayFromType();
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

        public void SetTurret()
        {
            _upgradeManager.SetTurret(_turret, this);
        }

        public void OnClick()
        {
            if (_upgradeManager == null)
                _upgradeManager = FindFirstObjectByType<TurretUpgradeManager>();

            switch (_upgradeType)
            {
                case TurretUpgradeType.Damage:
                    _upgradeManager.UpgradeDamage();
                    break;
                case TurretUpgradeType.FireRate:
                    _upgradeManager.UpgradeFireRate();
                    break;
                case TurretUpgradeType.CriticalChance:
                    _upgradeManager.UpgradeCriticalChance();
                    break;
                case TurretUpgradeType.CriticalDamageMultiplier:
                    _upgradeManager.UpgradeCriticalDamageMultiplier();
                    break;
                case TurretUpgradeType.ExplosionRadius:
                    _upgradeManager.UpgradeExplosionRadius();
                    break;
                case TurretUpgradeType.SplashDamage:
                    _upgradeManager.UpgradeSplashDamage();
                    break;
                case TurretUpgradeType.PierceChance:
                    _upgradeManager.UpgradePierceChance();
                    break;
                case TurretUpgradeType.PierceDamageFalloff:
                    _upgradeManager.UpgradePierceDamageFalloff();
                    break;
                case TurretUpgradeType.PelletCount:
                    _upgradeManager.UpgradePelletCount();
                    break;
                case TurretUpgradeType.DamageFalloffOverDistance:
                    _upgradeManager.UpgradeDamageFalloffOverDistance();
                    break;
                case TurretUpgradeType.PercentBonusDamagePerSec:
                    _upgradeManager.UpgradePercentBonusDamagePerSec();
                    break;
                case TurretUpgradeType.SlowEffect:
                    _upgradeManager.UpgradeSlowEffect();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
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
                _ => type.ToString()
            };
        }

        public void UpdateDisplayFromType()
        {
            switch (_upgradeType)
            {
                case TurretUpgradeType.Damage:
                    _upgradeManager.UpdateDamageDisplay();
                    break;
                case TurretUpgradeType.FireRate:
                    _upgradeManager.UpdateFireRateDisplay();
                    break;
                case TurretUpgradeType.CriticalChance:
                    _upgradeManager.UpdateCriticalChanceDisplay();
                    break;
                case TurretUpgradeType.CriticalDamageMultiplier:
                    _upgradeManager.UpdateCriticalDamageMultiplierDisplay();
                    break;
                case TurretUpgradeType.ExplosionRadius:
                    _upgradeManager.UpdateExplosionRadiusDisplay();
                    break;
                case TurretUpgradeType.SplashDamage:
                    _upgradeManager.UpdateSplashDamageDisplay();
                    break;
                case TurretUpgradeType.PierceChance:
                    _upgradeManager.UpdatePierceChanceDisplay();
                    break;
                case TurretUpgradeType.PierceDamageFalloff:
                    _upgradeManager.UpdatePierceDamageFalloffDisplay();
                    break;
                case TurretUpgradeType.PelletCount:
                    _upgradeManager.UpdatePelletCountDisplay();
                    break;
                case TurretUpgradeType.DamageFalloffOverDistance:
                    _upgradeManager.UpdateDamageFalloffOverDistanceDisplay();
                    break;
                case TurretUpgradeType.PercentBonusDamagePerSec:
                    _upgradeManager.UpdatePercentBonusDamagePerSecDisplay();
                    break;
                case TurretUpgradeType.SlowEffect:
                    _upgradeManager.UpdateSlowEffectDisplay();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
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
                _ => "Upgrade effect not documented."
            };
        }

        public void UpdateInteractableState()
        {
            if (_baseTurret == null || _upgradeManager == null)
                return;

            float cost = _upgradeType switch
            {
                TurretUpgradeType.Damage => _upgradeManager.GetDamageUpgradeCost(_turret),
                TurretUpgradeType.FireRate => _upgradeManager.GetFireRateUpgradeCost(_turret),
                TurretUpgradeType.CriticalChance => _upgradeManager.GetCriticalChanceUpgradeCost(_turret),
                TurretUpgradeType.CriticalDamageMultiplier => _upgradeManager.GetCriticalDamageMultiplierUpgradeCost(_turret),
                TurretUpgradeType.ExplosionRadius => _upgradeManager.GetExplosionRadiusUpgradeCost(_turret),
                TurretUpgradeType.SplashDamage => _upgradeManager.GetSplashDamageUpgradeCost(_turret),
                TurretUpgradeType.PierceChance => _upgradeManager.GetPierceChanceUpgradeCost(_turret),
                TurretUpgradeType.PierceDamageFalloff => _upgradeManager.GetPierceDamageFalloffUpgradeCost(_turret),
                TurretUpgradeType.PelletCount => _upgradeManager.GetPelletCountUpgradeCost(_turret),
                TurretUpgradeType.DamageFalloffOverDistance => _upgradeManager.GetDamageFalloffOverDistanceUpgradeCost(_turret),
                TurretUpgradeType.PercentBonusDamagePerSec => _upgradeManager.GetBonusDamagePerSecUpgradeCost(_turret),
                TurretUpgradeType.SlowEffect => _upgradeManager.GetSlowEffectUpgradeCost(_turret),
                _ => float.MaxValue
            };

            Button button = GetComponentInChildren<Button>();
            if (button != null)
                button.interactable = GameManager.Instance.Money >= (ulong)cost;
        }
    }
}