using Assets.Scripts.Systems;
using Assets.Scripts.Turrets;
using Assets.Scripts.UpgradeSystem;
using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


namespace Assets.Scripts.UI
{
    public class TurretUpgradeButton : MonoBehaviour
    {
        [Header("Currency Type")]
        [SerializeField] private Currency currencyType = Currency.Scraps;

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

        private Vector3 originalScale;
        private Color originalColor;

        // For gunners 
        private static TurretStatsInstance _scratch; // shared buffer to avoid GC

        public void SetUpgradeType(TurretUpgradeType type)
        {
            _upgradeType = type;
            if (_statName != null)
                _statName.SetText(GetDisplayNameForUpgrade(type));
            UpdateDisplayFromType();
        }

        private void Awake()
        {
            // Auto-assign the first two TextMeshProUGUI components in children
            TextMeshProUGUI[] tmpros = GetComponentsInChildren<TextMeshProUGUI>();

            if (tmpros.Length >= 5)
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

        private void Start()
        {
            // Store original scale and color for animations
            originalScale = _button.GetComponent<RectTransform>().localScale;
            originalColor = _button.GetComponent<Image>().color;
            // Initialize the button interactable state
            UpdateInteractableState();
        }

        /// <summary>
        /// Called by TurretUpgradePanelUI each time the panel opens.
        /// </summary>
        public void Init(BaseTurret baseTurret, Currency curType)
        {
            _baseTurret = baseTurret;
            currencyType = curType;                 //  decide which pool we’ll spend
            _upgradeManager ??= FindFirstObjectByType<TurretUpgradeManager>();

            // Pick the correct stat container (runtime or permanent)
            _turret = _baseTurret.GetUpgradeableStats(currencyType);

            _statName.SetText(GetDisplayNameForUpgrade(_upgradeType));
            UpdateDisplay();
            UpdateUpgradeAmount();
            UpdateInteractableState();
        }

        private void OnEnable()
        {
            StartCoroutine(LateEnable());            
        }

        // Used because these buttons are enabled at the start of the game and there wans't enough time for the instances
        IEnumerator LateEnable()
        {
            yield return null;
            GameManager.Instance.OnCurrencyChanged += HandleCurrencyChanged;
            MultipleBuyOption.Instance.OnBuyAmountChanged += OnBuyAmountChanged;
            UpdateUpgradeAmount();
            UpdateInteractableState();
            UpdateDisplayFromType();
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnCurrencyChanged -= HandleCurrencyChanged;


            MultipleBuyOption.Instance.OnBuyAmountChanged -= OnBuyAmountChanged;
        }

        private void OnBuyAmountChanged(object sender, EventArgs e)
        {
            UpdateDisplayFromType();
            UpdateUpgradeAmount();
            UpdateInteractableState();
        }

        private void HandleCurrencyChanged(Currency type, ulong _)
        {
            if (type != currencyType) return;

            UpdateDisplayFromType();
            UpdateUpgradeAmount();
            UpdateInteractableState();
        }

        public void UpdateDisplayFromType()
        {
            if (_upgradeManager == null) return;

            // Let the manager compute cost/bonus from the true upgradeable container
            _upgradeManager.UpdateUpgradeDisplay(_turret, _upgradeType, this);

            // Then replace the visible number with the effective (base + gunner)
            ReplaceWithEffectiveValue();
        }

        public void OnClick()
        {
            if (_upgradeManager == null)
                _upgradeManager = FindFirstObjectByType<TurretUpgradeManager>();

            if (currencyType == Currency.BlackSteel)
                _upgradeManager.UpgradePermanentTurretStat(
                    _baseTurret, _upgradeType, this, _upgradeAmount);   // edits PermanentStats
            else
            {
                _upgradeManager.UpgradeTurretStat(
                    _turret, _upgradeType, this, _upgradeAmount, currencyType);
                _baseTurret.RecomputeEffectiveFromGunner();
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

        public void UpdateStats(string value, string upgradeAmount, string upgradeCost, string count)
        {
            _statValue.SetText(value);
            _statUpgradeAmount.SetText(upgradeAmount);
            _statUpgradeCost.SetText(UIManager.GetCurrencyIcon(currencyType) + " " + upgradeCost);
            _statUpgradeCount.SetText(count);

            // Get gunner stats
            ReplaceWithEffectiveValue();

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
            ReplaceWithEffectiveValue();
        }

        public void UpdateInteractableState()
        {
            if (_baseTurret == null || _upgradeManager == null)
                return;

            int amount = MultipleBuyOption.Instance.GetBuyAmount();
            float cost = _upgradeManager.GetTurretUpgradeCost(
                _turret, _upgradeType, amount, currencyType);

            bool hasEnough = GameManager.Instance.GetCurrency(currencyType) >= (ulong)cost;
            _button.interactable = hasEnough && _upgradeAmount > 0;

        }

        private void UpdateUpgradeAmount()
        {
            _upgradeAmount = MultipleBuyOption.Instance.GetBuyAmount();
        }

        public void OnPointerEnter()
        {
            // Scale down
            _button.GetComponent<RectTransform>().DOScale(originalScale * 0.96f, 0.05f).SetEase(Ease.OutQuad).SetUpdate(true);

            // Red overlay
            _button.GetComponent<Image>().DOColor(Color.red, 0.05f).SetUpdate(true);
        }

        public void OnPointerExit()
        {
            // Restore scale
            _button.GetComponent<RectTransform>().DOScale(originalScale, 0.1f).SetEase(Ease.OutQuad).SetUpdate(true);

            // Restore color
            _button.GetComponent<Image>().DOColor(originalColor, 0.1f).SetUpdate(true);
        }

        #region Gunner Support        

        private void ReplaceWithEffectiveValue()
        {
            // If there's no turret bound, we can't compute an effective value.
            if (_baseTurret == null) return;

            // 1) Start from a FULL clone of the turret's baseline (mirrors gameplay path).
            //    This guarantees every field (e.g., PelletCount, SplashDamage) has a valid base value.
            _scratch = BaseTurret.CloneStatsWithoutLevels(_baseTurret.RuntimeStats);

            // 2) Layer Gunner bonuses, using the same "apply deltas to an existing sheet" model
            //    that the runtime uses (BuildEffectiveStats in BaseTurret).
            if (GunnerManager.Instance != null)
            {
                // Apply on top of the cloned baseline to preserve untouched stats.
                GunnerManager.Instance.ApplyTo(_baseTurret.RuntimeStats, _baseTurret.SlotIndex, _scratch);
            }

            // 3) Layer Prestige bonuses for the final effective display (UI-only; does not mutate runtime/permanent).
            if (PrestigeManager.Instance != null)
            {
                PrestigeManager.Instance.ApplyToTurretStats(_scratch);
            }

            // 4) Read the correct field from the fully-built effective sheet and show it.
            float v = GetEffectiveForType(_upgradeType, _scratch);
            string s = FormatForType(_upgradeType, v);
            _statValue.SetText(s);
        }


        private static float GetEffectiveForType(TurretUpgradeType t, TurretStatsInstance s)
        {
            switch (t)
            {
                case TurretUpgradeType.Damage: return s.Damage;
                case TurretUpgradeType.FireRate: return s.FireRate;
                case TurretUpgradeType.Range: return s.Range;
                case TurretUpgradeType.RotationSpeed: return s.RotationSpeed;

                case TurretUpgradeType.CriticalChance: return s.CriticalChance;
                case TurretUpgradeType.CriticalDamageMultiplier: return s.CriticalDamageMultiplier;

                case TurretUpgradeType.ExplosionRadius: return s.ExplosionRadius;
                case TurretUpgradeType.SplashDamage: return s.SplashDamage;
                case TurretUpgradeType.PierceChance: return s.PierceChance;
                case TurretUpgradeType.PierceDamageFalloff: return s.PierceDamageFalloff;

                case TurretUpgradeType.PelletCount: return s.PelletCount;
                case TurretUpgradeType.DamageFalloffOverDistance: return s.DamageFalloffOverDistance;

                case TurretUpgradeType.KnockbackStrength: return s.KnockbackStrength;
                case TurretUpgradeType.PercentBonusDamagePerSec: return s.PercentBonusDamagePerSec;
                case TurretUpgradeType.SlowEffect: return s.SlowEffect;

                case TurretUpgradeType.BounceCount: return s.BounceCount;
                case TurretUpgradeType.BounceRange: return s.BounceRange;
                case TurretUpgradeType.BounceDelay: return s.BounceDelay;
                case TurretUpgradeType.BounceDamagePct: return s.BounceDamagePct;

                case TurretUpgradeType.ConeAngle: return s.ConeAngle;
                case TurretUpgradeType.ExplosionDelay: return s.ExplosionDelay;

                case TurretUpgradeType.AheadDistance: return s.AheadDistance;
                case TurretUpgradeType.MaxTrapsActive: return s.MaxTrapsActive;

                case TurretUpgradeType.ArmorPenetration: return s.ArmorPenetration;

                default: return float.NaN;
            }
        }

        private static string FormatForType(TurretUpgradeType t, float v)
        {
            switch (t)
            {
                case TurretUpgradeType.FireRate: return $"{v:F2}/s";
                case TurretUpgradeType.Range: return $"{v:F1}";
                case TurretUpgradeType.RotationSpeed: return $"{v:F1}";

                // Show fractional percentages when the stat can be upgraded by fractions
                case TurretUpgradeType.CriticalChance: return $"{v:F1}%";
                case TurretUpgradeType.CriticalDamageMultiplier: return $"{v:F1}%";

                case TurretUpgradeType.SplashDamage: return $"{(v * 100f):F1}%";
                case TurretUpgradeType.PierceChance: return $"{v:F1}%";
                case TurretUpgradeType.PierceDamageFalloff: return $"{v:F1}%";
                case TurretUpgradeType.DamageFalloffOverDistance: return $"{v:F1}%";

                case TurretUpgradeType.BounceDelay: return $"{v:F2}s";
                case TurretUpgradeType.BounceDamagePct: return $"{(v * 100f):F1}%";

                case TurretUpgradeType.ConeAngle: return $"{v:F1}°";
                case TurretUpgradeType.ExplosionDelay: return $"{v:F2}s";

                case TurretUpgradeType.SlowEffect: return $"{v:F1}%";
                case TurretUpgradeType.PercentBonusDamagePerSec: return $"{v:F1}%";
                case TurretUpgradeType.ArmorPenetration: return $"{v:F1}%";

                // Counts like pellets or trap pool still look best as ints, but for everything else prefer one decimal.
                default:
                    return $"{v:F1}";
            }
        }



        #endregion
    }
}