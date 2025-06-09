using Assets.Scripts.Systems;
using Assets.Scripts.Turrets;
using Assets.Scripts.UpgradeSystem;
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
        [SerializeField] private TextMeshProUGUI _statName, _statValue, _statUpgradeAmount, _statUpgradeCost, _statUpgradeCount;

        [Header("Upgrade Type")]
        [SerializeField] private TurretUpgradeType _upgradeType;

        private Button _button;
        private int _upgradeAmount;

        private void Awake()
        {
            // Auto-assign the first two TextMeshProUGUI components in children
            TextMeshProUGUI[] tmpros = GetComponentsInChildren<TextMeshProUGUI>();

            if (tmpros.Length >= 4)
            {
                _statName = tmpros[0];
                _statValue = tmpros[1];
                _statUpgradeAmount = tmpros[2];
                _statUpgradeCount = tmpros[3];
                _statUpgradeCost = tmpros[4];
            }
            else
                Debug.LogWarning($"[TurretUpgradeButton] Couldn't auto-assign TextMeshProUGUI on {name}");

            _button = GetComponentInChildren<Button>();
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
            MultipleBuyOption.Instance.OnBuyAmountChanged += OnBuyAmountChanged;

            UpdateUpgradeAmount();
            UpdateInteractableState();
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnMoneyChanged -= HandleMoneyChanged;

            MultipleBuyOption.Instance.OnBuyAmountChanged -= OnBuyAmountChanged;
        }

        private void OnBuyAmountChanged(object sender, EventArgs e)
        {
            UpdateDisplayFromType();
            UpdateUpgradeAmount();
            UpdateInteractableState();
        }

        private void HandleMoneyChanged(ulong _)
        {
            UpdateDisplayFromType();
            UpdateUpgradeAmount();
            UpdateInteractableState();
        }

        public void UpdateDisplayFromType()
        {
            _upgradeManager.UpdateUpgradeDisplay(_turret, _upgradeType, this);
        }

        public void OnClick()
        {
            if (_upgradeManager == null)
                _upgradeManager = FindFirstObjectByType<TurretUpgradeManager>();

            _upgradeManager.UpgradeTurretStat(_turret, _upgradeType, this, _upgradeAmount);
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

        public void UpdateStats(string value, string upgradeAmount, string upgradeCost, string count)
        {
            _statValue.SetText(value);
            _statUpgradeAmount.SetText(upgradeAmount);
            _statUpgradeCost.SetText(upgradeCost);
            _statUpgradeCount.SetText(count);
        }

        private string GetDisplayNameForUpgrade(TurretUpgradeType type)
        {
            TurretUpgradeMeta meta = TurretUpgradeMetaManager.GetMeta(type);
            return meta != null ? meta.DisplayName : type.ToString();
        }

        private string GetUpgradeDescription(TurretUpgradeType type)
        {
            TurretUpgradeMeta meta = TurretUpgradeMetaManager.GetMeta(type);
            return meta != null ? meta.Description : "Upgrade effect not documented.";
        }

        public void UpdateDisplay()
        {
            _upgradeManager.UpdateUpgradeDisplay(_turret, _upgradeType, this);
        }

        public void UpdateInteractableState()
        {
            if (_baseTurret == null || _upgradeManager == null)
                return;

            int amount = MultipleBuyOption.Instance.GetBuyAmount();
            float cost = _upgradeManager.GetTurretUpgradeCost(_turret, _upgradeType, amount);

            if (GameManager.Instance.Money >= cost && _upgradeAmount > 0)
                _button.interactable = true;
            else
                _button.interactable = false;
        }

        private void UpdateUpgradeAmount()
        {
            _upgradeAmount = MultipleBuyOption.Instance.GetBuyAmount();
        }
    }
}