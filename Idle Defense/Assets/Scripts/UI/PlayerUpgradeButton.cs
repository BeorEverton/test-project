using Assets.Scripts.PlayerBase;
using Assets.Scripts.Systems;
using Assets.Scripts.UpgradeSystem;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI
{
    public class PlayerUpgradeButton : MonoBehaviour
    {
        [Header("UI Elements (Auto-Assigned)")]
        [SerializeField] private TextMeshProUGUI _statName, _statValue, _statUpgradeAmount, _statUpgradeCost, _statUpgradeCount;

        [Header("Upgrade Type")]
        [SerializeField] private PlayerUpgradeType _upgradeType;

        private PlayerBaseManager _playerBaseManager;
        private PlayerBaseUpgradeManager _upgradeManager;
        private Button _button;

        private void Awake()
        {
            var tmpros = GetComponentsInChildren<TextMeshProUGUI>();
            if (tmpros.Length >= 5)
            {
                _statName = tmpros[0];
                _statValue = tmpros[1];
                _statUpgradeAmount = tmpros[2];
                _statUpgradeCount = tmpros[3];
                _statUpgradeCost = tmpros[4];
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
            _playerBaseManager = PlayerBaseManager.Instance;
            _upgradeManager = FindFirstObjectByType<PlayerBaseUpgradeManager>();
            _statName.SetText(GetDisplayNameForUpgrade(_upgradeType));
            GameManager.Instance.OnMoneyChanged += HandleMoneyChanged;
            _playerBaseManager.OnStatsLoaded += OnStatsLoaded;
            MultipleBuyOption.Instance.OnBuyAmountChanged += OnBuyAmountChanged;
        }

        private void OnBuyAmountChanged(object sender, EventArgs e)
        {
            UpdateInteractableState();
            UpdateDisplayFromType();
        }

        private void OnStatsLoaded(object sender, EventArgs e)
        {
            UpdateInteractableState();
            UpdateDisplayFromType();
        }

        public void OnClick()
        {
            _upgradeManager.UpgradePlayerBaseStat(_playerBaseManager.Stats, _upgradeType, this);
        }

        public void UpdateStats(string value, string bonus, string cost, string count)
        {
            _statValue.SetText(value);
            _statUpgradeAmount.SetText(bonus);
            _statUpgradeCost.SetText(cost);
            _statUpgradeCount.SetText(count);
        }

        public void UpdateDisplayFromType()
        {
            _upgradeManager.UpdateUpgradeDisplay(_playerBaseManager.Stats, _upgradeType, this);
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

        private string GetDisplayNameForUpgrade(PlayerUpgradeType type)
        {
            PlayerBaseUpgradeMeta meta = PlayerBaseUpgradeMetaManager.GetMeta(type);
            return meta != null ? meta.DisplayName : type.ToString();
        }

        private string GetUpgradeDescription(PlayerUpgradeType type)
        {
            PlayerBaseUpgradeMeta meta = PlayerBaseUpgradeMetaManager.GetMeta(type);
            return meta != null ? meta.Description : "Upgrade effect not documented.";
        }

        private void HandleMoneyChanged(ulong _)
        {
            UpdateInteractableState();
            UpdateDisplayFromType();
        }

        private void UpdateInteractableState()
        {
            if (_button == null || _upgradeManager == null)
                return;

            int amount = MultipleBuyOption.Instance.GetBuyAmount();

            float cost = _upgradeManager.GetPlayerBaseUpgradeCost(_playerBaseManager.Stats, _upgradeType, amount);
            int availableAmount = _upgradeManager.GetPlayerBaseAvailableUpgradeAmount(_playerBaseManager.Stats, _upgradeType);

            if (GameManager.Instance.Money >= cost && availableAmount > 0)
                _button.interactable = true;
            else
                _button.interactable = false;
        }
    }
}