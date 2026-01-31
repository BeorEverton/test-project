using Assets.Scripts.Systems;
using Assets.Scripts.Turrets;
using Assets.Scripts.UpgradeSystem;
using DG.Tweening;
using System;
using System.Text;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


namespace Assets.Scripts.UI
{
    public class TurretUpgradeButton : MonoBehaviour
    {
        [Header("Currency Type")]
        [SerializeField] private Currency currencyType = Currency.Scraps;
        public Currency CurrencyType => currencyType;

        [Header("Set in Runtime")]
        private TurretUpgradeManager _upgradeManager;

        [Header("Assigned in Inspector")]
        public BaseTurret _baseTurret;
        [SerializeField] private TurretStatsInstance _turret;

        [Header("UI Elements (Auto-Assigned)")]
        [SerializeField] private TextMeshProUGUI _statName, _statValue, _statUpgradeAmount, _statUpgradeCost, _statUpgradeCount;

        [Header("Upgrade Type")]
        [SerializeField] private TurretUpgradeType _upgradeType;

        [Header("Optional Cooldown UI")]
        [SerializeField] private Slider _fireCooldownSlider;

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
            if (_fireCooldownSlider == null)
                _fireCooldownSlider = GetComponentInChildren<Slider>(true);
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

        /* Removed to show fire rate on the turret
        private void Update()
        {
            if (_upgradeType == TurretUpgradeType.FireRate)
                UpdateFireRateCooldownUI();
        }
        */

        private void UpdateFireRateCooldownUI()
        {
            if (_fireCooldownSlider == null)
                return;

            // Only show this slider on the FireRate button and when we have a turret instance.
            bool show = _upgradeType == TurretUpgradeType.FireRate && _baseTurret != null;

            if (_fireCooldownSlider.gameObject.activeSelf != show)
                _fireCooldownSlider.gameObject.SetActive(show);

            if (!show)
                return;

            _fireCooldownSlider.value = _baseTurret.FireCooldown01;
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

            // Replace BOTH the value and the shown +X using effective (gunner + prestige) math
            ReplaceWithEffectiveValueAndDelta();
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

        #region Tooltip Support
        public void EnableTooltip()
        {
            string description = GetUpgradeDescription(_upgradeType);

            // Append dynamic breakdown (base + gunner + prestige) so the tooltip matches what the player sees.
            string breakdown = BuildUpgradeBreakdownTooltip(_upgradeType);

            if (!string.IsNullOrEmpty(breakdown))
                description = $"{description}\n\n{breakdown}";

            TooltipManager.Instance.ShowTooltip(description);
        }

        public void DisableTooltip()
        {
            TooltipManager.Instance.HideTooltip();
        }

        private string BuildUpgradeBreakdownTooltip(TurretUpgradeType type)
        {
            if (_baseTurret == null || _baseTurret.RuntimeStats == null)
                return string.Empty;

            // 1) Base (turret-only)
            var baseScratch = BaseTurret.CloneStatsWithoutLevels(_baseTurret.RuntimeStats);
            float baseV = GetEffectiveForType(type, baseScratch);

            // 2) Base + Gunner
            var gunnerScratch = BaseTurret.CloneStatsWithoutLevels(_baseTurret.RuntimeStats);
            if (GunnerManager.Instance != null)
                GunnerManager.Instance.ApplyTo(_baseTurret.RuntimeStats, _baseTurret.SlotIndex, gunnerScratch);
            float gunnerV = GetEffectiveForType(type, gunnerScratch);

            // 3) Base + Gunner + Prestige
            var finalScratch = BaseTurret.CloneStatsWithoutLevels(_baseTurret.RuntimeStats);
            if (GunnerManager.Instance != null)
                GunnerManager.Instance.ApplyTo(_baseTurret.RuntimeStats, _baseTurret.SlotIndex, finalScratch);

            if (PrestigeManager.Instance != null)
                PrestigeManager.Instance.ApplyToTurretStats(finalScratch);

            float finalV = GetEffectiveForType(type, finalScratch);

            float gunnerDelta = gunnerV - baseV;
            float prestigeDelta = finalV - gunnerV;

            // Formatting
            string baseS = FormatForType(type, baseV);
            string gunnerDeltaS = FormatDeltaForType(type, gunnerDelta);
            string prestigeDeltaS = FormatDeltaForType(type, prestigeDelta);
            string totalS = FormatForType(type, finalV);

            var sb = new StringBuilder(128);
            sb.AppendLine($"Base: {baseS}");

            // Only show lines that matter (keeps tooltip clean in cases where bonus is 0)
            if (!IsZeroDelta(type, gunnerDelta))
                sb.AppendLine($"Gunner: {gunnerDeltaS}");

            if (!IsZeroDelta(type, prestigeDelta))
                sb.AppendLine($"Prestige: {prestigeDeltaS}");

            sb.AppendLine($"Total: {totalS}");

            return sb.ToString().TrimEnd();
        }

        private static bool IsZeroDelta(TurretUpgradeType type, float delta)
        {
            // Int-like stats should use integer comparison tolerance
            if (IsIntLike(type))
                return Mathf.Abs(Mathf.Round(delta)) <= 0f;

            return Mathf.Abs(delta) < 0.0001f;
        }

        private static bool IsIntLike(TurretUpgradeType type)
        {
            return type == TurretUpgradeType.PelletCount
                || type == TurretUpgradeType.BounceCount
                || type == TurretUpgradeType.MaxTrapsActive;
        }

        #endregion

        public void UpdateStats(string value, string upgradeAmount, string upgradeCost, string count)
        {
            _statValue.SetText(value);
            _statUpgradeAmount.SetText(upgradeAmount);
            _statUpgradeCost.SetText(UIManager.GetCurrencyIcon(currencyType) + " " + upgradeCost);
            _statUpgradeCount.SetText(count);

            // Replace BOTH the value and the shown +X using effective (gunner + prestige) math
            ReplaceWithEffectiveValueAndDelta();

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
            ReplaceWithEffectiveValueAndDelta();
        }

        public void UpdateInteractableState()
        {
            if (_baseTurret == null || _upgradeManager == null)
                return;

            float cost;
            int desired = MultipleBuyOption.Instance.GetBuyAmount();
            int effective = _upgradeManager.GetEffectiveBuyAmount(_turret, _upgradeType, desired, currencyType, out cost);

            _upgradeAmount = effective;

            bool hasEnough = effective > 0 && GameManager.Instance.GetCurrency(currencyType) >= (ulong)cost;
            _button.interactable = hasEnough;


        }

        private void UpdateUpgradeAmount()
        {
            if (_upgradeManager == null || _turret == null)
            {
                _upgradeAmount = MultipleBuyOption.Instance.GetBuyAmount();
                return;
            }

            float cost;
            int desired = MultipleBuyOption.Instance.GetBuyAmount();
            _upgradeAmount = _upgradeManager.GetEffectiveBuyAmount(_turret, _upgradeType, desired, currencyType, out cost);
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

        // Cached MemberwiseClone() reflection to avoid JSON allocations during frequent UI updates.
        private static System.Reflection.MethodInfo _memberwiseCloneMI;

        private static TurretStatsInstance CloneStatsFast(TurretStatsInstance src)
        {
            if (src == null) return null;

            // TurretStatsInstance is expected to be a class. MemberwiseClone makes a cheap copy of all fields.
            _memberwiseCloneMI ??= typeof(TurretStatsInstance).GetMethod(
                "MemberwiseClone",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
            );

            return (TurretStatsInstance)_memberwiseCloneMI.Invoke(src, null);
        }

        private TurretStatsInstance BuildEffectiveSheetFrom(TurretStatsInstance baseSheet)
        {
            // Start from a full clone so ApplyTo can layer deltas on top safely.
            TurretStatsInstance effective = CloneStatsFast(baseSheet);
            if (effective == null) return null;

            if (GunnerManager.Instance != null)
            {
                GunnerManager.Instance.ApplyTo(baseSheet, _baseTurret.SlotIndex, effective);
            }

            if (PrestigeManager.Instance != null)
            {
                PrestigeManager.Instance.ApplyToTurretStats(effective);
            }

            return effective;
        }

        private void ReplaceWithEffectiveValueAndDelta()
        {
            if (_baseTurret == null || _upgradeManager == null) return;

            // CURRENT (effective)
            TurretStatsInstance curBase = _turret; // runtime or permanent container chosen in Init()
            TurretStatsInstance curEff = BuildEffectiveSheetFrom(curBase);
            if (curEff == null) return;

            float curV = GetEffectiveForType(_upgradeType, curEff);

            // NEXT (effective) = clone base, apply upgrade to clone, then build effective
            TurretStatsInstance nextBase = CloneStatsFast(curBase);
            if (nextBase == null) return;

            _upgradeManager.ApplyUpgradeToStats(nextBase, _upgradeType, _upgradeAmount);

            TurretStatsInstance nextEff = BuildEffectiveSheetFrom(nextBase);
            if (nextEff == null) return;

            float nextV = GetEffectiveForType(_upgradeType, nextEff);

            // Update value
            _statValue.SetText(FormatForType(_upgradeType, curV));

            // Update delta shown on the button to match reality
            float delta = nextV - curV;
            _statUpgradeAmount.SetText(FormatDeltaForType(_upgradeType, delta));
        }

        private static string FormatDeltaForType(TurretUpgradeType t, float delta)
        {
            // Keep the same formatting as the stat, but force a + / - prefix.
            string sign = delta >= 0f ? "+" : "-";
            float v = Mathf.Abs(delta);

            // Reuse the same per-type formatting (but without losing the sign).
            switch (t)
            {
                case TurretUpgradeType.FireRate: return $"{sign}{v:F2}/s";
                case TurretUpgradeType.Range: return $"{sign}{v:F1}";
                case TurretUpgradeType.RotationSpeed: return $"{sign}{v:F1}";

                case TurretUpgradeType.CriticalChance: return $"{sign}{v:F1}%";
                case TurretUpgradeType.CriticalDamageMultiplier: return $"{sign}{v:F1}%";

                case TurretUpgradeType.SplashDamage: return $"{sign}{(v * 100f):F1}%";
                case TurretUpgradeType.PierceChance: return $"{sign}{v:F1}%";
                case TurretUpgradeType.PierceDamageFalloff: return $"{sign}{v:F1}%";
                case TurretUpgradeType.DamageFalloffOverDistance: return $"{sign}{v:F1}%";

                case TurretUpgradeType.BounceDelay: return $"{sign}{v:F2}s";
                case TurretUpgradeType.BounceDamagePct: return $"{sign}{(v * 100f):F1}%";

                case TurretUpgradeType.ConeAngle: return $"{sign}{v:F1}°";
                case TurretUpgradeType.ExplosionDelay: return $"{sign}{v:F2}s";

                case TurretUpgradeType.SlowEffect: return $"{sign}{v:F1}%";
                case TurretUpgradeType.PercentBonusDamagePerSec: return $"{sign}{v:F1}%";
                case TurretUpgradeType.ArmorPenetration: return $"{sign}{v:F1}%";

                default:
                    return $"{sign}{v:F1}";
            }
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