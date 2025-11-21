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

        public void CloseAndSelect()
        {
            // Hide the equip list
            gameObject.SetActive(false);

            // Try to open the upgrade panel for the newly equipped turret in this slot.
            // 1) Get the runtime stats now bound to the target slot
            var runtime = TurretSlotManager.Instance.Get(targetSlot);
            if (runtime == null)
            {
                Debug.LogWarning($"[EquipPanelUI] No runtime stats found on slot {targetSlot} after equip.");
                return;
            }

            // 2) Resolve the spawned turret GameObject from the runtime instance
            var go = TurretInventoryManager.Instance.GetGameObjectForInstance(runtime);
            if (go == null)
            {
                // Fallback: if the mapping didn’t resolve yet, try a light scene lookup under the slot anchor
                // (Usually not needed because SlotWorldButton spawns synchronously on OnEquippedChanged)
                Debug.LogWarning($"[EquipPanelUI] No GameObject registered for slot {targetSlot}. Upgrade UI will not open.");
                return;
            }

            // 3) Get the BaseTurret component
            var baseTurret = go.GetComponent<BaseTurret>();
            if (baseTurret == null)
            {
                Debug.LogWarning($"[EquipPanelUI] Spawned turret on slot {targetSlot} has no BaseTurret component.");
                return;
            }

            // 4) Open the standard upgrade panel with the correct slot and turret
            UIManager.Instance.OpenTurretUpgradePanel(targetSlot, baseTurret);
        }


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
                            CloseAndSelect();
                    },
                    icon
                );
            }
        }


    }
}