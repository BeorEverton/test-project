using Assets.Scripts.Systems.Audio;
using Assets.Scripts.Turrets;
using Assets.Scripts.UI;
using System;
using UnityEngine;

namespace Assets.Scripts.Systems
{
    public class TurretUpgradeManager : MonoBehaviour
    {
        [Header("Assigned at Runtime")]
        [SerializeField] private TurretStatsInstance turret;

        private TurretUpgradeButton turretUpgradeButton;
        public static event Action OnAnyTurretUpgraded;


        [Header("Cost Scaling Settings")]
        [SerializeField] private int hybridThreshold = 50;
        [SerializeField] private float quadraticFactor = 0.1f;
        [SerializeField] private float exponentialPower = 1.15f;

        public void SetTurret(TurretStatsInstance turret, TurretUpgradeButton turretUpgrade)
        {
            this.turret = turret;
            turretUpgradeButton = turretUpgrade;
        }

        private bool TrySpend(float cost)
        {
            if (GameManager.Instance.Money >= cost)
            {
                GameManager.Instance.SpendMoney((ulong)cost);
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


        public void UpgradeDamage()
        {
            float cost = GetExponentialCost_PlusLevel(turret.DamageUpgradeBaseCost, turret.DamageLevel, turret.DamageCostExponentialMultiplier);

            if (TrySpend(cost))
            {
                turret.DamageLevel += 1f;
                turret.Damage = turret.BaseDamage * Mathf.Pow(turret.DamageUpgradeAmount, turret.DamageLevel) + turret.DamageLevel;
                UpdateDamageDisplay(); 
                AudioManager.Instance.Play("Upgrade");  
                turretUpgradeButton._baseTurret.UpdateTurretAppearance();
                OnAnyTurretUpgraded?.Invoke();

            }

        }

        public void UpgradeFireRate()
        {
            float cost = GetExponentialCost_PlusLevel(turret.FireRateUpgradeBaseCost, turret.FireRateLevel, turret.FireRateCostExponentialMultiplier);

            if (TrySpend(cost))
            {
                turret.FireRateLevel += 1f;
                turret.FireRate = turret.BaseFireRate + turret.FireRateUpgradeAmount * turret.FireRateLevel;
                UpdateFireRateDisplay();
                AudioManager.Instance.Play("Upgrade");  
                turretUpgradeButton._baseTurret.UpdateTurretAppearance();
                OnAnyTurretUpgraded?.Invoke();

            }
        }

        public void UpgradeCriticalChance()
        {
            if (turret.CriticalChance >= 50f)
                return;

            float cost = GetExponentialCost(turret.CriticalChanceUpgradeBaseCost, turret.CriticalChanceLevel, turret.CriticalChanceCostExponentialMultiplier);

            if (TrySpend(cost))
            {
                turret.CriticalChanceLevel += 1f;
                turret.CriticalChance = Mathf.Min(50f, turret.BaseCritChance + turret.CriticalChanceUpgradeAmount * turret.CriticalChanceLevel);
                UpdateCriticalChanceDisplay();
                AudioManager.Instance.Play("Upgrade");  
                turretUpgradeButton._baseTurret.UpdateTurretAppearance();
                OnAnyTurretUpgraded?.Invoke();

            }

        }

        public void UpgradeCriticalDamageMultiplier()
        {
            float cost = GetExponentialCost(turret.CriticalDamageMultiplierUpgradeBaseCost, turret.CriticalDamageMultiplierLevel, turret.CriticalDamageCostExponentialMultiplier);

            if (TrySpend(cost))
            {
                turret.CriticalDamageMultiplierLevel += 1f;
                turret.CriticalDamageMultiplier = turret.BaseCritDamage + turret.CriticalDamageMultiplierUpgradeAmount * turret.CriticalDamageMultiplierLevel;
                UpdateCriticalDamageMultiplierDisplay();
                AudioManager.Instance.Play("Upgrade"); 
                turretUpgradeButton._baseTurret.UpdateTurretAppearance();
                OnAnyTurretUpgraded?.Invoke();

            }

        }

        public void UpgradeExplosionRadius()
        {
            if (turret.ExplosionRadius >= 5f)
            {
                UpdateExplosionRadiusDisplay(); // Update UI to show Max if needed
                return;
            }

            float cost = GetHybridCost(turret.ExplosionRadiusUpgradeBaseCost, turret.ExplosionRadiusLevel);
            if (TrySpend(cost))
            {
                turret.ExplosionRadius += turret.ExplosionRadiusUpgradeAmount;
                turret.ExplosionRadiusLevel += 1f;
                UpdateExplosionRadiusDisplay();
                AudioManager.Instance.Play("Upgrade");  
                turretUpgradeButton._baseTurret.UpdateTurretAppearance();
            }
                
        }


        public void UpgradeSplashDamage()
        {
            float cost = GetHybridCost(turret.SplashDamageUpgradeBaseCost, turret.SplashDamageLevel);
            if (TrySpend(cost))
            {
                turret.SplashDamage += turret.SplashDamageUpgradeAmount;
                turret.SplashDamageLevel += 1f;
                UpdateSplashDamageDisplay();
                AudioManager.Instance.Play("Upgrade"); 
                turretUpgradeButton._baseTurret.UpdateTurretAppearance();
            }
        }

        public void UpgradePierceChance()
        {
            if (turret.PierceChance >= 100f)
                return;

            float cost = GetHybridCost(turret.PierceChanceUpgradeBaseCost, turret.PierceChanceLevel);
            if (TrySpend(cost))
            {
                turret.PierceChance = Mathf.Min(100f, turret.PierceChance + turret.PierceChanceUpgradeAmount);
                turret.PierceChanceLevel += 1;
                UpdatePierceChanceDisplay();
                AudioManager.Instance.Play("Upgrade");  
                turretUpgradeButton._baseTurret.UpdateTurretAppearance();
            }
        }


        public void UpgradePierceDamageFalloff()
        {
            float cost = GetHybridCost(turret.PierceDamageFalloffUpgradeBaseCost, turret.PierceDamageFalloffLevel);
            if (TrySpend(cost))
            {
                turret.PierceDamageFalloff -= turret.PierceDamageFalloffUpgradeAmount;
                turret.PierceDamageFalloffLevel += 1f;
                UpdatePierceDamageFalloffDisplay();
                AudioManager.Instance.Play("Upgrade"); 
                turretUpgradeButton._baseTurret.UpdateTurretAppearance();
            }
        }

        public void UpgradePelletCount()
        {
            float cost = GetHybridCost(turret.PelletCountUpgradeBaseCost, turret.PelletCountLevel);
            if (TrySpend(cost))
            {
                turret.PelletCount += turret.PelletCountUpgradeAmount;
                turret.PelletCountLevel += 1;
                UpdatePelletCountDisplay();
                AudioManager.Instance.Play("Upgrade");  turretUpgradeButton._baseTurret.UpdateTurretAppearance();
            }
        }

        public void UpgradeDamageFalloffOverDistance()
        {
            if (turret.DamageFalloffOverDistance <= 0f)
            {
                UpdateDamageFalloffOverDistanceDisplay(); // Still update UI if player clicks it
                return;
            }

            float cost = GetHybridCost(turret.DamageFalloffOverDistanceUpgradeBaseCost, turret.DamageFalloffOverDistanceLevel);
            if (TrySpend(cost))
            {
                turret.DamageFalloffOverDistance -= turret.DamageFalloffOverDistanceUpgradeAmount;
                turret.DamageFalloffOverDistance = Mathf.Max(0f, turret.DamageFalloffOverDistance); // Clamp to avoid negative values
                turret.DamageFalloffOverDistanceLevel += 1f;
                UpdateDamageFalloffOverDistanceDisplay();
                AudioManager.Instance.Play("Upgrade"); 
                turretUpgradeButton._baseTurret.UpdateTurretAppearance();
            }
        }

        public void UpgradeKnockbackStrength()
        {
            float cost = GetExponentialCost(
                turret.KnockbackStrengthUpgradeBaseCost,
                turret.KnockbackStrengthLevel,
                turret.KnockbackStrengthCostExponentialMultiplier
            );

            if (TrySpend(cost))
            {
                turret.KnockbackStrengthLevel += 1f;
                turret.KnockbackStrength += turret.KnockbackStrengthUpgradeAmount;
                UpdateKnockbackStrengthDisplay();
                AudioManager.Instance.Play("Upgrade");
                turretUpgradeButton._baseTurret.UpdateTurretAppearance();
            }
        }

        public void UpgradePercentBonusDamagePerSec()
        {
            float cost = GetHybridCost(turret.PercentBonusDamagePerSecUpgradeBaseCost, turret.PercentBonusDamagePerSecLevel);
            if (TrySpend(cost))
            {
                turret.PercentBonusDamagePerSec += turret.PercentBonusDamagePerSecUpgradeAmount;
                turret.PercentBonusDamagePerSecLevel += 1f;
                UpdatePercentBonusDamagePerSecDisplay();
                AudioManager.Instance.Play("Upgrade");  
                turretUpgradeButton._baseTurret.UpdateTurretAppearance();
            } 
        }

        public void UpgradeSlowEffect()
        {
            if (turret.SlowEffect >= 100f)
            {
                UpdateSlowEffectDisplay(); // Update UI to show Max
                return;
            }

            float cost = GetHybridCost(turret.SlowEffectUpgradeBaseCost, turret.SlowEffectLevel);
            if (TrySpend(cost))
            {
                turret.SlowEffect += turret.SlowEffectUpgradeAmount;
                turret.SlowEffect = Mathf.Min(turret.SlowEffect, 100f); // Clamp to 100%
                turret.SlowEffectLevel += 1f;
                UpdateSlowEffectDisplay();
                AudioManager.Instance.Play("Upgrade");  
                turretUpgradeButton._baseTurret.UpdateTurretAppearance();
            }                
        }

        // Update Display Methods

        public void UpdateDamageDisplay()
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


        public void UpdateFireRateDisplay()
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

        public void UpdateCriticalChanceDisplay()
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


        public void UpdateCriticalDamageMultiplierDisplay()
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

        public void UpdateExplosionRadiusDisplay()
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

        public void UpdateSplashDamageDisplay()
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

        public void UpdatePierceChanceDisplay()
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

        public void UpdatePierceDamageFalloffDisplay()
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

        public void UpdatePelletCountDisplay()
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

        public void UpdateKnockbackStrengthDisplay()
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

        public void UpdateDamageFalloffOverDistanceDisplay()
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

        public void UpdatePercentBonusDamagePerSecDisplay()
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

        public void UpdateSlowEffectDisplay()
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