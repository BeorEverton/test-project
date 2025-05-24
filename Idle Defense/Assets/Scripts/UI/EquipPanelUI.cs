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

            HashSet<int> equippedIds = TurretSlotManager.Instance.ExportEquipped()
                .Where(id => id >= 0)
                .ToHashSet();

            List<TurretStatsInstance> owned = TurretInventoryManager.Instance.Owned;

            for (int i = 0; i < owned.Count; i++)
            {
                if (equippedIds.Contains(i))
                    continue;          // skip already equipped

                TurretStatsInstance inst = owned[i];

                // Spawn card prefab
                EquipItemButton card = Instantiate(itemPrefab, listParent);
                EquipItemButton btn = card.GetComponent<EquipItemButton>();

                // Pick icon that matches this turret's current level
                Sprite icon = TurretIconUtility.GetIcon(inst);

                // Hook the button
                btn.Init(
                    inst,
                    () =>                                     // onClick
                    {
                        if (TurretSlotManager.Instance.Equip(targetSlot, inst))
                            Close();                          // close panel on success
                    },
                    icon
                );
            }
        }
    }
}