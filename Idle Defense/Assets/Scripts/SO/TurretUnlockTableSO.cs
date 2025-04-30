// Assets/Scripts/SO/TurretUnlockInfoSO.cs
using Assets.Scripts.SO;
using System.Collections.Generic;
using System;
using UnityEngine;

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
    }
}
