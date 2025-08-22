using Assets.Scripts.SO;
using Assets.Scripts.Turrets;
using System.Linq;
using UnityEngine;
// for BaseTurret

namespace Assets.Scripts.Systems
{
    public class TurretSlotVisualiser : MonoBehaviour
    {
        [Header("Scene anchors for the 5 slots")]
        [SerializeField] private Transform[] slotAnchors = new Transform[5];

        [Header("Shared data")]
        [SerializeField] private TurretLibrarySO library;   // drag the SAME asset here

        private readonly GameObject[] spawned = new GameObject[5];

        void Start()
        {
            TurretSlotManager.Instance.OnEquippedChanged += HandleChanged;
            RefreshAll();                       // in case a save loaded equips before Awake
        }

        void OnDestroy()
        {
            if (TurretSlotManager.Instance != null)
                TurretSlotManager.Instance.OnEquippedChanged -= HandleChanged;
        }

        void RefreshAll()
        {
            for (int i = 0; i < 5; i++)
                HandleChanged(i, TurretSlotManager.Instance.Get(i));
        }

        void HandleChanged(int slot, TurretStatsInstance inst)
        {
            Debug.Log($"TurretSlotVisualiser: slot {slot} changed to {inst?.TurretType}");
            // Deactivate current turret on this slot
            if (spawned[slot] != null)
            {
                spawned[slot].SetActive(false);
                spawned[slot].transform.SetParent(null);
            }

            if (inst == null)
            {
                spawned[slot] = null;
                return;
            }

            Debug.Log($"TurretSlotVisualiser: slot {slot} will spawn {inst.TurretType}");

            // Try to get existing turret from inventory manager
            GameObject reused = TurretInventoryManager.Instance.GetGameObjectForInstance(inst);
            Debug.Log($"TurretSlotVisualiser: reused turret for slot {slot} is {reused?.name}");
            if (reused != null)
            {
                reused.transform.position = slotAnchors[slot].position;
                reused.transform.SetParent(slotAnchors[slot]);
                reused.SetActive(true);
                spawned[slot] = reused;
                return;
            }

            // If not found, instantiate new and register it
            GameObject prefab = TurretLibrary.Instance.GetPrefab(inst.TurretType);
            GameObject go = Instantiate(prefab, slotAnchors[slot].position, Quaternion.identity, slotAnchors[slot]);

            // Make sure the turret is tracked
            if (TurretInventoryManager.Instance.GetGameObjectForInstance(inst) == null)
            {
                Debug.Log($"TurretSlotVisualiser: registered new turret instance for {inst.TurretType} at slot {slot}");
                TurretInventoryManager.Instance.RegisterTurretInstance(inst, go);
            }

            Debug.Log($"TurretSlotVisualiser: spawned new turret for slot {slot} is {go.name}");
            spawned[slot] = go;
        }


    }
}
