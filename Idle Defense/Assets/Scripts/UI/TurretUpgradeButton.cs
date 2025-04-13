using TMPro;
using Assets.Scripts.Turrets;
using UnityEngine;


public class TurretUpgradeButton : MonoBehaviour
{
    [Header("Set in Runtime")]
    private TurretUpgradeManager upgradeManager;

    [Header("Assigned in Inspector")]
    [SerializeField] private BaseTurret baseTurret;
    [SerializeField] private TurretStatsInstance turret;

    [Header("UI Elements (Auto-Assigned)")]
    [SerializeField] private TextMeshProUGUI statName, statValue, statUpgradeAmount, statUpgradeCost;

    [Header("Upgrade Type")]
    [SerializeField] private TurretUpgradeType upgradeType;

    private void Awake()
    {
        // Auto-assign the first two TextMeshProUGUI components in children
        var tmpros = GetComponentsInChildren<TextMeshProUGUI>();

        if (tmpros.Length >= 4)
        {
            statName = tmpros[0];
            statValue = tmpros[1];
            statUpgradeAmount = tmpros[2];
            statUpgradeCost = tmpros[3];
        }
        else
        {
            Debug.LogWarning($"[TurretUpgradeButton] Couldn't auto-assign TextMeshProUGUI on {name}");
        }
    }


    private void Start()
    {
        upgradeManager = FindFirstObjectByType<TurretUpgradeManager>();
        turret = baseTurret.GetStats();

        // Update the initial data
        SetTurret();
        statName.SetText(GetDisplayNameForUpgrade(upgradeType));
        UpdateDisplayFromType();

    }

    public void SetTurret()
    {
        upgradeManager.SetTurret(turret, this);
    }

    public void OnClick()
    {
        if (upgradeManager == null)
            upgradeManager = FindFirstObjectByType<TurretUpgradeManager>();

        switch (upgradeType)
        {
            case TurretUpgradeType.Damage: upgradeManager.UpgradeDamage(); break;
            case TurretUpgradeType.FireRate: upgradeManager.UpgradeFireRate(); break;
            case TurretUpgradeType.CriticalChance: upgradeManager.UpgradeCriticalChance(); break;
            case TurretUpgradeType.CriticalDamageMultiplier: upgradeManager.UpgradeCriticalDamageMultiplier(); break;
            case TurretUpgradeType.ExplosionRadius: upgradeManager.UpgradeExplosionRadius(); break;
            case TurretUpgradeType.SplashDamage: upgradeManager.UpgradeSplashDamage(); break;
            case TurretUpgradeType.PierceChance: upgradeManager.UpgradePierceChance(); break;
            case TurretUpgradeType.PierceDamageFalloff: upgradeManager.UpgradePierceDamageFalloff(); break;
            case TurretUpgradeType.PelletCount: upgradeManager.UpgradePelletCount(); break;
            case TurretUpgradeType.DamageFalloffOverDistance: upgradeManager.UpgradeDamageFalloffOverDistance(); break;
            case TurretUpgradeType.PercentBonusDamagePerSec: upgradeManager.UpgradePercentBonusDamagePerSec(); break;
            case TurretUpgradeType.SlowEffect: upgradeManager.UpgradeSlowEffect(); break;
        }
    }


    public void UpdateStats(string value, string upgradeAmount, string upgradeCost)
    {
        statValue.SetText(value);
        statUpgradeAmount.SetText(upgradeAmount);
        statUpgradeCost.SetText(upgradeCost);
    }


    private string GetDisplayNameForUpgrade(TurretUpgradeType type)
    {
        return type switch
        {
            TurretUpgradeType.Damage => "Damage",
            TurretUpgradeType.FireRate => "Fire Rate",
            TurretUpgradeType.CriticalChance => "Critical Chance",
            TurretUpgradeType.CriticalDamageMultiplier => "Critical Damage",
            TurretUpgradeType.ExplosionRadius => "Explosion Radius",
            TurretUpgradeType.SplashDamage => "Splash Damage",
            TurretUpgradeType.PierceChance => "Pierce Chance",
            TurretUpgradeType.PierceDamageFalloff => "Pierce Falloff",
            TurretUpgradeType.PelletCount => "Pellet Count",
            TurretUpgradeType.DamageFalloffOverDistance => "Range Falloff",
            TurretUpgradeType.PercentBonusDamagePerSec => "Bonus Dmg/s",
            TurretUpgradeType.SlowEffect => "Slow Effect",
            _ => type.ToString()
        };
    }

    public void UpdateDisplayFromType()
    {
        switch (upgradeType)
        {
            case TurretUpgradeType.Damage: upgradeManager.UpdateDamageDisplay(); break;
            case TurretUpgradeType.FireRate: upgradeManager.UpdateFireRateDisplay(); break;
            case TurretUpgradeType.CriticalChance: upgradeManager.UpdateCriticalChanceDisplay(); break;
            case TurretUpgradeType.CriticalDamageMultiplier: upgradeManager.UpdateCriticalDamageMultiplierDisplay(); break;
            case TurretUpgradeType.ExplosionRadius: upgradeManager.UpdateExplosionRadiusDisplay(); break;
            case TurretUpgradeType.SplashDamage: upgradeManager.UpdateSplashDamageDisplay(); break;
            case TurretUpgradeType.PierceChance: upgradeManager.UpdatePierceChanceDisplay(); break;
            case TurretUpgradeType.PierceDamageFalloff: upgradeManager.UpdatePierceDamageFalloffDisplay(); break;
            case TurretUpgradeType.PelletCount: upgradeManager.UpdatePelletCountDisplay(); break;
            case TurretUpgradeType.DamageFalloffOverDistance: upgradeManager.UpdateDamageFalloffOverDistanceDisplay(); break;
            case TurretUpgradeType.PercentBonusDamagePerSec: upgradeManager.UpdatePercentBonusDamagePerSecDisplay(); break;
            case TurretUpgradeType.SlowEffect: upgradeManager.UpdateSlowEffectDisplay(); break;
        }
    }

}
