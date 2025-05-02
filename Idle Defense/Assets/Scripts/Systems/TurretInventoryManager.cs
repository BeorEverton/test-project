using Assets.Scripts.SO;
using Assets.Scripts.Systems;
using Assets.Scripts.Turrets;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;
using Assets.Scripts.WaveSystem;
using Assets.Scripts.Systems.Save;

public class TurretInventoryManager : MonoBehaviour
{
    public static TurretInventoryManager I { get; private set; }

    [SerializeField] private TurretUnlockTableSO unlockTable;
    private readonly List<TurretStatsInstance> owned = new();
    public readonly HashSet<TurretType> unlockedTypes = new();
    [SerializeField] private TurretLibrarySO turretLibrary;   // assign the new asset

    public List<TurretStatsInstance> Owned => owned;

    private List<int> pendingEquipped;
    private List<bool> pendingPurchased;

    public int WaveRequirement(TurretType t) =>
        unlockTable.Entries.First(e => e.Type == t).WaveToUnlock;

    public event Action OnInventoryChanged;   // UI will subscribe

    public bool IsTurretTypeUnlocked(TurretType t) => unlockedTypes.Contains(t);


    void Awake()
    {
        if (I == null) I = this;      
    }

    private void Start()
    {
        WaveManager.Instance.OnWaveStarted += HandleWaveStarted;
        EnsureStarterTurret();

        if (pendingEquipped != null)
        {
            TurretSlotManager.I.ImportEquipped(pendingEquipped);
            pendingEquipped = null;
        }
        if (pendingPurchased != null)
        {
            TurretSlotManager.I.ImportPurchasedFlags(pendingPurchased);
            pendingPurchased = null;
        }

    }

    private void EnsureStarterTurret()
    {
        if (owned.Count > 0) return;                     // already got one (load)

        TryUnlockByWave(0);

        if (unlockedTypes.Contains(TurretType.MachineGun))
        {
            TurretInfoSO baseSO = turretLibrary.GetInfo(TurretType.MachineGun);
            var inst = new TurretStatsInstance(baseSO)
            {
                TurretType = TurretType.MachineGun,
                IsUnlocked = true
            };
            owned.Add(inst);
        }

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
        OnInventoryChanged?.Invoke();
        SaveGameManager.Instance.SaveGame();
        return changed;
    }

    public bool TryPurchase(TurretType type)
    {
        if (!unlockedTypes.Contains(type)) return false;

        int countOwned = owned.Count(t => t.TurretType == type);
        if (countOwned >= 5)
            return false;

        ulong cost = GetCost(type, countOwned);

        if (GameManager.Instance.Money < cost) return false;
        GameManager.Instance.SpendMoney(cost);

        // create runtime copy from the original SO
        TurretInfoSO baseSO = turretLibrary.GetInfo(type);
        var inst = new TurretStatsInstance(baseSO)
        {
            TurretType = type,
            /* this flag MUST be true otherwise BaseTurret.Start()
               will clone the stats and you’ll lose all later upgrades */
            IsUnlocked = true
        };
        owned.Add(inst);

        OnInventoryChanged?.Invoke();
        SaveGameManager.Instance.SaveGame();
        return true;

    }

    public ulong GetCost(TurretType type, int currentOwned)
    {
        var entry = unlockTable.Entries.First(e => e.Type == type);
        return entry.FirstCopyCost * (ulong)Mathf.Pow(2, Mathf.Max(0, currentOwned - 1));
    }

    public GameObject GetPrefab(TurretType t) => turretLibrary.GetPrefab(t);


    // ---------- save / load ----------

    public TurretInventoryDTO ExportToDTO()
    {
        return new TurretInventoryDTO
        {
            Owned = owned,
            EquippedIds = TurretSlotManager.I.ExportEquipped(),
            UnlockedTypes = unlockedTypes.ToList(),
            SlotPurchased = TurretSlotManager.I.GetPurchasedFlags()
        };
    }

    public void ImportFromDTO(TurretInventoryDTO dto)
    {
        owned.Clear();
        owned.AddRange(dto.Owned ?? new());

        unlockedTypes.Clear();
        unlockedTypes.UnionWith(dto.UnlockedTypes ?? new());

        if (TurretSlotManager.I != null)
        {
            TurretSlotManager.I.ImportEquipped(dto.EquippedIds);
            TurretSlotManager.I.ImportPurchasedFlags(dto.SlotPurchased);
        }
        else
        {
            pendingEquipped = dto.EquippedIds;
            pendingPurchased = dto.SlotPurchased;
        }
    }

}
