using UnityEngine;
using TMPro;
using Assets.Scripts.Turrets;

namespace Assets.Scripts.UI
{
    public class TurretTargetDropdown : MonoBehaviour
    {
        private TMP_Dropdown dropdown;
        private BaseTurret baseTurret;
        private TurretUpgradeButton turretUpgradeButton; // Used only to fetch the turret

        private void Start()
        {
            dropdown = GetComponent<TMP_Dropdown>();
            turretUpgradeButton = transform.parent.GetComponentInChildren<TurretUpgradeButton>();
            if (turretUpgradeButton) baseTurret = turretUpgradeButton._baseTurret;
            else Debug.LogError("Turret not found on " + name);
        }

        public void OnDropdownValueChanged(int index)
        {
            baseTurret.SetTarget(index);
        }

    }

}