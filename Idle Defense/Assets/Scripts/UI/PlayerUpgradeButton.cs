using Assets.Scripts.Systems;
using TMPro;
using UnityEngine;

namespace Assets.Scripts.UI
{
    public class PlayerUpgradeButton : MonoBehaviour
    {
        [Header("UI Elements (Auto-Assigned)")]
        [SerializeField] private TextMeshProUGUI _statName, _statValue, _statUpgradeAmount, _statUpgradeCost;

        [Header("Upgrade Type")]
        [SerializeField] private PlayerUpgradeType _upgradeType;

        private PlayerBaseManager _upgradeManager;

        private void Awake()
        {
            var tmpros = GetComponentsInChildren<TextMeshProUGUI>();
            if (tmpros.Length >= 4)
            {
                _statName = tmpros[0];
                _statValue = tmpros[1];
                _statUpgradeAmount = tmpros[2];
                _statUpgradeCost = tmpros[3];
            }
        }

        private void Start()
        {
            _upgradeManager = PlayerBaseManager.Instance;
            _statName.SetText(GetDisplayNameForUpgrade(_upgradeType));
            UpdateDisplayFromType();
        }

        public void OnClick()
        {
            switch (_upgradeType)
            {
                case PlayerUpgradeType.MaxHealth:
                    _upgradeManager.UpgradeMaxHealth();
                    break;
                case PlayerUpgradeType.RegenAmount:
                    _upgradeManager.UpgradeRegenAmount();
                    break;
                case PlayerUpgradeType.RegenInterval:
                    _upgradeManager.UpgradeRegenInterval();
                    break;
            }
            UpdateDisplayFromType();
        }

        public void UpdateStats(string value, string bonus, string cost)
        {
            _statValue.SetText(value);
            _statUpgradeAmount.SetText(bonus);
            _statUpgradeCost.SetText(cost);
        }

        public void UpdateDisplayFromType()
        {
            switch (_upgradeType)
            {
                case PlayerUpgradeType.MaxHealth:
                    _upgradeManager.UpdateMaxHealthDisplay(this);
                    break;
                case PlayerUpgradeType.RegenAmount:
                    _upgradeManager.UpdateRegenAmountDisplay(this);
                    break;
                case PlayerUpgradeType.RegenInterval:
                    _upgradeManager.UpdateRegenIntervalDisplay(this);
                    break;
            }
        }

        private string GetDisplayNameForUpgrade(PlayerUpgradeType type)
        {
            return type switch
            {
                PlayerUpgradeType.MaxHealth => "Max Health",
                PlayerUpgradeType.RegenAmount => "Regen / Tick",
                PlayerUpgradeType.RegenInterval => "Regen Rate",
                _ => type.ToString()
            };
        }
    }
}