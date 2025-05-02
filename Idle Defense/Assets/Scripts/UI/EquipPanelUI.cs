using Assets.Scripts.Systems;
using Assets.Scripts.Turrets;
using System.Linq;
using UnityEngine;

public class EquipPanelUI : MonoBehaviour
{
    [SerializeField] private Transform listParent;         // empty in ScrollView
    [SerializeField] private EquipItemButton itemPrefab;   // simple button prefab

    private int targetSlot;                                // slot we’ll equip into

    public void Open(int slotIndex)
    {
        targetSlot = slotIndex;
        RefreshList();
        gameObject.SetActive(true);
    }

    public void Close() => gameObject.SetActive(false);

    // -------------------------------------------------------------------------
    private void RefreshList()
    {
        // Clear old cards
        foreach (Transform c in listParent) Destroy(c.gameObject);

        var equippedIds = TurretSlotManager.I.ExportEquipped()
                                            .Where(id => id >= 0)
                                            .ToHashSet();

        var owned = TurretInventoryManager.I.Owned;

        for (int i = 0; i < owned.Count; i++)
        {
            if (equippedIds.Contains(i)) continue;          // skip already equipped

            TurretStatsInstance inst = owned[i];

            // Spawn card prefab
            var card = Instantiate(itemPrefab, listParent);
            var btn = card.GetComponent<EquipItemButton>();

            // Pick icon that matches this turret's current level
            Sprite icon = TurretIconUtility.GetIcon(inst);

            // Hook the button
            btn.Init(
                inst,
                () =>                                     // onClick
                {
                    if (TurretSlotManager.I.Equip(targetSlot, inst))
                        Close();                          // close panel on success
                },
                icon
            );
        }
    }

}
