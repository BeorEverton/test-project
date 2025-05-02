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
            TurretSlotManager.I.UnlockSlot(slotIndex);   // tries to pay
            UpdateOverlay();
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

        if (spawned != null) Destroy(spawned);

        if (inst != null)
        {
            GameObject prefab = TurretInventoryManager.I.GetPrefab(inst.TurretType);
            spawned = Instantiate(prefab, barrelAnchor.position,
                                   Quaternion.identity, barrelAnchor);
            spawned.GetComponent<BaseTurret>().SavedStats = inst;
        }
        UpdateOverlay();
    }

    private void UpdateColor()
    {
        bool purchased = TurretSlotManager.I.Purchased(slotIndex);
        SpriteRenderer slotSprite = GetComponentInChildren<SpriteRenderer>();
        slotSprite.color = purchased ? Color.white : Color.black;

        if (purchased)
        {
            lockedGroup.gameObject.SetActive(false);
            lockedGroup.interactable = false;
        }
    }

    private void OnWaveStart(object sender, WaveManager.OnWaveStartedEventArgs _)
    {
        UpdateOverlay();
    }

    /* ------------- overlay logic ----------------------------- */
    private void UpdateOverlay()
    {
        bool purchased = TurretSlotManager.I.Purchased(slotIndex);
        int curWave = WaveManager.Instance.GetCurrentWaveIndex();
        int needWave = TurretSlotManager.I.WaveRequirement(slotIndex);

        /* find the next locked slot (lowest index not purchased) */
        int nextLocked = -1;
        for (int i = 0; i < 5; i++)
        {
            if (!TurretSlotManager.I.Purchased(i))
            {
                nextLocked = i;
                break;
            }
        }
        bool isNext = slotIndex == nextLocked;

        /* already bought: hide overlay */
        if (purchased)
        {
            lockedGroup.gameObject.SetActive(false);            
            lockedGroup.interactable = false;
            return;
        }

        /* show overlay */
        lockedGroup.alpha = 1;
        lockedGroup.interactable = true;

        waveText.text = "Wave " + needWave;

        if (isNext)
        {
            ulong cost = TurretSlotManager.I.BuyCost(slotIndex);
            priceText.text = "$" + UIManager.AbbreviateNumber(cost);
            priceText.gameObject.SetActive(true);

            bool canBuy = curWave >= needWave;
            waveText.gameObject.SetActive(false);
            priceText.color = Color.black;
            buyButton.interactable = canBuy;
        }
        else
        {
            priceText.gameObject.SetActive(false);
            buyButton.interactable = false;
        }
    }
}
