using Assets.Scripts.Systems.Audio;
using Assets.Scripts.Turrets;
using Assets.Scripts.UI;
using Assets.Scripts.UpgradeSystem;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Systems
{
    public class TurretUpgradeManager : MonoBehaviour
    {
        private TurretUpgradeButton turretUpgradeButton;

        public static event Action OnAnyTurretUpgraded;

        [Header("Cost Scaling Settings")]
        [SerializeField] private int hybridThreshold = 50;
        [SerializeField] private float quadraticFactor = 0.1f;
        [SerializeField] private float exponentialPower = 1.15f;

        private Dictionary<TurretUpgradeType, TurretUpgrade> _upgrades;

        private void Start()
        {
            InitializeUpgrades();
        }

        private void InitializeUpgrades()
        {
            _upgrades = new Dictionary<TurretUpgradeType, TurretUpgrade>
            {
                [TurretUpgradeType.Damage] = new()
                {
                    GetCurrentValue = t => t.Damage,
                    SetCurrentValue = (t, v) => t.Damage = v,
                    GetLevel = t => t.DamageLevel,
                    SetLevel = (t, v) => t.DamageLevel = v,
                    GetBaseCode = t => t.BaseDamage,
                    GetUpgradeAmount = t => t.DamageUpgradeAmount,
                    GetCostMultiplier = t => t.DamageCostExponentialMultiplier,
                    GetMaxValue = t => float.MaxValue,
                    GetMinValue = t => 0f,
                    GetCost = GetDamageUpgradeCost,
                    UpdateDisplay = UpdateDamageDisplay,
                    OnUpgrade = UpgradeDamage
                },
                [TurretUpgradeType.FireRate] = new()
                {
                    GetCurrentValue = t => t.FireRate,
                    SetCurrentValue = (t, v) => t.FireRate = v,
                    GetLevel = t => t.FireRateLevel,
                    SetLevel = (t, v) => t.FireRateLevel = v,
                    GetBaseCode = t => t.BaseFireRate,
                    GetUpgradeAmount = t => t.FireRateUpgradeAmount,
                    GetCostMultiplier = t => t.FireRateCostExponentialMultiplier,
                    GetMaxValue = t => float.MaxValue,
                    GetMinValue = t => 0f,
                    GetCost = GetFireRateUpgradeCost,
                    UpdateDisplay = UpdateFireRateDisplay,
                    OnUpgrade = UpgradeFireRate
                },
                [TurretUpgradeType.CriticalChance] = new()
                {
                    GetCurrentValue = t => t.CriticalChance,
                    SetCurrentValue = (t, v) => t.CriticalChance = v,
                    GetLevel = t => t.CriticalChanceLevel,
                    SetLevel = (t, v) => t.CriticalChanceLevel = v,
                    GetBaseCode = t => t.BaseCritChance,
                    GetUpgradeAmount = t => t.CriticalChanceUpgradeAmount,
                    GetCostMultiplier = t => t.CriticalChanceCostExponentialMultiplier,
                    GetMaxValue = t => 50f,
                    GetMinValue = t => 0f,
                    GetCost = GetCriticalChanceUpgradeCost,
                    UpdateDisplay = UpdateCriticalChanceDisplay,
                    OnUpgrade = UpgradeCriticalChance
                },
                [TurretUpgradeType.CriticalDamageMultiplier] = new()
                {
                    GetCurrentValue = t => t.CriticalDamageMultiplier,
                    SetCurrentValue = (t, v) => t.CriticalDamageMultiplier = v,
                    GetLevel = t => t.CriticalDamageMultiplierLevel,
                    SetLevel = (t, v) => t.CriticalDamageMultiplierLevel = v,
                    GetBaseCode = t => t.BaseCritDamage,
                    GetUpgradeAmount = t => t.CriticalDamageMultiplierUpgradeAmount,
                    GetCostMultiplier = t => t.CriticalDamageCostExponentialMultiplier,
                    GetMaxValue = t => float.MaxValue,
                    GetMinValue = t => 0f,
                    GetCost = GetCriticalDamageMultiplierUpgradeCost,
                    UpdateDisplay = UpdateCriticalDamageMultiplierDisplay,
                    OnUpgrade = UpgradeCriticalDamageMultiplier
                },
                [TurretUpgradeType.ExplosionRadius] = new()
                {
                    GetCurrentValue = t => t.ExplosionRadius,
                    SetCurrentValue = (t, v) => t.ExplosionRadius = v,
                    GetLevel = t => t.ExplosionRadiusLevel,
                    SetLevel = (t, v) => t.ExplosionRadiusLevel = v,
                    GetBaseCode = t => 0f,
                    GetUpgradeAmount = t => t.ExplosionRadiusUpgradeAmount,
                    GetCostMultiplier = t => 0f,
                    GetMaxValue = t => float.MaxValue,
                    GetMinValue = t => 0f,
                    GetCost = GetExplosionRadiusUpgradeCost,
                    UpdateDisplay = UpdateExplosionRadiusDisplay,
                    OnUpgrade = UpgradeExplosionRadius
                },
                [TurretUpgradeType.SplashDamage] = new()
                {
                    GetCurrentValue = t => t.SplashDamage,
                    SetCurrentValue = (t, v) => t.SplashDamage = v,
                    GetLevel = t => t.SplashDamageLevel,
                    SetLevel = (t, v) => t.SplashDamageLevel = v,
                    GetBaseCode = t => 0f,
                    GetUpgradeAmount = t => t.SplashDamageUpgradeAmount,
                    GetCostMultiplier = t => 0f,
                    GetMaxValue = t => float.MaxValue,
                    GetMinValue = t => 0f,
                    GetCost = GetSplashDamageUpgradeCost,
                    UpdateDisplay = UpdateSplashDamageDisplay,
                    OnUpgrade = UpgradeSplashDamage
                },
                [TurretUpgradeType.PierceChance] = new()
                {
                    GetCurrentValue = t => t.PierceChance,
                    SetCurrentValue = (t, v) => t.PierceChance = v,
                    GetLevel = t => t.PierceChanceLevel,
                    SetLevel = (t, v) => t.PierceChanceLevel = v,
                    GetBaseCode = t => 0f,
                    GetUpgradeAmount = t => t.PierceChanceUpgradeAmount,
                    GetCostMultiplier = t => 0f,
                    GetMaxValue = t => 100f,
                    GetMinValue = t => 0f,
                    GetCost = GetPierceChanceUpgradeCost,
                    UpdateDisplay = UpdatePierceChanceDisplay,
                    OnUpgrade = UpgradePierceChance
                },
                [TurretUpgradeType.PierceDamageFalloff] = new()
                {
                    GetCurrentValue = t => t.PierceDamageFalloff,
                    SetCurrentValue = (t, v) => t.PierceDamageFalloff = v,
                    GetLevel = t => t.PierceDamageFalloffLevel,
                    SetLevel = (t, v) => t.PierceDamageFalloffLevel = v,
                    GetBaseCode = t => 0f,
                    GetUpgradeAmount = t => t.PierceDamageFalloffUpgradeAmount,
                    GetCostMultiplier = t => 0f,
                    GetMaxValue = t => float.MaxValue,
                    GetMinValue = t => 0f,
                    GetCost = GetPierceDamageFalloffUpgradeCost,
                    UpdateDisplay = UpdatePierceDamageFalloffDisplay,
                    OnUpgrade = UpgradePierceDamageFalloff
                },
                [TurretUpgradeType.PelletCount] = new()
                {
                    GetCurrentValue = t => t.PelletCount,
                    SetCurrentValue = (t, v) => t.PelletCount = (int)v,
                    GetLevel = t => t.PelletCountLevel,
                    SetLevel = (t, v) => t.PelletCountLevel = v,
                    GetBaseCode = t => 0f,
                    GetUpgradeAmount = t => t.PelletCountUpgradeAmount,
                    GetCostMultiplier = t => 0f,
                    GetMaxValue = t => float.MaxValue,
                    GetMinValue = t => 0f,
                    GetCost = GetPelletCountUpgradeCost,
                    UpdateDisplay = UpdatePelletCountDisplay,
                    OnUpgrade = UpgradePelletCount
                },
                [TurretUpgradeType.DamageFalloffOverDistance] = new()
                {
                    GetCurrentValue = t => t.DamageFalloffOverDistance,
                    SetCurrentValue = (t, v) => t.DamageFalloffOverDistance = v,
                    GetLevel = t => t.DamageFalloffOverDistanceLevel,
                    SetLevel = (t, v) => t.DamageFalloffOverDistanceLevel = v,
                    GetBaseCode = t => 0f,
                    GetUpgradeAmount = t => t.DamageFalloffOverDistanceUpgradeAmount,
                    GetCostMultiplier = t => 0f,
                    GetMaxValue = t => float.MaxValue,
                    GetMinValue = t => 0f,
                    GetCost = GetDamageFalloffOverDistanceUpgradeCost,
                    UpdateDisplay = UpdateDamageFalloffOverDistanceDisplay,
                    OnUpgrade = UpgradeDamageFalloffOverDistance
                },
                [TurretUpgradeType.KnockbackStrength] = new()
                {
                    GetCurrentValue = t => t.KnockbackStrength,
                    SetCurrentValue = (t, v) => t.KnockbackStrength = v,
                    GetLevel = t => t.KnockbackStrengthLevel,
                    SetLevel = (t, v) => t.KnockbackStrengthLevel = v,
                    GetBaseCode = t => 0f,
                    GetUpgradeAmount = t => t.KnockbackStrengthUpgradeAmount,
                    GetCostMultiplier = t => t.KnockbackStrengthCostExponentialMultiplier,
                    GetMaxValue = t => float.MaxValue,
                    GetMinValue = t => 0f,
                    GetCost = GetKnockbackStrengthUpgradeCost,
                    UpdateDisplay = UpdateKnockbackStrengthDisplay,
                    OnUpgrade = UpgradeKnockbackStrength
                },
                [TurretUpgradeType.PercentBonusDamagePerSec] = new()
                {
                    GetCurrentValue = t => t.PercentBonusDamagePerSec,
                    SetCurrentValue = (t, v) => t.PercentBonusDamagePerSec = v,
                    GetLevel = t => t.PercentBonusDamagePerSecLevel,
                    SetLevel = (t, v) => t.PercentBonusDamagePerSecLevel = v,
                    GetBaseCode = t => 0f,
                    GetUpgradeAmount = t => t.PercentBonusDamagePerSecUpgradeAmount,
                    GetCostMultiplier = t => 0f,
                    GetMaxValue = t => float.MaxValue,
                    GetMinValue = t => 0f,
                    GetCost = GetBonusDamagePerSecUpgradeCost,
                    UpdateDisplay = UpdatePercentBonusDamagePerSecDisplay,
                    OnUpgrade = UpgradePercentBonusDamagePerSec
                },
                [TurretUpgradeType.SlowEffect] = new()
                {
                    GetCurrentValue = t => t.SlowEffect,
                    SetCurrentValue = (t, v) => t.SlowEffect = v,
                    GetLevel = t => t.SlowEffectLevel,
                    SetLevel = (t, v) => t.SlowEffectLevel = v,
                    GetBaseCode = t => 0f,
                    GetUpgradeAmount = t => t.SlowEffectUpgradeAmount,
                    GetCostMultiplier = t => 0f,
                    GetMaxValue = t => 100f,
                    GetMinValue = t => 0f,
                    GetCost = GetSlowEffectUpgradeCost,
                    UpdateDisplay = UpdateSlowEffectDisplay,
                    OnUpgrade = UpgradeSlowEffect
                }
            };
        }

        public void UpgradeStat(TurretStatsInstance turret, TurretUpgradeType type)
        {
            if (!_upgrades.TryGetValue(type, out TurretUpgrade upgrade))
                return;

            float cost = upgrade.GetCost(turret);
            if (upgrade.GetMaxValue != null && upgrade.GetCurrentValue(turret) >= upgrade.GetMaxValue(turret))
            {
                upgrade.UpdateDisplay?.Invoke(turret);
                return;
            }
            if (upgrade.GetMinValue != null && upgrade.GetCurrentValue(turret) <= upgrade.GetMinValue(turret))
            {
                upgrade.UpdateDisplay?.Invoke(turret);
                return;
            }

            if (TrySpend(cost))
            {
                float newValue = upgrade.GetCurrentValue(turret) + upgrade.GetUpgradeAmount(turret);
                upgrade.SetCurrentValue(turret, newValue);
                upgrade.SetLevel(turret, upgrade.GetLevel(turret) + 1);
                upgrade.UpdateDisplay?.Invoke(turret);
                AudioManager.Instance.Play("Upgrade");
                turretUpgradeButton._baseTurret.UpdateTurretAppearance();
                upgrade.OnUpgrade?.Invoke(turret);
                OnAnyTurretUpgraded?.Invoke();
            }
        }

        private bool TrySpend(float cost)
        {
            if (GameManager.Instance.Money >= cost)
            {
                GameManager.Instance.SpendMoney((ulong)cost);
                StatsManager.Instance.UpgradeAmount++;
                return true;
            }

            AudioManager.Instance.Play("No Money");
            Debug.Log("Not enough money.");
            return false;
        }

        private float GetHybridCost(float baseCost, float level)
        {
            if (level < hybridThreshold)
            {
                return baseCost * (1f + level * level * quadraticFactor);
            }
            else
            {
                return baseCost * Mathf.Pow(exponentialPower, level);
            }
        }

        private float GetExponentialCost_PlusLevel(float baseCost, float level, float exponentialMultiplier)
        {
            return baseCost + Mathf.Pow(exponentialMultiplier, level) + level;
        }

        private float GetExponentialCost(float baseCost, float level, float multiplier)
        {
            return baseCost * Mathf.Pow(multiplier, level);
        }


        public float GetKnockbackStrengthUpgradeCost(TurretStatsInstance t) =>
    GetHybridCost(t.KnockbackStrengthUpgradeBaseCost, t.KnockbackStrengthLevel);


        public void UpgradeDamage(TurretStatsInstance turret)
        {
            float cost = GetExponentialCost_PlusLevel(turret.DamageUpgradeBaseCost, turret.DamageLevel, turret.DamageCostExponentialMultiplier);

            if (TrySpend(cost))
            {
                turret.DamageLevel++;
                turret.Damage = turret.BaseDamage * Mathf.Pow(turret.DamageUpgradeAmount, turret.DamageLevel) + turret.DamageLevel;
                UpdateDamageDisplay(turret);
                AudioManager.Instance.Play("Upgrade");
                turretUpgradeButton._baseTurret.UpdateTurretAppearance();
                OnAnyTurretUpgraded?.Invoke();
            }

        }

        public void UpgradeFireRate(TurretStatsInstance turret)
        {
            float cost = GetExponentialCost_PlusLevel(turret.FireRateUpgradeBaseCost, turret.FireRateLevel, turret.FireRateCostExponentialMultiplier);

            if (TrySpend(cost))
            {
                turret.FireRateLevel++;
                turret.FireRate = turret.BaseFireRate + turret.FireRateUpgradeAmount * turret.FireRateLevel;
                UpdateFireRateDisplay(turret);
                AudioManager.Instance.Play("Upgrade");
                turretUpgradeButton._baseTurret.UpdateTurretAppearance();
                OnAnyTurretUpgraded?.Invoke();
            }
        }

        public void UpgradeCriticalChance(TurretStatsInstance turret)
        {
            if (turret.CriticalChance >= 50f)
                return;

            float cost = GetExponentialCost(turret.CriticalChanceUpgradeBaseCost, turret.CriticalChanceLevel, turret.CriticalChanceCostExponentialMultiplier);

            if (TrySpend(cost))
            {
                turret.CriticalChanceLevel++;
                turret.CriticalChance = Mathf.Min(50f, turret.BaseCritChance + turret.CriticalChanceUpgradeAmount * turret.CriticalChanceLevel);
                UpdateCriticalChanceDisplay(turret);
                AudioManager.Instance.Play("Upgrade");
                turretUpgradeButton._baseTurret.UpdateTurretAppearance();
                OnAnyTurretUpgraded?.Invoke();
            }

        }

        public void UpgradeCriticalDamageMultiplier(TurretStatsInstance turret)
        {
            float cost = GetExponentialCost(turret.CriticalDamageMultiplierUpgradeBaseCost, turret.CriticalDamageMultiplierLevel, turret.CriticalDamageCostExponentialMultiplier);

            if (TrySpend(cost))
            {
                turret.CriticalDamageMultiplierLevel++;
                turret.CriticalDamageMultiplier = turret.BaseCritDamage + turret.CriticalDamageMultiplierUpgradeAmount * turret.CriticalDamageMultiplierLevel;
                UpdateCriticalDamageMultiplierDisplay(turret);
                AudioManager.Instance.Play("Upgrade");
                turretUpgradeButton._baseTurret.UpdateTurretAppearance();
                OnAnyTurretUpgraded?.Invoke();
            }

        }

        public void UpgradeExplosionRadius(TurretStatsInstance turret)
        {
            if (turret.ExplosionRadius >= 5f)
            {
                UpdateExplosionRadiusDisplay(turret); // Update UI to show Max if needed
                return;
            }

            float cost = GetHybridCost(turret.ExplosionRadiusUpgradeBaseCost, turret.ExplosionRadiusLevel);
            if (TrySpend(cost))
            {
                turret.ExplosionRadius += turret.ExplosionRadiusUpgradeAmount;
                turret.ExplosionRadiusLevel++;
                UpdateExplosionRadiusDisplay(turret);
                AudioManager.Instance.Play("Upgrade");
                turretUpgradeButton._baseTurret.UpdateTurretAppearance();
                OnAnyTurretUpgraded?.Invoke();
            }

        }


        public void UpgradeSplashDamage(TurretStatsInstance turret)
        {
            float cost = GetHybridCost(turret.SplashDamageUpgradeBaseCost, turret.SplashDamageLevel);
            if (TrySpend(cost))
            {
                turret.SplashDamage += turret.SplashDamageUpgradeAmount;
                turret.SplashDamageLevel++;
                UpdateSplashDamageDisplay(turret);
                AudioManager.Instance.Play("Upgrade");
                turretUpgradeButton._baseTurret.UpdateTurretAppearance();
                OnAnyTurretUpgraded?.Invoke();
            }
        }

        public void UpgradePierceChance(TurretStatsInstance turret)
        {
            if (turret.PierceChance >= 100f)
                return;

            float cost = GetHybridCost(turret.PierceChanceUpgradeBaseCost, turret.PierceChanceLevel);
            if (TrySpend(cost))
            {
                turret.PierceChance = Mathf.Min(100f, turret.PierceChance + turret.PierceChanceUpgradeAmount);
                turret.PierceChanceLevel += 1;
                UpdatePierceChanceDisplay(turret);
                AudioManager.Instance.Play("Upgrade");
                turretUpgradeButton._baseTurret.UpdateTurretAppearance();
                OnAnyTurretUpgraded?.Invoke();
            }
        }


        public void UpgradePierceDamageFalloff(TurretStatsInstance turret)
        {
            float cost = GetHybridCost(turret.PierceDamageFalloffUpgradeBaseCost, turret.PierceDamageFalloffLevel);
            if (TrySpend(cost))
            {
                turret.PierceDamageFalloff -= turret.PierceDamageFalloffUpgradeAmount;
                turret.PierceDamageFalloffLevel++;
                UpdatePierceDamageFalloffDisplay(turret);
                AudioManager.Instance.Play("Upgrade");
                turretUpgradeButton._baseTurret.UpdateTurretAppearance();
                OnAnyTurretUpgraded?.Invoke();
            }
        }

        public void UpgradePelletCount(TurretStatsInstance turret)
        {
            float cost = GetHybridCost(turret.PelletCountUpgradeBaseCost, turret.PelletCountLevel);
            if (TrySpend(cost))
            {
                turret.PelletCount += turret.PelletCountUpgradeAmount;
                turret.PelletCountLevel += 1;
                UpdatePelletCountDisplay(turret);
                AudioManager.Instance.Play("Upgrade");
                turretUpgradeButton._baseTurret.UpdateTurretAppearance();
                OnAnyTurretUpgraded?.Invoke();
            }
        }

        public void UpgradeDamageFalloffOverDistance(TurretStatsInstance turret)
        {
            if (turret.DamageFalloffOverDistance <= 0f)
            {
                UpdateDamageFalloffOverDistanceDisplay(turret); // Still update UI if player clicks it
                return;
            }

            float cost = GetHybridCost(turret.DamageFalloffOverDistanceUpgradeBaseCost, turret.DamageFalloffOverDistanceLevel);
            if (TrySpend(cost))
            {
                turret.DamageFalloffOverDistance -= turret.DamageFalloffOverDistanceUpgradeAmount;
                turret.DamageFalloffOverDistance = Mathf.Max(0f, turret.DamageFalloffOverDistance); // Clamp to avoid negative values
                turret.DamageFalloffOverDistanceLevel++;
                UpdateDamageFalloffOverDistanceDisplay(turret);
                AudioManager.Instance.Play("Upgrade");
                turretUpgradeButton._baseTurret.UpdateTurretAppearance();
                OnAnyTurretUpgraded?.Invoke();
            }
        }

        public void UpgradeKnockbackStrength(TurretStatsInstance turret)
        {
            float cost = GetExponentialCost(
                turret.KnockbackStrengthUpgradeBaseCost,
                turret.KnockbackStrengthLevel,
                turret.KnockbackStrengthCostExponentialMultiplier
            );

            if (TrySpend(cost))
            {
                turret.KnockbackStrengthLevel++;
                turret.KnockbackStrength += turret.KnockbackStrengthUpgradeAmount;
                UpdateKnockbackStrengthDisplay(turret);
                AudioManager.Instance.Play("Upgrade");
                turretUpgradeButton._baseTurret.UpdateTurretAppearance();
                OnAnyTurretUpgraded?.Invoke();
            }
        }

        public void UpgradePercentBonusDamagePerSec(TurretStatsInstance turret)
        {
            float cost = GetHybridCost(turret.PercentBonusDamagePerSecUpgradeBaseCost, turret.PercentBonusDamagePerSecLevel);
            if (TrySpend(cost))
            {
                turret.PercentBonusDamagePerSec += turret.PercentBonusDamagePerSecUpgradeAmount;
                turret.PercentBonusDamagePerSecLevel++;
                UpdatePercentBonusDamagePerSecDisplay(turret);
                AudioManager.Instance.Play("Upgrade");
                turretUpgradeButton._baseTurret.UpdateTurretAppearance();
                OnAnyTurretUpgraded?.Invoke();
            }
        }

        public void UpgradeSlowEffect(TurretStatsInstance turret)
        {
            if (turret.SlowEffect >= 100f)
            {
                UpdateSlowEffectDisplay(turret); // Update UI to show Max
                return;
            }

            float cost = GetHybridCost(turret.SlowEffectUpgradeBaseCost, turret.SlowEffectLevel);
            if (TrySpend(cost))
            {
                turret.SlowEffect += turret.SlowEffectUpgradeAmount;
                turret.SlowEffect = Mathf.Min(turret.SlowEffect, 100f); // Clamp to 100%
                turret.SlowEffectLevel++;
                UpdateSlowEffectDisplay(turret);
                AudioManager.Instance.Play("Upgrade");
                turretUpgradeButton._baseTurret.UpdateTurretAppearance();
                OnAnyTurretUpgraded?.Invoke();
            }
        }

        // Update Display Methods

        public void UpdateDamageDisplay(TurretStatsInstance turret)
        {
            if (turret == null)
                return;

            float current = turret.Damage;

            // Predict next level damage without actually upgrading
            float nextLevel = turret.DamageLevel + 1f;
            float nextDamage = turret.BaseDamage * Mathf.Pow(turret.DamageUpgradeAmount, nextLevel) + nextLevel;

            float bonus = nextDamage - current;

            float cost = GetExponentialCost_PlusLevel(
                turret.DamageUpgradeBaseCost,
                turret.DamageLevel,
                turret.DamageCostExponentialMultiplier
            );

            turretUpgradeButton.UpdateStats(
                UIManager.AbbreviateNumber(current),
                $"+{UIManager.AbbreviateNumber(bonus)}",
                $"${UIManager.AbbreviateNumber(cost)}"
            );
        }


        public void UpdateFireRateDisplay(TurretStatsInstance turret)
        {
            if (turret == null)
                return;

            float currentAttackSpeed = turret.FireRate;
            float bonusSPS = turret.FireRateUpgradeAmount;
            float cost = GetExponentialCost_PlusLevel(turret.FireRateUpgradeBaseCost, turret.FireRateLevel, turret.FireRateCostExponentialMultiplier);

            turretUpgradeButton.UpdateStats(
                $"{currentAttackSpeed:F2}/s",
                $"+{bonusSPS:F2}/s",
                $"${UIManager.AbbreviateNumber(cost)}"
            );
        }

        public void UpdateCriticalChanceDisplay(TurretStatsInstance turret)
        {
            if (turret == null)
                return;

            float current = turret.CriticalChance;
            float bonus = turret.CriticalChanceUpgradeAmount;
            float cost = GetExponentialCost(
                turret.CriticalChanceUpgradeBaseCost,
                turret.CriticalChanceLevel,
                turret.CriticalChanceCostExponentialMultiplier
            );

            if (current >= 50f)
            {
                turretUpgradeButton.UpdateStats($"{(int)current}%", "Max", "");
            }
            else
            {
                turretUpgradeButton.UpdateStats(
                    $"{(int)current}%",
                    $"+{(int)bonus}%",
                    $"${UIManager.AbbreviateNumber(cost)}"
                );
            }
        }


        public void UpdateCriticalDamageMultiplierDisplay(TurretStatsInstance turret)
        {
            if (turret == null)
                return;
            float current = turret.CriticalDamageMultiplier;
            float bonus = turret.CriticalDamageMultiplierUpgradeAmount;
            float cost = GetExponentialCost(turret.CriticalDamageMultiplierUpgradeBaseCost, turret.CriticalDamageMultiplierLevel, turret.CriticalDamageCostExponentialMultiplier);
            turretUpgradeButton.UpdateStats(
                $"{(int)current}%",
                    $"+{(int)bonus}%",
                $"${UIManager.AbbreviateNumber(cost)}"
            );
        }

        public void UpdateExplosionRadiusDisplay(TurretStatsInstance turret)
        {
            if (turret == null)
                return;
            float current = turret.ExplosionRadius;
            float bonus = turret.ExplosionRadiusUpgradeAmount;
            float cost = GetHybridCost(turret.ExplosionRadiusUpgradeBaseCost, turret.ExplosionRadiusLevel);

            if (turret.ExplosionRadius >= 5f)
            {
                turretUpgradeButton.UpdateStats($"{current:F1}", "Max", "");
            }
            else
            {
                turretUpgradeButton.UpdateStats(
                    $"{current:F1}",
                    $"+{bonus:F1}",
                    $"${UIManager.AbbreviateNumber(cost)}"
                );
            }
        }

        public void UpdateSplashDamageDisplay(TurretStatsInstance turret)
        {
            if (turret == null)
                return;
            float current = turret.SplashDamage;
            float bonus = turret.SplashDamageUpgradeAmount;
            float cost = GetHybridCost(turret.SplashDamageUpgradeBaseCost, turret.SplashDamageLevel);
            turretUpgradeButton.UpdateStats(
                UIManager.AbbreviateNumber(current),
                $"+{UIManager.AbbreviateNumber(bonus)}",
                $"${UIManager.AbbreviateNumber(cost)}"
            );
        }

        public void UpdatePierceChanceDisplay(TurretStatsInstance turret)
        {
            if (turret == null)
                return;
            float current = turret.PierceChance;
            float bonus = turret.PierceChanceUpgradeAmount;
            float cost = GetHybridCost(turret.PierceChanceUpgradeBaseCost, turret.PierceChanceLevel);

            if (current >= 100f)
            {
                turretUpgradeButton.UpdateStats($"{current:F1}%", "Max", "");
            }
            else
            {
                turretUpgradeButton.UpdateStats(
                    $"{current:F1}%",
                    $"+{bonus:F1}%",
                    $"${UIManager.AbbreviateNumber(cost)}"
                );
            }
        }

        public void UpdatePierceDamageFalloffDisplay(TurretStatsInstance turret)
        {
            if (turret == null)
                return;

            float currentFalloff = turret.PierceDamageFalloff;
            float currentRetained = 100f - currentFalloff;

            // Predict next values
            float nextFalloff = Mathf.Max(0f, currentFalloff - turret.PierceDamageFalloffUpgradeAmount);
            float nextRetained = 100f - nextFalloff;

            float bonus = nextRetained - currentRetained;
            float cost = GetHybridCost(turret.PierceDamageFalloffUpgradeBaseCost, turret.PierceDamageFalloffLevel);

            turretUpgradeButton.UpdateStats(
                $"{currentRetained:F1}%",
                $"+{bonus:F1}%",
                $"${UIManager.AbbreviateNumber(cost)}"
            );
        }

        public void UpdatePelletCountDisplay(TurretStatsInstance turret)
        {
            if (turret == null)
                return;
            float current = turret.PelletCount;
            float bonus = turret.PelletCountUpgradeAmount;
            float cost = GetHybridCost(turret.PelletCountUpgradeBaseCost, turret.PelletCountLevel);

            turretUpgradeButton.UpdateStats(
                UIManager.AbbreviateNumber(current),
                $"+{UIManager.AbbreviateNumber(bonus)}",
                $"${UIManager.AbbreviateNumber(cost)}"
            );
        }

        public void UpdateKnockbackStrengthDisplay(TurretStatsInstance turret)
        {
            if (turret == null)
                return;

            float current = turret.KnockbackStrength;
            float bonus = turret.KnockbackStrengthUpgradeAmount;
            float cost = GetExponentialCost(
                turret.KnockbackStrengthUpgradeBaseCost,
                turret.KnockbackStrengthLevel,
                turret.KnockbackStrengthCostExponentialMultiplier
            );


            turretUpgradeButton.UpdateStats(
                $"{current:F1}",
                $"+{bonus:F1}",
                $"${UIManager.AbbreviateNumber(cost)}"
            );
        }

        public void UpdateDamageFalloffOverDistanceDisplay(TurretStatsInstance turret)
        {
            if (turret == null)
                return;
            float current = turret.DamageFalloffOverDistance;
            float bonus = turret.DamageFalloffOverDistanceUpgradeAmount;
            float cost = GetHybridCost(turret.DamageFalloffOverDistanceUpgradeBaseCost, turret.DamageFalloffOverDistanceLevel);

            if (current <= 0f)
            {
                turretUpgradeButton.UpdateStats($"{current:F1}%", "Max", "");
            }
            else
            {
                turretUpgradeButton.UpdateStats(
                    $"{current:F1}%",
                    $"-{bonus:F1}%",
                    $"${UIManager.AbbreviateNumber(cost)}"
                );
            }
        }

        public void UpdatePercentBonusDamagePerSecDisplay(TurretStatsInstance turret)
        {
            if (turret == null)
                return;
            float current = turret.PercentBonusDamagePerSec;
            float bonus = turret.PercentBonusDamagePerSecUpgradeAmount;
            float cost = GetHybridCost(turret.PercentBonusDamagePerSecUpgradeBaseCost, turret.PercentBonusDamagePerSecLevel);

            turretUpgradeButton.UpdateStats(
                $"{current:F1}%",
                $"+{bonus:F1}%",
                $"${UIManager.AbbreviateNumber(cost)}"
            );
        }

        public void UpdateSlowEffectDisplay(TurretStatsInstance turret)
        {
            if (turret == null)
                return;
            float current = turret.SlowEffect;
            float bonus = turret.SlowEffectUpgradeAmount;
            float cost = GetHybridCost(turret.SlowEffectUpgradeBaseCost, turret.SlowEffectLevel);

            if (current >= 100f)
            {
                turretUpgradeButton.UpdateStats($"{current:F1}%", "Max", "");
            }
            else
            {
                turretUpgradeButton.UpdateStats(
                    $"{current:F1}%",
                    $"+{bonus:F1}%",
                    $"${UIManager.AbbreviateNumber(cost)}"
                );
            }
        }

        // Used for enabling/disabling the buttons on the UI based on their costs
        public float GetDamageUpgradeCost(TurretStatsInstance t) =>
            GetExponentialCost_PlusLevel(t.DamageUpgradeBaseCost, t.DamageLevel, t.DamageCostExponentialMultiplier);

        public float GetFireRateUpgradeCost(TurretStatsInstance t) =>
            GetExponentialCost_PlusLevel(t.FireRateUpgradeBaseCost, t.FireRateLevel, t.FireRateCostExponentialMultiplier);

        public float GetCriticalChanceUpgradeCost(TurretStatsInstance t) =>
            GetExponentialCost(t.CriticalChanceUpgradeBaseCost, t.CriticalChanceLevel, t.CriticalChanceCostExponentialMultiplier);

        public float GetCriticalDamageMultiplierUpgradeCost(TurretStatsInstance t) =>
            GetExponentialCost(t.CriticalDamageMultiplierUpgradeBaseCost, t.CriticalDamageMultiplierLevel, t.CriticalDamageCostExponentialMultiplier);

        public float GetExplosionRadiusUpgradeCost(TurretStatsInstance t) =>
            GetHybridCost(t.ExplosionRadiusUpgradeBaseCost, t.ExplosionRadiusLevel);

        public float GetSplashDamageUpgradeCost(TurretStatsInstance t) =>
            GetHybridCost(t.SplashDamageUpgradeBaseCost, t.SplashDamageLevel);

        public float GetPierceChanceUpgradeCost(TurretStatsInstance t) =>
            GetHybridCost(t.PierceChanceUpgradeBaseCost, t.PierceChanceLevel);

        public float GetPierceDamageFalloffUpgradeCost(TurretStatsInstance t) =>
            GetHybridCost(t.PierceDamageFalloffUpgradeBaseCost, t.PierceDamageFalloffLevel);

        public float GetPelletCountUpgradeCost(TurretStatsInstance t) =>
            GetHybridCost(t.PelletCountUpgradeBaseCost, t.PelletCountLevel);

        public float GetDamageFalloffOverDistanceUpgradeCost(TurretStatsInstance t) =>
            GetHybridCost(t.DamageFalloffOverDistanceUpgradeBaseCost, t.DamageFalloffOverDistanceLevel);

        public float GetBonusDamagePerSecUpgradeCost(TurretStatsInstance t) =>
            GetHybridCost(t.PercentBonusDamagePerSecUpgradeBaseCost, t.PercentBonusDamagePerSecLevel);

        public float GetSlowEffectUpgradeCost(TurretStatsInstance t) =>
            GetHybridCost(t.SlowEffectUpgradeBaseCost, t.SlowEffectLevel);
    }


    public enum TurretUpgradeType
    {
        Damage,
        FireRate,
        CriticalChance,
        CriticalDamageMultiplier,
        ExplosionRadius,
        SplashDamage,
        PierceChance,
        PierceDamageFalloff,
        PelletCount,
        DamageFalloffOverDistance,
        PercentBonusDamagePerSec,
        SlowEffect,
        KnockbackStrength
    }
}