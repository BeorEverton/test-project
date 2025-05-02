using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Assets.Scripts.Systems;
using Assets.Scripts.Turrets;
using Assets.Scripts.UI;
using Assets.Scripts.WaveSystem;
using Assets.Scripts.SO;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Systems.Audio;

public class SlotWorldButton : MonoBehaviour
{
    /* ----------- slot setup --------------------------------- */
    [Header("Slot setup")]
    [SerializeField] private int slotIndex;      // 0..4
    [SerializeField] private Transform barrelAnchor;   // prefab spawn point

    /* ----------- overlay UI --------------------------------- */
    [Header("Locked overlay")]
    [SerializeField] private CanvasGroup lockedGroup;  // parent canvas group
    [SerializeField] private TMP_Text waveText;      // "Wave 20"
    [SerializeField] private TMP_Text priceText;     // "$5 000"
    [SerializeField] private Button buyButton;     // same click as slot

    /* ----------- turret prefab setup ------------------------ */
    [Header("Turret prefab")]
    [SerializeField] private Transform canvasTransform;       // parent of upgrade panel
    [SerializeField] private TurretUpgradePanelMapping[] panelMappings;
    private GameObject activePanel;
    private TurretType? currentPanelTurretType;
    [SerializeField] private GameObject noTurretHint;

    [System.Serializable]
    public struct TurretUpgradePanelMapping
    {
        public TurretType type;
        public GameObject panelPrefab;
    }

    private GameObject spawned;

    /* --------------------------------------------------------- */
    private void Start()
    {
        TurretSlotManager.I.OnEquippedChanged += RefreshSlot;
        WaveManager.Instance.OnWaveStarted += OnWaveStart;

        RefreshSlot(slotIndex, TurretSlotManager.I.Get(slotIndex));
        UpdateOverlay();
        UpdateColor();
    }

    private void OnDestroy()
    {
        if (TurretSlotManager.I != null)
            TurretSlotManager.I.OnEquippedChanged -= RefreshSlot;

        if (WaveManager.Instance != null)
            WaveManager.Instance.OnWaveStarted -= OnWaveStart;
    }

    /* ---------------- main click ----------------------------- */
    public void OnSlotClicked()
    {
        UIManager.Instance.DeactivateRightPanels();
        AudioManager.Instance.Play("Click");

        // Skip first children because it's the title
        for (int i = 1; i < canvasTransform.transform.childCount; i++)
        {
            canvasTransform.transform.GetChild(i).gameObject.SetActive(false);
        }
        canvasTransform.gameObject.SetActive(true);

        if (!TurretSlotManager.I.Purchased(slotIndex))
        {
            if (TurretSlotManager.I.UnlockSlot(slotIndex))
            {
                UpdateOverlay();
                RefreshSlot(slotIndex, TurretSlotManager.I.Get(slotIndex)); // force check

                // Immediately open the equip panel after unlocking
                UIManager.Instance.OpenEquipPanel(slotIndex);
            }
            return;
        }



        TurretStatsInstance inst = TurretSlotManager.I.Get(slotIndex);

        if (inst == null)
        {
            UIManager.Instance.OpenEquipPanel(slotIndex);
            return;
        }

        // turret is equipped
        if (currentPanelTurretType == inst.TurretType && activePanel != null)
        {
            // reuse current panel
            activePanel.SetActive(true);
            activePanel.GetComponent<TurretUpgradePanelUI>()
                       .Open(slotIndex, inst, GetComponentInChildren<BaseTurret>());
        }
        else
        {
            // close previous panel if type changed
            if (activePanel != null)
                Destroy(activePanel);

            GameObject panelPrefab = null;
            foreach (var mapping in panelMappings)
            {
                if (mapping.type == inst.TurretType)
                {
                    panelPrefab = mapping.panelPrefab;
                    break;
                }
            }

            if (panelPrefab != null)
            {
                activePanel = Instantiate(panelPrefab, canvasTransform);
                currentPanelTurretType = inst.TurretType;

                var ui = activePanel.GetComponent<TurretUpgradePanelUI>();
                ui.Open(slotIndex, inst,  GetComponentInChildren<BaseTurret>());
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

        if (changed != slotIndex) return;

        if (spawned != null)
            Destroy(spawned);

        if (inst != null)
        {
            GameObject prefab = TurretInventoryManager.I.GetPrefab(inst.TurretType);
            spawned = Instantiate(prefab, barrelAnchor.position,
                                  Quaternion.identity, barrelAnchor);
            spawned.GetComponent<BaseTurret>().SavedStats = inst;

            if (noTurretHint != null)
                noTurretHint.SetActive(false);
        }
        else
        {
            if (noTurretHint != null)
                noTurretHint.SetActive(true);
        }

        UpdateOverlay();
    }



    //  Called whenever slot state / wave changes.
    //  Controls the tint of the Slot sprite (black = unavailable).

    private void UpdateColor()
    {
        bool purchased = TurretSlotManager.I.Purchased(slotIndex);
        int curWave = WaveManager.Instance.GetCurrentWaveIndex();
        int needWave = TurretSlotManager.I.WaveRequirement(slotIndex);

        int nextLocked = -1;
        for (int i = 0; i < 5; ++i)
        {
            if (!TurretSlotManager.I.Purchased(i))
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
        bool purchased = TurretSlotManager.I.Purchased(slotIndex);
        int curWave = WaveManager.Instance.GetCurrentWaveIndex();
        int needWave = TurretSlotManager.I.WaveRequirement(slotIndex);

        int nextLocked = -1;
        for (int i = 0; i < 5; ++i)
        {
            if (!TurretSlotManager.I.Purchased(i))
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
            ulong cost = TurretSlotManager.I.BuyCost(slotIndex);

            if (curWave < needWave)
            {
                waveText.gameObject.SetActive(true);
                priceText.gameObject.SetActive(true);

                waveText.text = "Wave " + needWave;
                priceText.text = "$" + UIManager.AbbreviateNumber(cost);

                waveText.color = Color.white;
                priceText.color = Color.white;

                buyButton.interactable = false;
            }
            else
            {
                waveText.gameObject.SetActive(false);
                priceText.gameObject.SetActive(true);

                priceText.text = "$" + UIManager.AbbreviateNumber(cost);
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


}
