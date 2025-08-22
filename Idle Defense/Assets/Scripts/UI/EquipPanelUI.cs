using Assets.Scripts.Systems;
using Assets.Scripts.Turrets;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts.UI
{
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
            foreach (Transform c in listParent)
                Destroy(c.gameObject);

            // Use permanent references – the only canonical "owned" instances.
            var owned = TurretInventoryManager.Instance.Owned;
            var equippedSet = TurretSlotManager.Instance.GetEquippedPermanents().ToHashSet();

            foreach (var inst in owned)
            {
                if (equippedSet.Contains(inst))
                    continue; // already equipped somewhere

                // Spawn card prefab
                EquipItemButton card = Instantiate(itemPrefab, listParent);
                EquipItemButton btn = card.GetComponent<EquipItemButton>();

                Sprite icon = TurretIconUtility.GetIcon(inst);

                btn.Init(
                    inst,
                    () =>
                    {
                        if (TurretSlotManager.Instance.Equip(targetSlot, inst))
                            Close();
                    },
                    icon
                );
            }
        }

    }
}