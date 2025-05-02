using Assets.Scripts.Systems;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


namespace Assets.Scripts.UI
{
    public class PlayerUpgradeButton : MonoBehaviour
    {
        [Header("UI Elements (Auto-Assigned)")]
        [SerializeField] private TextMeshProUGUI _statName, _statValue, _statUpgradeAmount, _statUpgradeCost;

        [Header("Upgrade Type")]
        [SerializeField] private PlayerUpgradeType _upgradeType;

        private PlayerBaseManager _upgradeManager;
        private Button _button;


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

            _button = GetComponentInChildren<Button>();
        }
        private void OnEnable()
        {
            if (GameManager.Instance)
                GameManager.Instance.OnMoneyChanged += HandleMoneyChanged;
            UpdateInteractableState(); // Run immediately on enable
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnMoneyChanged -= HandleMoneyChanged;
        }


        private void Start()
        {
            _upgradeManager = PlayerBaseManager.Instance;
            _statName.SetText(GetDisplayNameForUpgrade(_upgradeType));
            GameManager.Instance.OnMoneyChanged += HandleMoneyChanged;
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
                PlayerUpgradeType.MaxHealth => "Base Health",
                PlayerUpgradeType.RegenAmount => "Repair / Tick",
                PlayerUpgradeType.RegenInterval => "Repair Delay",
                _ => type.ToString()
            };
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

        private string GetUpgradeDescription(PlayerUpgradeType type)
        {
            return type switch
            {
                PlayerUpgradeType.MaxHealth => "Increases base maximum health.",
                PlayerUpgradeType.RegenAmount => "Increases the amount of heath repaired every tick.",
                PlayerUpgradeType.RegenInterval => "Reduces the time needed to start repairing after taking damage. Minimum 0.5s",
                _ => "Upgrade effect not documented."
            };
        }

        private void HandleMoneyChanged(ulong _)
        {
            UpdateInteractableState();
        }

        private void UpdateInteractableState()
        {
            if (_button == null || _upgradeManager == null)
                return;

            float cost = _upgradeType switch
            {
                PlayerUpgradeType.MaxHealth => _upgradeManager.Info.MaxHealthUpgradeBaseCost * Mathf.Pow(1.1f, _upgradeManager.Info.MaxHealthLevel),
                PlayerUpgradeType.RegenAmount => _upgradeManager.Info.RegenAmountUpgradeBaseCost * Mathf.Pow(1.1f, _upgradeManager.Info.RegenAmountLevel),
                PlayerUpgradeType.RegenInterval => _upgradeManager.Info.RegenIntervalUpgradeBaseCost * Mathf.Pow(1.1f, _upgradeManager.Info.RegenIntervalLevel),
                _ => 0f
            };

            _button.interactable = GameManager.Instance.Money >= (ulong)cost;
        }

    }
}