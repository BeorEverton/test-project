using Assets.Scripts.SO;
using Assets.Scripts.Systems;
using Assets.Scripts.Turrets;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;
using Assets.Scripts.WaveSystem;

public class TurretInventoryManager : MonoBehaviour
{
    public static TurretInventoryManager I { get; private set; }

    [SerializeField] private TurretUnlockTableSO unlockTable;
    private readonly List<TurretStatsInstance> owned = new();
    private readonly HashSet<TurretType> unlockedTypes = new();
    [SerializeField] private TurretLibrarySO turretLibrary;   // assign the new asset

    public List<TurretStatsInstance> Owned => owned;

    void Awake()
    {
        if (I == null) I = this;          
        Load();     
    }

    private void Start()
    {
        WaveManager.Instance.OnWaveStarted += HandleWaveStarted;
    }

    void OnDestroy()
    {
        if (WaveManager.Instance != null)
            WaveManager.Instance.OnWaveStarted -= HandleWaveStarted;
    }

    private void HandleWaveStarted(object sender, WaveManager.OnWaveStartedEventArgs e)
    {
        TryUnlockByWave(e.WaveNumber);
    }

    public bool TryUnlockByWave(int currentWave)
    {
        bool changed = false;
        foreach (var e in unlockTable.Entries)
        {
            if (currentWave >= e.WaveToUnlock && unlockedTypes.Add(e.Type))
                changed = true;
        }
        if (changed) Save();
        return changed;
    }

    public bool TryPurchase(TurretType type)
    {
        if (!unlockedTypes.Contains(type)) return false;

        int countOwned = owned.Count(t => t.TurretType == type);
        ulong cost = GetCost(type, countOwned);

        if (GameManager.Instance.Money < cost) return false;
        GameManager.Instance.SpendMoney(cost);

        // create runtime copy from the original SO
        TurretInfoSO baseSO = turretLibrary.GetInfo(type);       // helper that returns the right SO
        owned.Add(new TurretStatsInstance(baseSO) { TurretType = type });
        Save();
        return true;
    }

    public ulong GetCost(TurretType type, int currentOwned)
    {
        var entry = unlockTable.Entries.First(e => e.Type == type);
        return entry.FirstCopyCost * (ulong)Mathf.Pow(2, Mathf.Max(0, currentOwned - 1));
    }

    public GameObject GetPrefab(TurretType t) => turretLibrary.GetPrefab(t);

    // ---------- save / load ----------
    const string SAVE_KEY = "turret_inv";

    [Serializable]
    class SaveData
    {
        public List<TurretStatsInstance> owned;
        public List<int> equippedIds;            // handled by SlotManager
        public List<TurretType> unlocked;
        public List<bool> slotPurchased;

    }

    public void Save()
    {
        var data = new SaveData
        {
            owned = owned,
            unlocked = unlockedTypes.ToList(),
            equippedIds = TurretSlotManager.I.ExportEquipped(),
            slotPurchased = TurretSlotManager.I.GetPurchasedFlags()
        };
        PlayerPrefs.SetString(SAVE_KEY, JsonUtility.ToJson(data));
    }

    void Load()
    {
        if (!PlayerPrefs.HasKey(SAVE_KEY)) return;
        var data = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(SAVE_KEY));
        owned.Clear(); owned.AddRange(data.owned ?? new());
        unlockedTypes.Clear(); unlockedTypes.UnionWith(data.unlocked ?? new());
        TurretSlotManager.I.ImportPurchasedFlags(data.slotPurchased);
        // slots will finish loading after their own Awake()
    }
}
