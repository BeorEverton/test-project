using Assets.Scripts.Systems;
using Assets.Scripts.UI;
using UnityEngine;

public class TurretUpgradeManager : MonoBehaviour
{
    [Header("Assigned at Runtime")]
    [SerializeField] private TurretStatsInstance turret;

    [Header("Upgrade Amounts")]
    public float DamageUpgrade = 1f;
    public float FireRateUpgrade = 1f;
    public float CriticalChanceUpgrade = 1f;
    public float CriticalDamageMultiplierUpgrade = 1f;
    public float ExplosionRadiusUpgrade = 1f;
    public float SplashDamageUpgrade = 1f;
    public int PierceCountUpgrade = 1;
    public float PierceDamageFalloffUpgrade = 1f;
    public int PelletCountUpgrade = 1;
    public float DamageFalloffOverDistanceUpgrade = 1f;
    public float PercentBonusDamagePerSecUpgrade = 1f;
    public float SlowEffectUpgrade = 1f;

    private TurretUpgradeButton turretUpgradeButton;

    public void SetTurret(TurretStatsInstance turret, TurretUpgradeButton turretUpgrade)
    {
        this.turret = turret;
        turretUpgradeButton = turretUpgrade;
    }

    private bool TrySpend(float cost)
    {
        if (GameManager.Instance.GetMoney() >= cost)
        {
            GameManager.Instance.RemoveMoney((ulong)cost);
            return true;
        }

        Debug.Log("Not enough money.");
        return false;
    }

    public void UpgradeDamage()
    {
        int cost = turret.GetUpgradeCost(nameof(turret.DamageLevel));
        if (TrySpend(cost))
        {
            turret.Damage += DamageUpgrade;
            turret.DamageLevel += 1f;
            UpdateDamageDisplay();
        }
    }

    public void UpgradeFireRate()
    {
        int cost = turret.GetUpgradeCost(nameof(turret.FireRateLevel));
        if (TrySpend(cost))
        {
            turret.FireRate += FireRateUpgrade;
            turret.FireRateLevel += 1f;
            UpdateFireRateDisplay();
        }
    }

    public void UpgradeCriticalChance()
    {
        int cost = turret.GetUpgradeCost(nameof(turret.CriticalChanceLevel));
        if (TrySpend(cost))
        {
            turret.CriticalChance += CriticalChanceUpgrade;
            turret.CriticalChanceLevel += 1f;
            UpdateCriticalChanceDisplay();
        }
    }

    public void UpgradeCriticalDamageMultiplier()
    {
        int cost = turret.GetUpgradeCost(nameof(turret.CriticalDamageMultiplierLevel));
        if (TrySpend(cost))
        {
            turret.CriticalDamageMultiplier += CriticalDamageMultiplierUpgrade;
            turret.CriticalDamageMultiplierLevel += 1f;
            UpdateCriticalDamageMultiplierDisplay();
        }
    }

    public void UpgradeExplosionRadius()
    {
        int cost = turret.GetUpgradeCost(nameof(turret.ExplosionRadiusLevel));
        if (TrySpend(cost))
        {
            turret.ExplosionRadius += ExplosionRadiusUpgrade;
            turret.ExplosionRadiusLevel += 1f;
            UpdateExplosionRadiusDisplay();
        }
    }

    public void UpgradeSplashDamage()
    {
        int cost = turret.GetUpgradeCost(nameof(turret.SplashDamageLevel));
        if (TrySpend(cost))
        {
            turret.SplashDamage += SplashDamageUpgrade;
            turret.SplashDamageLevel += 1f;
            UpdateSplashDamageDisplay();
        }
    }

    public void UpgradePierceCount()
    {
        int cost = turret.GetUpgradeCost(nameof(turret.PierceCountLevel));
        if (TrySpend(cost))
        {
            turret.PierceCount += PierceCountUpgrade;
            turret.PierceCountLevel += 1;
            UpdatePierceCountDisplay();
        }
    }

    public void UpgradePierceDamageFalloff()
    {
        int cost = turret.GetUpgradeCost(nameof(turret.PierceDamageFalloffLevel));
        if (TrySpend(cost))
        {
            turret.PierceDamageFalloff += PierceDamageFalloffUpgrade;
            turret.PierceDamageFalloffLevel += 1f;
            UpdatePierceDamageFalloffDisplay();
        }
    }

    public void UpgradePelletCount()
    {
        int cost = turret.GetUpgradeCost(nameof(turret.PelletCountLevel));
        if (TrySpend(cost))
        {
            turret.PelletCount += PelletCountUpgrade;
            turret.PelletCountLevel += 1;
            UpdatePelletCountDisplay();
        }
    }

    public void UpgradeDamageFalloffOverDistance()
    {
        int cost = turret.GetUpgradeCost(nameof(turret.DamageFalloffOverDistanceLevel));
        if (TrySpend(cost))
        {
            turret.DamageFalloffOverDistance += DamageFalloffOverDistanceUpgrade;
            turret.DamageFalloffOverDistanceLevel += 1f;
            UpdateDamageFalloffOverDistanceDisplay();
        }
    }

    public void UpgradePercentBonusDamagePerSec()
    {
        int cost = turret.GetUpgradeCost(nameof(turret.PercentBonusDamagePerSecLevel));
        if (TrySpend(cost))
        {
            turret.PercentBonusDamagePerSec += PercentBonusDamagePerSecUpgrade;
            turret.PercentBonusDamagePerSecLevel += 1f;
            UpdatePercentBonusDamagePerSecDisplay();
        }
    }

    public void UpgradeSlowEffect()
    {
        int cost = turret.GetUpgradeCost(nameof(turret.SlowEffectLevel));
        if (TrySpend(cost))
        {
            turret.SlowEffect += SlowEffectUpgrade;
            turret.SlowEffectLevel += 1f;
            UpdateSlowEffectDisplay();
        }
    }

    #region Update Upgrade Button UI

    public void UpdateDamageDisplay()
    {
        var current = turret.Damage;
        var bonus = DamageUpgrade;
        var cost = turret.GetUpgradeCost(nameof(turret.DamageLevel));

        turretUpgradeButton.UpdateStats(UIManager.AbbreviateNumber(current),
            $"+{UIManager.AbbreviateNumber(bonus)} ${UIManager.AbbreviateNumber(cost)}");
    }

    public void UpdateFireRateDisplay()
    {
        var current = turret.FireRate;
        var bonus = FireRateUpgrade;
        var cost = turret.GetUpgradeCost(nameof(turret.FireRateLevel));

        turretUpgradeButton.UpdateStats(UIManager.AbbreviateNumber(current),
            $"+{UIManager.AbbreviateNumber(bonus)} ${UIManager.AbbreviateNumber(cost)}");
    }

    public void UpdateCriticalChanceDisplay()
    {
        var current = turret.CriticalChance;
        var bonus = CriticalChanceUpgrade;
        var cost = turret.GetUpgradeCost(nameof(turret.CriticalChanceLevel));

        turretUpgradeButton.UpdateStats(UIManager.AbbreviateNumber(current),
            $"+{UIManager.AbbreviateNumber(bonus)} ${UIManager.AbbreviateNumber(cost)}");
    }

    public void UpdateCriticalDamageMultiplierDisplay()
    {
        var current = turret.CriticalDamageMultiplier;
        var bonus = CriticalDamageMultiplierUpgrade;
        var cost = turret.GetUpgradeCost(nameof(turret.CriticalDamageMultiplierLevel));

        turretUpgradeButton.UpdateStats(UIManager.AbbreviateNumber(current),
            $"+{UIManager.AbbreviateNumber(bonus)} ${UIManager.AbbreviateNumber(cost)}");
    }

    public void UpdateExplosionRadiusDisplay()
    {
        var current = turret.ExplosionRadius;
        var bonus = ExplosionRadiusUpgrade;
        var cost = turret.GetUpgradeCost(nameof(turret.ExplosionRadiusLevel));

        turretUpgradeButton.UpdateStats(UIManager.AbbreviateNumber(current),
            $"+{UIManager.AbbreviateNumber(bonus)} ${UIManager.AbbreviateNumber(cost)}");
    }

    public void UpdateSplashDamageDisplay()
    {
        var current = turret.SplashDamage;
        var bonus = SplashDamageUpgrade;
        var cost = turret.GetUpgradeCost(nameof(turret.SplashDamageLevel));

        turretUpgradeButton.UpdateStats(UIManager.AbbreviateNumber(current),
            $"+{UIManager.AbbreviateNumber(bonus)} ${UIManager.AbbreviateNumber(cost)}");
    }

    public void UpdatePierceCountDisplay()
    {
        var current = turret.PierceCount;
        var bonus = PierceCountUpgrade;
        var cost = turret.GetUpgradeCost(nameof(turret.PierceCountLevel));

        turretUpgradeButton.UpdateStats(UIManager.AbbreviateNumber(current),
            $"+{UIManager.AbbreviateNumber(bonus)} ${UIManager.AbbreviateNumber(cost)}");
    }

    public void UpdatePierceDamageFalloffDisplay()
    {
        var current = turret.PierceDamageFalloff;
        var bonus = PierceDamageFalloffUpgrade;
        var cost = turret.GetUpgradeCost(nameof(turret.PierceDamageFalloffLevel));

        turretUpgradeButton.UpdateStats(UIManager.AbbreviateNumber(current),
            $"+{UIManager.AbbreviateNumber(bonus)} ${UIManager.AbbreviateNumber(cost)}");
    }

    public void UpdatePelletCountDisplay()
    {
        var current = turret.PelletCount;
        var bonus = PelletCountUpgrade;
        var cost = turret.GetUpgradeCost(nameof(turret.PelletCountLevel));

        turretUpgradeButton.UpdateStats(UIManager.AbbreviateNumber(current),
            $"+{UIManager.AbbreviateNumber(bonus)} ${UIManager.AbbreviateNumber(cost)}");
    }

    public void UpdateDamageFalloffOverDistanceDisplay()
    {
        var current = turret.DamageFalloffOverDistance;
        var bonus = DamageFalloffOverDistanceUpgrade;
        var cost = turret.GetUpgradeCost(nameof(turret.DamageFalloffOverDistanceLevel));

        turretUpgradeButton.UpdateStats(UIManager.AbbreviateNumber(current),
            $"+{UIManager.AbbreviateNumber(bonus)} ${UIManager.AbbreviateNumber(cost)}");
    }

    public void UpdatePercentBonusDamagePerSecDisplay()
    {
        var current = turret.PercentBonusDamagePerSec;
        var bonus = PercentBonusDamagePerSecUpgrade;
        var cost = turret.GetUpgradeCost(nameof(turret.PercentBonusDamagePerSecLevel));

        turretUpgradeButton.UpdateStats(UIManager.AbbreviateNumber(current),
            $"+{UIManager.AbbreviateNumber(bonus)} ${UIManager.AbbreviateNumber(cost)}");
    }

    public void UpdateSlowEffectDisplay()
    {
        var current = turret.SlowEffect;
        var bonus = SlowEffectUpgrade;
        var cost = turret.GetUpgradeCost(nameof(turret.SlowEffectLevel));

        turretUpgradeButton.UpdateStats(UIManager.AbbreviateNumber(current),
            $"+{UIManager.AbbreviateNumber(bonus)} ${UIManager.AbbreviateNumber(cost)}");
    }

    #endregion

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
