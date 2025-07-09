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
            Currency cur = GameManager.Instance.CurrentGameState == GameState.Management
            ? Currency.BlackSteel        // permanent
            : Currency.Scraps;           // temporary

            foreach (TurretUpgradeButton btn in contentRoot.GetComponentsInChildren<TurretUpgradeButton>(true))
            {
                btn.enabled = true;
                btn.Init(_baseTurret, cur);  // <-- now passes turret & currency
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
