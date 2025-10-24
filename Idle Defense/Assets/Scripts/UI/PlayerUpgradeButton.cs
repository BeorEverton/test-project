using Assets.Scripts.PlayerBase;
using Assets.Scripts.Systems;
using Assets.Scripts.UpgradeSystem;
using DG.Tweening;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI
{
    public class PlayerUpgradeButton : MonoBehaviour
    {
        [Header("Currency")]
        [SerializeField] private Currency currencyType = Currency.Scraps;

        [SerializeField] private bool isPermanentUpgrade = false;

        [Header("UI Elements (Auto-Assigned)")]
        [SerializeField] private TextMeshProUGUI _statName, _statValue, _statUpgradeAmount, _statUpgradeCost, _statUpgradeCount;

        [Header("Upgrade Type")]
        [SerializeField] private PlayerUpgradeType _upgradeType;

        private PlayerBaseManager _playerBaseManager;
        private PlayerBaseUpgradeManager _upgradeManager;
        private Button _button;

        private Vector3 originalScale;
        private Color originalColor;


        private void Awake()
        {
            TextMeshProUGUI[] tmpros = GetComponentsInChildren<TextMeshProUGUI>();
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
                GameManager.Instance.OnCurrencyChanged += HandleCurrencyChanged;

            UpdateInteractableState(); // Run immediately on enable
            UpdateDisplayFromType();
        }

        private void OnDisable()
        {
            if (GameManager.Instance)
                GameManager.Instance.OnCurrencyChanged -= HandleCurrencyChanged;
        }

        private void Start()
        {
            _playerBaseManager = PlayerBaseManager.Instance;
            _upgradeManager = FindFirstObjectByType<PlayerBaseUpgradeManager>();
            _statName.SetText(GetDisplayNameForUpgrade(_upgradeType));
            GameManager.Instance.OnCurrencyChanged += HandleCurrencyChanged;

            MultipleBuyOption.Instance.OnBuyAmountChanged += OnBuyAmountChanged;
            _playerBaseManager.OnStatsLoaded += OnStatsLoaded;

            // For the button animation
            originalScale = _button.GetComponent<RectTransform>().localScale;
            originalColor = _button.GetComponent<Image>().color;
            Invoke("UpdateInteractableState", .5f); // Needs to delay because of PlayerBaseManager initialization
            Invoke("UpdateDisplayFromType", .5f);
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
            if (isPermanentUpgrade)
                _upgradeManager.UpgradePermanentStat(_upgradeType, this);
            else
                _upgradeManager.UpgradePlayerBaseStat(_playerBaseManager.Stats, _upgradeType, this);

        }

        public void UpdateStats(string value, string bonus, string cost, string count)
        {
            _statValue.SetText(value);
            _statUpgradeAmount.SetText(bonus);
            _statUpgradeCost.SetText(UIManager.GetCurrencyIcon(currencyType) + " " + cost);
            _statUpgradeCount.SetText(count);
        }

        public void UpdateDisplayFromType()
        {
            // Avoid errors on first activation
            if (_playerBaseManager == null || _upgradeManager == null)
                return;
            var stats = isPermanentUpgrade ? _playerBaseManager.PermanentStats : _playerBaseManager.Stats;
            _upgradeManager.UpdateUpgradeDisplay(stats, _upgradeType, this);

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

        private void HandleCurrencyChanged(Currency changedCurrency, ulong _)
        {
            if (changedCurrency != currencyType) return;

            UpdateInteractableState();
            UpdateDisplayFromType();
        }


        private void UpdateInteractableState()
        {
            if (_button == null || _upgradeManager == null)
                return;

            int amount = MultipleBuyOption.Instance.GetBuyAmount();
            var stats = isPermanentUpgrade ? _playerBaseManager.PermanentStats : _playerBaseManager.Stats;
            float cost = _upgradeManager.GetPlayerBaseUpgradeCost(stats, _upgradeType, amount);

            _button.interactable = GameManager.Instance.GetCurrency(currencyType) >= (ulong)cost;
        }

        public void OnPointerEnter()
        {
            // Scale down
            _button.GetComponent<RectTransform>().DOScale(originalScale * 0.96f, 0.05f).SetEase(Ease.OutQuad);

            // Red overlay
            _button.GetComponent<Image>().DOColor(Color.red, 0.05f);
        }

        public void OnPointerExit()
        {
            // Restore scale
            _button.GetComponent<RectTransform>().DOScale(originalScale, 0.1f).SetEase(Ease.OutQuad);

            // Restore color
            _button.GetComponent<Image>().DOColor(originalColor, 0.1f);
        }

    }
}