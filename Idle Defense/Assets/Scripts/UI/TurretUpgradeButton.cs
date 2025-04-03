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
    [SerializeField] private TextMeshProUGUI statValue, upgradeAndCost;

    [Header("Upgrade Type")]
    [SerializeField] private TurretUpgradeType upgradeType;

    private void Awake()
    {
        // Auto-assign the first two TextMeshProUGUI components in children
        var tmpros = GetComponentsInChildren<TextMeshProUGUI>();

        if (tmpros.Length >= 2)
        {
            statValue = tmpros[1];
            upgradeAndCost = tmpros[2];
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
        UpdateDisplayFromType();

    }

    public void SetTurret()
    {
        upgradeManager.SetTurret(turret, this);
    }

    public void UpdateStats(string value, string upgradeCost)
    {
        statValue.SetText(value);
        upgradeAndCost.SetText(upgradeCost);
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
            case TurretUpgradeType.PierceCount: upgradeManager.UpdatePierceCountDisplay(); break;
            case TurretUpgradeType.PierceDamageFalloff: upgradeManager.UpdatePierceDamageFalloffDisplay(); break;
            case TurretUpgradeType.PelletCount: upgradeManager.UpdatePelletCountDisplay(); break;
            case TurretUpgradeType.DamageFalloffOverDistance: upgradeManager.UpdateDamageFalloffOverDistanceDisplay(); break;
            case TurretUpgradeType.PercentBonusDamagePerSec: upgradeManager.UpdatePercentBonusDamagePerSecDisplay(); break;
            case TurretUpgradeType.SlowEffect: upgradeManager.UpdateSlowEffectDisplay(); break;
        }
    }

}
