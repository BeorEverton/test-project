using Assets.Scripts.Systems;
using Assets.Scripts.Turrets;
using Assets.Scripts.UI;
using UnityEngine;

public class TurretUpgradeManager : MonoBehaviour
{
    [Header("Assigned at Runtime")]
    [SerializeField] private TurretStatsInstance turret;

    private TurretUpgradeButton turretUpgradeButton;

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

    public void UpgradeDamage()
    {
        float cost = GetHybridCost(turret.DamageUpgradeBaseCost, turret.DamageLevel);
        if (TrySpend(cost))
        {
            turret.Damage += turret.DamageUpgradeAmount;
            turret.DamageLevel += 1f;
            UpdateDamageDisplay();
        }
    }

    public void UpgradeFireRate()
    {
        if (turret.FireRate <= turret.FireRateUpgradeAmount) return;

        float cost = GetHybridCost(turret.FireRateUpgradeBaseCost, turret.FireRateLevel);
        if (TrySpend(cost))
        {
            turret.FireRate = Mathf.Max(turret.FireRateUpgradeAmount, turret.FireRate - turret.FireRateUpgradeAmount);
            turret.FireRateLevel += 1f;
            UpdateFireRateDisplay();
        }
    }

    public void UpgradeCriticalChance()
    {
        if (turret.CriticalChance >= 100f) return;

        float cost = GetHybridCost(turret.CriticalChanceUpgradeBaseCost, turret.CriticalChanceLevel);
        if (TrySpend(cost))
        {
            turret.CriticalChance = Mathf.Min(100f, turret.CriticalChance + turret.CriticalChanceUpgradeAmount);
            turret.CriticalChanceLevel += 1f;
            UpdateCriticalChanceDisplay();
        }
    }

    public void UpgradeCriticalDamageMultiplier()
    {
        float cost = GetHybridCost(turret.CriticalDamageMultiplierUpgradeBaseCost, turret.CriticalDamageMultiplierLevel);
        if (TrySpend(cost))
        {
            turret.CriticalDamageMultiplier += turret.CriticalDamageMultiplierUpgradeAmount;
            turret.CriticalDamageMultiplierLevel += 1f;
            UpdateCriticalDamageMultiplierDisplay();
        }
    }

    public void UpgradeExplosionRadius()
    {
        float cost = GetHybridCost(turret.ExplosionRadiusUpgradeBaseCost, turret.ExplosionRadiusLevel);
        if (TrySpend(cost))
        {
            turret.ExplosionRadius += turret.ExplosionRadiusUpgradeAmount;
            turret.ExplosionRadiusLevel += 1f;
            UpdateExplosionRadiusDisplay();
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
        }
    }

    public void UpgradePierceCount()
    {
        float cost = GetHybridCost(turret.PierceCountUpgradeBaseCost, turret.PierceCountLevel);
        if (TrySpend(cost))
        {
            turret.PierceCount += turret.PierceCountUpgradeAmount;
            turret.PierceCountLevel += 1;
            UpdatePierceCountDisplay();
        }
    }

    public void UpgradePierceDamageFalloff()
    {
        float cost = GetHybridCost(turret.PierceDamageFalloffUpgradeBaseCost, turret.PierceDamageFalloffLevel);
        if (TrySpend(cost))
        {
            turret.PierceDamageFalloff += turret.PierceDamageFalloffUpgradeAmount;
            turret.PierceDamageFalloffLevel += 1f;
            UpdatePierceDamageFalloffDisplay();
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
        }
    }

    public void UpgradeDamageFalloffOverDistance()
    {
        float cost = GetHybridCost(turret.DamageFalloffOverDistanceUpgradeBaseCost, turret.DamageFalloffOverDistanceLevel);
        if (TrySpend(cost))
        {
            turret.DamageFalloffOverDistance += turret.DamageFalloffOverDistanceUpgradeAmount;
            turret.DamageFalloffOverDistanceLevel += 1f;
            UpdateDamageFalloffOverDistanceDisplay();
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
        }
    }

    public void UpgradeSlowEffect()
    {
        float cost = GetHybridCost(turret.SlowEffectUpgradeBaseCost, turret.SlowEffectLevel);
        if (TrySpend(cost))
        {
            turret.SlowEffect += turret.SlowEffectUpgradeAmount;
            turret.SlowEffectLevel += 1f;
            UpdateSlowEffectDisplay();
        }
    }

    // Update Display Methods

    public void UpdateDamageDisplay()
    {
        if (turret == null) return;
        var current = turret.Damage;
        var bonus = turret.DamageUpgradeAmount;
        var cost = GetHybridCost(turret.DamageUpgradeBaseCost, turret.DamageLevel);
        turretUpgradeButton.UpdateStats(UIManager.AbbreviateNumber(current), $"+{UIManager.AbbreviateNumber(bonus)} ${UIManager.AbbreviateNumber(cost)}");
    }

    public void UpdateFireRateDisplay()
    {
        if (turret == null) return;
        float currentDelay = turret.FireRate;
        float bonusSPS = turret.FireRateUpgradeAmount;
        float cost = GetHybridCost(turret.FireRateUpgradeBaseCost, turret.FireRateLevel);
        string currentDisplay = FormatFireRate(1f / currentDelay);
        string bonusDisplay = turret.FireRate <= turret.FireRateUpgradeAmount ? "Max" : $"+{bonusSPS:F2}/s ${UIManager.AbbreviateNumber(cost)}";
        turretUpgradeButton.UpdateStats(currentDisplay, bonusDisplay);
    }

    private string FormatFireRate(float shotsPerSecond)
    {
        return shotsPerSecond >= 1f ? $"{shotsPerSecond:F2}/s" : $"1/{(1f / shotsPerSecond):F2}s";
    }

    public void UpdateCriticalChanceDisplay()
    {
        if (turret == null) return;
        float current = turret.CriticalChance;
        float bonus = turret.CriticalChanceUpgradeAmount;
        float cost = GetHybridCost(turret.CriticalChanceUpgradeBaseCost, turret.CriticalChanceLevel);
        string bonusText = current >= 100f ? "Max" : $"+{bonus}% ${UIManager.AbbreviateNumber(cost)}";
        turretUpgradeButton.UpdateStats($"{current}%", bonusText);
    }

    public void UpdateCriticalDamageMultiplierDisplay()
    {
        if (turret == null) return;
        float current = 1 + turret.CriticalDamageMultiplier; // so it shows 120% instead of 20%
        float bonus = turret.CriticalDamageMultiplierUpgradeAmount;
        float cost = GetHybridCost(turret.CriticalDamageMultiplierUpgradeBaseCost, turret.CriticalDamageMultiplierLevel);
        turretUpgradeButton.UpdateStats($"{current}%", $"+{bonus}% ${UIManager.AbbreviateNumber(cost)}");
    }

    public void UpdateExplosionRadiusDisplay()
    {
        if (turret == null) return;
        var current = turret.ExplosionRadius;
        var bonus = turret.ExplosionRadiusUpgradeAmount;
        var cost = GetHybridCost(turret.ExplosionRadiusUpgradeBaseCost, turret.ExplosionRadiusLevel);
        turretUpgradeButton.UpdateStats(UIManager.AbbreviateNumber(current), $"+{UIManager.AbbreviateNumber(bonus)} ${UIManager.AbbreviateNumber(cost)}");
    }

    public void UpdateSplashDamageDisplay()
    {
        if (turret == null) return;
        var current = turret.SplashDamage;
        var bonus = turret.SplashDamageUpgradeAmount;
        var cost = GetHybridCost(turret.SplashDamageUpgradeBaseCost, turret.SplashDamageLevel);
        turretUpgradeButton.UpdateStats(UIManager.AbbreviateNumber(current), $"+{UIManager.AbbreviateNumber(bonus)} ${UIManager.AbbreviateNumber(cost)}");
    }

    public void UpdatePierceCountDisplay()
    {
        if (turret == null) return;
        var current = turret.PierceCount;
        var bonus = turret.PierceCountUpgradeAmount;
        var cost = GetHybridCost(turret.PierceCountUpgradeBaseCost, turret.PierceCountLevel);
        turretUpgradeButton.UpdateStats(UIManager.AbbreviateNumber(current), $"+{UIManager.AbbreviateNumber(bonus)} ${UIManager.AbbreviateNumber(cost)}");
    }

    public void UpdatePierceDamageFalloffDisplay()
    {
        if (turret == null) return;
        var current = turret.PierceDamageFalloff;
        var bonus = turret.PierceDamageFalloffUpgradeAmount;
        var cost = GetHybridCost(turret.PierceDamageFalloffUpgradeBaseCost, turret.PierceDamageFalloffLevel);
        turretUpgradeButton.UpdateStats(UIManager.AbbreviateNumber(current), $"+{UIManager.AbbreviateNumber(bonus)} ${UIManager.AbbreviateNumber(cost)}");
    }

    public void UpdatePelletCountDisplay()
    {
        if (turret == null) return;
        var current = turret.PelletCount;
        var bonus = turret.PelletCountUpgradeAmount;
        var cost = GetHybridCost(turret.PelletCountUpgradeBaseCost, turret.PelletCountLevel);
        turretUpgradeButton.UpdateStats(UIManager.AbbreviateNumber(current), $"+{UIManager.AbbreviateNumber(bonus)} ${UIManager.AbbreviateNumber(cost)}");
    }

    public void UpdateDamageFalloffOverDistanceDisplay()
    {
        if (turret == null) return;
        var current = turret.DamageFalloffOverDistance;
        var bonus = turret.DamageFalloffOverDistanceUpgradeAmount;
        var cost = GetHybridCost(turret.DamageFalloffOverDistanceUpgradeBaseCost, turret.DamageFalloffOverDistanceLevel);
        turretUpgradeButton.UpdateStats(UIManager.AbbreviateNumber(current), $"+{UIManager.AbbreviateNumber(bonus)} ${UIManager.AbbreviateNumber(cost)}");
    }

    public void UpdatePercentBonusDamagePerSecDisplay()
    {
        if (turret == null) return;
        var current = turret.PercentBonusDamagePerSec;
        var bonus = turret.PercentBonusDamagePerSecUpgradeAmount;
        var cost = GetHybridCost(turret.PercentBonusDamagePerSecUpgradeBaseCost, turret.PercentBonusDamagePerSecLevel);
        turretUpgradeButton.UpdateStats(UIManager.AbbreviateNumber(current), $"+{UIManager.AbbreviateNumber(bonus)} ${UIManager.AbbreviateNumber(cost)}");
    }

    public void UpdateSlowEffectDisplay()
    {
        if (turret == null) return;
        var current = turret.SlowEffect;
        var bonus = turret.SlowEffectUpgradeAmount;
        var cost = GetHybridCost(turret.SlowEffectUpgradeBaseCost, turret.SlowEffectLevel);
        turretUpgradeButton.UpdateStats(UIManager.AbbreviateNumber(current), $"+{UIManager.AbbreviateNumber(bonus)} ${UIManager.AbbreviateNumber(cost)}");
    }
}


public enum TurretUpgradeType
{
    Damage,
    FireRate,
    CriticalChance,
    CriticalDamageMultiplier,
    ExplosionRadius,
    SplashDamage,
    PierceCount,
    PierceDamageFalloff,
    PelletCount,
    DamageFalloffOverDistance,
    PercentBonusDamagePerSec,
    SlowEffect
}
