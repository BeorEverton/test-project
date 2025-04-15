using Assets.Scripts.Systems;
using Assets.Scripts.Turrets;
using System;
using TMPro;
using UnityEngine;

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

        private void Start()
        {
            _upgradeManager = FindFirstObjectByType<TurretUpgradeManager>();
            _turret = _baseTurret.GetStats();

            // Update the initial data
            SetTurret();
            _statName.SetText(GetDisplayNameForUpgrade(_upgradeType));
            UpdateDisplayFromType();
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
                TurretUpgradeType.PierceDamageFalloff => "Pierce Falloff",
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
    }
}