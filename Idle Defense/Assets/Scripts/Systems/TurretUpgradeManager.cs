using Assets.Scripts.SO;
using Assets.Scripts.Systems;
using UnityEngine;

public class TurretUpgradeManager : MonoBehaviour
{
    [SerializeField] private TurretStatsInstance turret;

    public void SetTurret(TurretStatsInstance turret)
    {
        this.turret = turret;
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
        float cost = Mathf.Pow(2, turret.DamageLevel);
        if (TrySpend(cost))
        {
            turret.Damage += 1f;
            turret.DamageLevel += 1f;
        }
    }

    public void UpgradeFireRate()
    {
        float cost = Mathf.Pow(2, turret.FireRateLevel);
        if (TrySpend(cost))
        {
            turret.FireRate += 1f;
            turret.FireRateLevel += 1f;
        }
    }

    public void UpgradeCriticalChance()
    {
        float cost = Mathf.Pow(2, turret.CriticalChanceLevel);
        if (TrySpend(cost))
        {
            turret.CriticalChance += 1f;
            turret.CriticalChanceLevel += 1f;
        }
    }

    public void UpgradeCriticalDamageMultiplier()
    {
        float cost = Mathf.Pow(2, turret.CriticalDamageMultiplierLevel);
        if (TrySpend(cost))
        {
            turret.CriticalDamageMultiplier += 1f;
            turret.CriticalDamageMultiplierLevel += 1f;
        }
    }

    public void UpgradeExplosionRadius()
    {
        float cost = Mathf.Pow(2, turret.ExplosionRadiusLevel);
        if (TrySpend(cost))
        {
            turret.ExplosionRadius += 1f;
            turret.ExplosionRadiusLevel += 1f;
        }
    }

    public void UpgradeSplashDamage()
    {
        float cost = Mathf.Pow(2, turret.SplashDamageLevel);
        if (TrySpend(cost))
        {
            turret.SplashDamage += 1f;
            turret.SplashDamageLevel += 1f;
        }
    }

    public void UpgradePierceCount()
    {
        float cost = Mathf.Pow(2, turret.PierceCountLevel);
        if (TrySpend(cost))
        {
            turret.PierceCount += 1;
            turret.PierceCountLevel += 1;
        }
    }

    public void UpgradePierceDamageFalloff()
    {
        float cost = Mathf.Pow(2, turret.PierceDamageFalloffLevel);
        if (TrySpend(cost))
        {
            turret.PierceDamageFalloff += 1f;
            turret.PierceDamageFalloffLevel += 1f;
        }
    }

    public void UpgradePelletCount()
    {
        float cost = Mathf.Pow(2, turret.PelletCountLevel);
        if (TrySpend(cost))
        {
            turret.PelletCount += 1;
            turret.PelletCountLevel += 1;
        }
    }

    public void UpgradeDamageFalloffOverDistance()
    {
        float cost = Mathf.Pow(2, turret.DamageFalloffOverDistanceLevel);
        if (TrySpend(cost))
        {
            turret.DamageFalloffOverDistance += 1f;
            turret.DamageFalloffOverDistanceLevel += 1f;
        }
    }

    public void UpgradeProcentBonusDamagePerSec()
    {
        float cost = Mathf.Pow(2, turret.ProcentBonusDamagePerSecLevel);
        if (TrySpend(cost))
        {
            turret.ProcentBonusDamagePerSec += 1f;
            turret.ProcentBonusDamagePerSecLevel += 1f;
        }
    }

    public void UpgradeSlowEffect()
    {
        float cost = Mathf.Pow(2, turret.SlowEffectLevel);
        if (TrySpend(cost))
        {
            turret.SlowEffect += 1f;
            turret.SlowEffectLevel += 1f;
        }
    }
}
