using Assets.Scripts.SO;
using Assets.Scripts.Systems;
using Assets.Scripts.Systems.Audio;
using Assets.Scripts.Turrets;
using Assets.Scripts.WaveSystem;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI
{
    public class SlotWorldButton : MonoBehaviour
    {
        /* ----------- slot setup --------------------------------- */
        [Header("Slot setup")]
        [SerializeField] private int slotIndex;      // 0..4
        [SerializeField] private Transform barrelAnchor;   // prefab spawn point
        [SerializeField] private GameObject visibilityWrapper;


        /* ----------- overlay UI --------------------------------- */
        [Header("Locked overlay")]
        [SerializeField] private CanvasGroup lockedGroup;  // parent canvas group
        [SerializeField] private TMP_Text waveText;      // "Wave 20"
        [SerializeField] private TMP_Text priceText;     // "$5 000"
        [SerializeField] private Button buyButton;     // same click as slot

        /* ----------- turret prefab setup ------------------------ */
        [Header("Turret prefab")]
        [SerializeField] private Transform canvasTransform;       // parent of upgrade panel
        [SerializeField] private Transform canvasTransformPermanent;       // parent of upgrade panel
        [SerializeField] private TurretUpgradePanelMapping[] panelMappings;
        private GameObject activePanel;
        private TurretType? currentPanelTurretType;
        [SerializeField] private GameObject noTurretHint;

        [System.Serializable]
        public struct TurretUpgradePanelMapping
        {
            public TurretType type;
            public GameObject panelPrefab;
            public GameObject permanentPanelPrefab;
        }

        private GameObject spawned;

        /* --------------------------------------------------------- */

        private void Start()
        {
            Initialize();//Invoke(nameof(Initialize), 0.1f); // Delay to ensure systems are initialized
        }

        private void Initialize()
        {
            TurretSlotManager.Instance.OnEquippedChanged += RefreshSlot;
            WaveManager.Instance.OnWaveStarted += OnWaveStart;

            RefreshSlot(slotIndex, TurretSlotManager.Instance.Get(slotIndex));
            UpdateOverlay();
            UpdateColor();
            GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
            HandleGameStateChanged(GameManager.Instance.CurrentGameState); // Initial state
        }

        private void OnDestroy()
        {
            if (TurretSlotManager.Instance != null)
                TurretSlotManager.Instance.OnEquippedChanged -= RefreshSlot;

            if (WaveManager.Instance != null)
                WaveManager.Instance.OnWaveStarted -= OnWaveStart;
        }

        /* ---------------- main click ----------------------------- */

        public void OnSlotClicked()
        {
            UIManager.Instance.DeactivateRightPanels();
            /*TabSelectorButton[] allTabs = FindObjectsByType<TabSelectorButton>(FindObjectsSortMode.None);
            foreach (var tab in allTabs)
            {
                tab.Deselect();
            }*/

            AudioManager.Instance.Play("Click");

            // Skip first children because it's the title
            bool management = GameManager.Instance.CurrentGameState == GameState.Management;

            // 1️ Hide every previously opened upgrade panel in **both** holders (skip index 0 = title)
            for (int i = 1; i < canvasTransform.childCount; i++)
                canvasTransform.GetChild(i).gameObject.SetActive(false);

            for (int i = 1; i < canvasTransformPermanent.childCount; i++)
                canvasTransformPermanent.GetChild(i).gameObject.SetActive(false);

            // 2️ Activate the holder that belongs to the current phase, hide the other
            canvasTransform.gameObject.SetActive(!management);
            canvasTransformPermanent.gameObject.SetActive(management);


            if (!TurretSlotManager.Instance.Purchased(slotIndex))
            {
                if (TurretSlotManager.Instance.UnlockSlot(slotIndex))
                {
                    UpdateOverlay();
                    RefreshSlot(slotIndex, TurretSlotManager.Instance.Get(slotIndex)); // force check

                    // Immediately open the equip panel after unlocking
                    UIManager.Instance.OpenEquipPanel(slotIndex);
                }
                return;
            }

            TurretStatsInstance inst = TurretSlotManager.Instance.Get(slotIndex);

            if (inst == null)
            {
                UIManager.Instance.OpenEquipPanel(slotIndex);
                return;
            }

            // turret is equipped
            Transform expectedParent = management ? canvasTransformPermanent : canvasTransform;

            if (activePanel != null &&
                currentPanelTurretType == inst.TurretType &&
                activePanel.transform.parent == expectedParent)          // ← parent matches phase?
            {
                // Safe to reuse
                activePanel.SetActive(true);
                activePanel.GetComponent<TurretUpgradePanelUI>()
                    .Open(slotIndex, GetComponentInChildren<BaseTurret>());
            }
            else
            {
                if (activePanel != null)
                    Destroy(activePanel);                                 // discard wrong-phase panel

                GameObject panelPrefab = (from m in panelMappings
                                          where m.type == inst.TurretType
                                          select (management ? m.permanentPanelPrefab
                                                             : m.panelPrefab)).FirstOrDefault();

                if (panelPrefab != null)
                {
                    activePanel = Instantiate(panelPrefab, expectedParent);
                    currentPanelTurretType = inst.TurretType;

                    activePanel.GetComponent<TurretUpgradePanelUI>()
                               .Open(slotIndex, GetComponentInChildren<BaseTurret>());
                }
                else
                {
                    Debug.LogWarning($"No panel prefab assigned for {inst.TurretType}");
                }
            }


        }

        /* ------------- refresh when slot state changes ----------- */

        private void RefreshSlot(int changed, TurretStatsInstance inst)
        {
            UpdateColor();

            if (changed != slotIndex)
                return;

            // Deactivate existing object (not destroy!)
            if (spawned != null)
            {
                spawned.gameObject.SetActive(false);
                spawned.transform.SetParent(null);
            }

            if (inst != null)
            {
                // Try to reuse a tracked turret object
                GameObject existing = TurretInventoryManager.Instance.GetGameObjectForInstance(inst);
                if (existing != null)
                {
                    existing.transform.position = barrelAnchor.position;
                    existing.transform.SetParent(barrelAnchor);
                    existing.SetActive(true);
                    spawned = existing;
                }
                else
                {
                    // No tracked turret, spawn new
                    GameObject prefab = TurretInventoryManager.Instance.GetPrefab(inst.TurretType);
                    spawned = Instantiate(prefab, barrelAnchor.position, Quaternion.identity, barrelAnchor);

                    var turret = spawned.GetComponent<BaseTurret>();
                    turret.PermanentStats = inst;
                    turret.SavedStats = inst; // Use current instance (not clone)

                    TurretInventoryManager.Instance.RegisterTurretInstance(inst, spawned);
                }

                if (noTurretHint != null)
                    noTurretHint.SetActive(false);
            }
            else
            {
                spawned = null;
                if (noTurretHint != null)
                    noTurretHint.SetActive(true);
            }

            UpdateOverlay();
        }


        //  Called whenever slot state / wave changes.
        //  Controls the tint of the Slot sprite (black = unavailable).

        private void UpdateColor()
        {
            bool purchased = TurretSlotManager.Instance.Purchased(slotIndex);
            int curWave = WaveManager.Instance.GetCurrentWaveIndex();
            int needWave = TurretSlotManager.Instance.WaveRequirement(slotIndex);

            int nextLocked = -1;
            for (int i = 0; i < 5; ++i)
            {
                if (!TurretSlotManager.Instance.Purchased(i))
                {
                    nextLocked = i;
                    break;
                }
            }

            bool whiteNow = purchased || (slotIndex == nextLocked && curWave >= needWave);

            SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
                sr.color = whiteNow ? Color.white : Color.black;
        }

        private void OnWaveStart(object sender, WaveManager.OnWaveStartedEventArgs _)
        {
            UpdateOverlay();
        }

        //  Shows the correct overlay text & button state.

        private void UpdateOverlay()
        {
            bool purchased = TurretSlotManager.Instance.Purchased(slotIndex);
            int curWave = WaveManager.Instance.GetCurrentWaveIndex();
            int needWave = TurretSlotManager.Instance.WaveRequirement(slotIndex);

            int nextLocked = -1;
            for (int i = 0; i < 5; i++)
            {
                if (!TurretSlotManager.Instance.Purchased(i))
                {
                    nextLocked = i;
                    break;
                }
            }

            bool isNext = slotIndex == nextLocked;

            if (purchased)
            {
                lockedGroup.gameObject.SetActive(false);
                lockedGroup.interactable = false;
                return;
            }

            lockedGroup.gameObject.SetActive(true);
            lockedGroup.alpha = 1f;
            lockedGroup.interactable = true;

            if (isNext)
            {
                ulong cost = TurretSlotManager.Instance.BuyCost(slotIndex);

                if (curWave < needWave)
                {
                    waveText.gameObject.SetActive(true);
                    priceText.gameObject.SetActive(true);

                    waveText.text = "Wave " + needWave;
                    priceText.text = "§" + UIManager.AbbreviateNumber(cost);

                    waveText.color = Color.white;
                    priceText.color = Color.white;

                    buyButton.interactable = false;
                }
                else
                {
                    waveText.gameObject.SetActive(false);
                    priceText.gameObject.SetActive(true);

                    priceText.text = "§" + UIManager.AbbreviateNumber(cost);
                    priceText.color = Color.black;

                    buyButton.interactable = true;
                }
            }
            else
            {
                waveText.gameObject.SetActive(true);
                priceText.gameObject.SetActive(false);

                waveText.text = "Wave " + needWave;
                waveText.color = Color.white;

                buyButton.interactable = false;
            }

            UpdateColor();
        }

        private void HandleGameStateChanged(GameState state)
        {

            // Only change this if stop on death
            if (!PlayerBaseManager.Instance.stopOnDeath)
                return;
            if (visibilityWrapper == null) return;

            bool isManagement = state == GameState.Management;
            bool isUnlocked = TurretSlotManager.Instance.Purchased(slotIndex);
            bool shouldBeVisible = isManagement || isUnlocked || slotIndex == 0;

            visibilityWrapper.SetActive(shouldBeVisible);
        }

    }
}