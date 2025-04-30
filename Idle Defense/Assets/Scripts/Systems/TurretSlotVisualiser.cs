// Assets/Scripts/Systems/TurretSlotVisualiser.cs
using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.Turrets;
using Assets.Scripts.Systems;     // for BaseTurret

public class TurretSlotVisualiser : MonoBehaviour
{
    [Header("Scene anchors for the 5 slots")]
    [SerializeField] private Transform[] slotAnchors = new Transform[5];

    [Header("Shared data")]
    [SerializeField] private TurretLibrarySO library;   // drag the SAME asset here

    private readonly GameObject[] spawned = new GameObject[5];

    void Start()
    {
        TurretSlotManager.I.OnEquippedChanged += HandleChanged;
        RefreshAll();                       // in case a save loaded equips before Awake
    }

    void OnDestroy()
    {
        if (TurretSlotManager.I != null)
            TurretSlotManager.I.OnEquippedChanged -= HandleChanged;
    }

    void RefreshAll()
    {
        for (int i = 0; i < 5; i++)
            HandleChanged(i, TurretSlotManager.I.Get(i));
    }

    void HandleChanged(int slot, TurretStatsInstance inst)
    {
        // 1. Remove previous
        if (spawned[slot] != null) Destroy(spawned[slot]);

        // 2. Empty slot -> nothing to do
        if (inst == null) { spawned[slot] = null; return; }

        // 3. Spawn new prefab
        GameObject prefab = library.GetPrefab(inst.TurretType);
        GameObject go = Instantiate(prefab, slotAnchors[slot].position, Quaternion.identity, slotAnchors[slot]);

        // Pass runtime stats to the turret behaviour
        var baseTurret = go.GetComponent<BaseTurret>();
        baseTurret.SavedStats = inst;

        spawned[slot] = go;
    }
}
