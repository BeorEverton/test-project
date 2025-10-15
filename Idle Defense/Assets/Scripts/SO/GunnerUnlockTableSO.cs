using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GunnerUnlockTable", menuName = "ScriptableObjects/Gunner Unlock Table", order = 11)]
public class GunnerUnlockTableSO : ScriptableObject
{
    [Serializable]
    public struct Entry
    {
        public GunnerSO Gunner;             // drag the SO here
        [HideInInspector] public string GunnerId; // auto-filled from Gunner

        public int WaveToUnlock;            // 0 = available from start
        public ulong unlockCost;         // cost in Scraps (0 = free)
        public Currency unlockCurrency;
        public bool RequirePrestigeUnlock;  // needs prestige node before purchasable
    }

    public List<Entry> Entries = new List<Entry>();

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Keep ids in sync with the SOs; helps diffs & runtime lookup
        for (int i = 0; i < Entries.Count; i++)
        {
            var e = Entries[i];
            e.GunnerId = e.Gunner ? e.Gunner.GunnerId : string.Empty;
            Entries[i] = e;
        }
    }
#endif

    public bool TryGet(string id, out Entry e)
    {
        for (int i = 0; i < Entries.Count; i++)
        {
            if (Entries[i].GunnerId == id) { e = Entries[i]; return true; }
        }
        e = default;
        return false;
    }

    // Convenience
    public bool TryGetBySO(GunnerSO so, out Entry e) => TryGet(so ? so.GunnerId : "", out e);

}
