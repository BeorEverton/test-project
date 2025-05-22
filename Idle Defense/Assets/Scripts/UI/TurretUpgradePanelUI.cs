using Assets.Scripts.Systems;
using Assets.Scripts.Turrets;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI
{
    public class TurretUpgradePanelUI : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private Button unequipButton;
        [SerializeField] private Transform contentRoot;   // Parent of all TurretUpgradeButtons

        private int _slotIndex;
        private BaseTurret _baseTurret;

        public void Open(int slotIndex, BaseTurret baseTurret)
        {
            _slotIndex = slotIndex;
            _baseTurret = baseTurret;

            // Assign turret to all upgrade buttons inside this panel
            foreach (TurretUpgradeButton btn in contentRoot.GetComponentsInChildren<TurretUpgradeButton>())
            {
                btn._baseTurret = _baseTurret;
                btn.enabled = true;
                btn.Init();
            }

            gameObject.SetActive(true);
        }

        public void Close() => gameObject.SetActive(false);

        private void Start()
        {
            unequipButton.onClick.AddListener(OnClickUnequip);
        }

        private void OnClickUnequip()
        {
            TurretSlotManager.Instance.Unequip(_slotIndex);
            Close();
        }
    }
}
