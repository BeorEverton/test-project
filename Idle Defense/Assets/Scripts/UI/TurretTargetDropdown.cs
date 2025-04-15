using Assets.Scripts.Turrets;
using TMPro;
using UnityEngine;

namespace Assets.Scripts.UI
{
    public class TurretTargetDropdown : MonoBehaviour
    {
        private TMP_Dropdown _dropdown;
        private BaseTurret _baseTurret;
        private TurretUpgradeButton _turretUpgradeButton; // Used only to fetch the turret

        private void Start()
        {
            _dropdown = GetComponent<TMP_Dropdown>();
            _turretUpgradeButton = transform.parent.parent.GetComponentInChildren<TurretUpgradeButton>();
            if (_turretUpgradeButton)
                _baseTurret = _turretUpgradeButton._baseTurret;
            else
                Debug.LogError("Turret not found on " + name);
        }

        public void OnDropdownValueChanged(int index)
        {
            _baseTurret.SetTarget(index);
        }
    }
}