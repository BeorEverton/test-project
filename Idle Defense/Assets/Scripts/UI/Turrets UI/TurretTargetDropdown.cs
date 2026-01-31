using Assets.Scripts.Turrets;
using TMPro;
using UnityEngine;

namespace Assets.Scripts.UI
{
    public class TurretTargetDropdown : MonoBehaviour
    {
        private TMP_Dropdown _dropdown;
        private BaseTurret _baseTurret;

        private void Awake()
        {
            _dropdown = GetComponent<TMP_Dropdown>();
            if (_dropdown == null)
                Debug.LogError($"[TurretTargetDropdown] Missing TMP_Dropdown on {name}");
        }

        /// <summary>
        /// Called by TurretUpgradePanelUI every time the panel opens for a turret.
        /// Ensures the dropdown reflects THAT turret's current target setting.
        /// </summary>
        public void Bind(BaseTurret turret)
        {
            _baseTurret = turret;

            if (_dropdown == null || _baseTurret == null)
                return;

            int idx = (int)_baseTurret.EnemyTargetChoice;
            idx = Mathf.Clamp(idx, 0, _dropdown.options.Count - 1);

            // Critical: don't fire OnValueChanged while syncing UI
            _dropdown.SetValueWithoutNotify(idx);
            _dropdown.RefreshShownValue();
        }

        public void OnDropdownValueChanged(int index)
        {
            if (_baseTurret == null)
                return;

            _baseTurret.SetTarget(index);
        }
    }
}
