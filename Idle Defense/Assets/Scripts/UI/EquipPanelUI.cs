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

            // Owned is now List<OwnedTurret>
            var owned = TurretInventoryManager.Instance.Owned;

            // Use equipped indices so we can compare by position in Owned
            var equippedIds = TurretSlotManager.Instance.ExportEquipped() ?? new List<int>();
            var equippedSet = new HashSet<int>(equippedIds.Where(i => i >= 0));

            for (int i = 0; i < owned.Count; i++)
            {
                if (equippedSet.Contains(i))
                    continue; // already equipped

                var entry = owned[i];
                var instPermanent = entry.Permanent;   // use Permanent for icon + selection

                // Spawn card prefab
                EquipItemButton card = Instantiate(itemPrefab, listParent);
                var btn = card.GetComponent<EquipItemButton>();

                Sprite icon = TurretIconUtility.GetIcon(instPermanent);

                btn.Init(
                    instPermanent,
                    () =>
                    {
                        if (TurretSlotManager.Instance.Equip(targetSlot, instPermanent))
                            Close();
                    },
                    icon
                );
            }
        }


    }
}