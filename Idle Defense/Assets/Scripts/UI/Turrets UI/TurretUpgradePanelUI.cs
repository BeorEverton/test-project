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

        // For gunners
        private bool _subscribed;

        public void Open(int slotIndex, BaseTurret baseTurret)
        {
            _slotIndex = slotIndex;
            _baseTurret = baseTurret;

            if (!baseTurret)
            {
                Debug.LogError($"[TurretUpgradePanelUI] Cannot open panel for slot {slotIndex} because base turret is null.");
                return;
            }

            Currency cur = GameManager.Instance.CurrentGameState == GameState.Management
                ? Currency.BlackSteel
                : Currency.Scraps;

            // Get the stats container that will actually be upgraded (runtime vs permanent)
            TurretStatsInstance stats = _baseTurret.GetUpgradeableStats(cur);

            // Ask the manager which upgrades are really available for THIS turret
            var mgr = FindFirstObjectByType<TurretUpgradeManager>();
            var available = mgr.GetAvailableUpgrades(stats);

            // Get all child buttons in layout order
            var buttons = contentRoot.GetComponentsInChildren<TurretUpgradeButton>(true);

            // Assign types to buttons 1:1; hide extras if any
            int i = 0;
            for (; i < buttons.Length; i++)
            {
                if (i < available.Count)
                {
                    var b = buttons[i];
                    b.gameObject.SetActive(true);
                    b.enabled = true;
                    b.SetUpgradeType(available[i]);   // <-- runtime assign
                    b.Init(_baseTurret, cur);         // passes turret & currency, sets texts
                }
                else
                {
                    buttons[i].gameObject.SetActive(false);
                }
            }

            gameObject.SetActive(true);

            // Unequip visibility same as before
            if (PlayerBaseManager.Instance.stopOnDeath)
            {
                bool inGame = GameManager.Instance.CurrentGameState == GameState.InGame;
                unequipButton.gameObject.SetActive(!inGame);
            }
        }

        public void Close() => gameObject.SetActive(false);

        private void Start()
        {
            unequipButton.onClick.AddListener(OnClickUnequip);
        }

        private void OnClickUnequip()
        {
            //Debug.Log($"[TurretUpgradePanelUI] Unequipping turret in slot {_slotIndex}.");
            TurretSlotManager.Instance.Unequip(_slotIndex);
            Close();
        }

        #region gunners        

        private void OnEnable()
        {
            if (!_subscribed && GunnerManager.Instance != null)
            {
                GunnerManager.Instance.OnSlotGunnerChanged += HandleGunnerChanged;
                GunnerManager.Instance.OnSlotGunnerStatsChanged += HandleGunnerChanged;
                _subscribed = true;
            }
            if (PrestigeManager.Instance != null)
                PrestigeManager.Instance.OnPrestigeChanged += HandlePrestigeChanged;

        }

        private void OnDisable()
        {
            if (_subscribed && GunnerManager.Instance != null)
            {
                GunnerManager.Instance.OnSlotGunnerChanged -= HandleGunnerChanged;
                GunnerManager.Instance.OnSlotGunnerStatsChanged -= HandleGunnerChanged;
                _subscribed = false;
            }
            if (PrestigeManager.Instance != null)
                PrestigeManager.Instance.OnPrestigeChanged -= HandlePrestigeChanged;

        }

        private void HandleGunnerChanged(int slot)
        {
            if (slot != _slotIndex) return;
            var buttons = contentRoot.GetComponentsInChildren<TurretUpgradeButton>(true);
            for (int i = 0; i < buttons.Length; i++)
                buttons[i].UpdateDisplayFromType();
        }

        #endregion

        private void HandlePrestigeChanged()
        {
            var buttons = contentRoot.GetComponentsInChildren<TurretUpgradeButton>(true);
            for (int i = 0; i < buttons.Length; i++)
                buttons[i].UpdateDisplayFromType();
        }

    }
}
