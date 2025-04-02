using Assets.Scripts.SO;
using Assets.Scripts.Turrets;
using UnityEngine;

public class TurretUpgradeButton : MonoBehaviour
{
    [Header("Set in Runtime")]
    private TurretUpgradeManager upgradeManager;

    [Header("Assigned in Inspector")]
    [SerializeField] private BaseTurret baseTurret;
    [SerializeField] private TurretStatsInstance turret;

    public void SetTurret()
    {
        upgradeManager.SetTurret(turret);
    }

    private void Start()
    {
        upgradeManager = FindFirstObjectByType<TurretUpgradeManager>();
        turret = baseTurret.GetStats();
    }
       

}
