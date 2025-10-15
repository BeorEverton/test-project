// Assets/Scripts/SO/TurretUnlockInfoSO.cs
using Assets.Scripts.SO;
using System.Collections.Generic;
using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif


[CreateAssetMenu(menuName = "IdleDefense/UnlockTable")]
public class TurretUnlockTableSO : ScriptableObject
{
    public List<Entry> Entries;

    [Serializable]
    public struct Entry
    {
        public TurretType Type;
        public int WaveToUnlock;     // 0 == unlocked from start
        public ulong FirstCopyCost;    // 0 == free first copy

        [Tooltip("If true, this turret is locked until a Prestige node unlocks it (see PrestigeNodeSO.UnlockTurretTypes).")]
        public bool RequirePrestigeUnlock;
    }

#if UNITY_EDITOR
    [ContextMenu("Auto-Fill All Entries")]
    private void AutoFill()
    {
        Entries ??= new List<Entry>();
        Entries.Clear();

        var types = (TurretType[])Enum.GetValues(typeof(TurretType));
        ulong cost = 100; // starting cost, tweak as needed

        foreach (var t in types)
        {
            var entry = new Entry
            {
                Type = t,
                WaveToUnlock = 0,        // left empty for you to adjust
                FirstCopyCost = cost
            };
            Entries.Add(entry);

            // Increase cost by 15%
            cost = (ulong)Mathf.CeilToInt(cost * 1.15f);
        }

        // Mark asset dirty so Unity saves it
        EditorUtility.SetDirty(this);
        Debug.Log("[TurretUnlockTableSO] Auto-filled " + Entries.Count + " entries.");
    }
#endif

}
