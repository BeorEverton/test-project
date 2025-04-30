using UnityEngine;
using Assets.Scripts.Systems;   // Singletons
using Assets.Scripts.Turrets;
using Assets.Scripts.UI;   // BaseTurret, TurretStatsInstance

// World-space slot that handles click/tap
public class SlotWorldButton : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] private int slotIndex;    // 0-4
    [SerializeField] private Transform barrelAnchor; // spawn point
    [SerializeField] private SpriteRenderer slotSprite;   // slot icon

    private GameObject spawned;

    private void Start()
    {
        TurretSlotManager.I.OnEquippedChanged += Refresh;
        Refresh(slotIndex, TurretSlotManager.I.Get(slotIndex));
    }

    private void OnDestroy()
    {
        if (TurretSlotManager.I != null)
            TurretSlotManager.I.OnEquippedChanged -= Refresh;
    }

    // Poll click/tap each frame (no colliders needed)
    private void Update()
    {
        if (!Input.GetMouseButtonDown(0)) return;

        Vector3 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        if (slotSprite.bounds.Contains(worldPos))
            HandleClick();
    }

    private void HandleClick()
    {
        Debug.Log("Handled click on slot " + slotIndex);
        // Pay for slot if not yet purchased
        if (!TurretSlotManager.I.Purchased(slotIndex))
        {
            TurretSlotManager.I.UnlockSlot(slotIndex);
            Debug.Log("Unlocked slot " + slotIndex);
            return;
        }

        // Decide whether to equip or open unequip panel
        TurretStatsInstance inst = TurretSlotManager.I.Get(slotIndex);
        if (inst == null)
            UIManager.Instance.OpenEquipPanel(slotIndex);      // custom UI
        else
            UIManager.Instance.OpenUnequipPanel(slotIndex);    // custom UI
    }

    // Spawn or destroy turret prefab when slot state changes
    private void Refresh(int changedSlot, TurretStatsInstance inst)
    {
        if (changedSlot != slotIndex) return;

        if (spawned != null) Destroy(spawned);

        if (inst == null) return; // empty slot

        GameObject prefab = TurretInventoryManager.I.GetPrefab(inst.TurretType);
        spawned = Instantiate(prefab, barrelAnchor.position, Quaternion.identity, barrelAnchor);
        spawned.GetComponent<BaseTurret>().SavedStats = inst;
    }
}
