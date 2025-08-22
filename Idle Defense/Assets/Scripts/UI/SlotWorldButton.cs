using Assets.Scripts.SO;
using Assets.Scripts.Systems;
using Assets.Scripts.Systems.Audio;
using Assets.Scripts.Turrets;
using Assets.Scripts.WaveSystem;
using System.Collections;
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
        [SerializeField] private GameObject activePanel;
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
            StartCoroutine(DelayHandleChange()); // Initial call to set visibility
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

            BaseTurret baseTurret = GetComponentInChildren<BaseTurret>();
            

            if (baseTurret == null)
            {
                UIManager.Instance.OpenEquipPanel(slotIndex);
                VFX.RangeOverlayManager.Instance.Hide();
            }
            else
            {
                // turret is equipped
                Transform expectedParent = management ? canvasTransformPermanent : canvasTransform;

                VFX.RangeOverlayManager.Instance.ShowFor(baseTurret);

                activePanel.GetComponent<TurretUpgradePanelUI>()
                                    .Open(slotIndex, baseTurret);
            }

        }

        /* ------------- refresh when slot state changes ----------- */

        private void RefreshSlot(int changed, TurretStatsInstance inst)
        {
            //Debug.Log("[SlotWorldButton] Refreshing slot " + changed);            

            if (changed != slotIndex)
                return;

            // Deactivate any existing object
            if (spawned != null)
            {
                //Debug.Log("[SlotWorldButton] Deactivating existing turret in slot " + slotIndex);
                spawned.gameObject.SetActive(false);
                spawned.transform.SetParent(null);
            }

            if (inst == null)
            {
                spawned = null;
                if (noTurretHint != null)
                    noTurretHint.SetActive(true);
                UpdateOverlay();
                return;
            }

            // Look up the EquippedTurret object instead of guessing stats
            if (!TurretSlotManager.Instance.TryGetEquippedTurret(slotIndex, out var equippedTurret) || equippedTurret == null)
            {
                Debug.LogWarning($"[SlotWorldButton] Slot {slotIndex} does not have a valid EquippedTurret reference.");
                return;
            }

            // Try to reuse the GameObject if it already exists
            GameObject go = TurretInventoryManager.Instance.GetGameObjectForInstance(equippedTurret.Runtime);
            if (go == null)
            {
                // Fallback: instantiate and register
                GameObject prefab = TurretInventoryManager.Instance.GetPrefab(inst.TurretType);
                go = Instantiate(prefab);
                var baseTurret = go.GetComponent<BaseTurret>();
                //Debug.Log("[SlotWorldButton] Instantiated new turret prefab for slot " + slotIndex);
                if (baseTurret != null)
                {
                    baseTurret.PermanentStats = equippedTurret.Permanent;
                    baseTurret.RuntimeStats = equippedTurret.Runtime;
                    baseTurret.UpdateTurretAppearance();

                    TurretInventoryManager.Instance.RegisterTurretInstance(baseTurret.PermanentStats, go);
                    TurretInventoryManager.Instance.RegisterTurretInstance(baseTurret.RuntimeStats, go);
                }
            }

            // Place the turret in the anchor
            go.transform.SetParent(barrelAnchor, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.SetActive(true);

            spawned = go;
            //Debug.Log("[SlotWorldButton] Spawned turret in slot " + slotIndex);

            if (noTurretHint != null)
                noTurretHint.SetActive(false);

            UpdateOverlay();
            UpdateColor();
        }

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

        IEnumerator DelayHandleChange()
        {
            yield return new WaitForSeconds(0.3f); // Delay to ensure systems are initialized
            HandleGameStateChanged(GameManager.Instance.CurrentGameState);
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
            UpdateColor();
        }
    }
}