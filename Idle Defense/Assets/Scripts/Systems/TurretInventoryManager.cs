using Assets.Scripts.SO;
using Assets.Scripts.Systems.Save;
using Assets.Scripts.Turrets;
using Assets.Scripts.WaveSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts.Systems
{
    public class TurretInventoryManager : MonoBehaviour
    {
        public static TurretInventoryManager Instance { get; private set; }

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

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
        }

        private void Start()
        {
            WaveManager.Instance.OnWaveStarted += HandleWaveStarted;
            EnsureStarterTurret();

            if (pendingEquipped != null)
            {
                TurretSlotManager.Instance.ImportEquipped(pendingEquipped);
                pendingEquipped = null;
            }
            if (pendingPurchased != null)
            {
                TurretSlotManager.Instance.ImportPurchasedFlags(pendingPurchased);
                pendingPurchased = null;
            }
        }

        private void EnsureStarterTurret()
        {
            if (owned.Count > 0)
                return;                     // already got one (load)

            TryUnlockByWave(0);

            if (!unlockedTypes.Contains(TurretType.MachineGun))
                return;

            TurretInfoSO baseSO = turretLibrary.GetInfo(TurretType.MachineGun);
            TurretStatsInstance inst = new(baseSO)
            {
                TurretType = TurretType.MachineGun,
                IsUnlocked = true
            };
            owned.Add(inst);
        }

        private void OnDestroy()
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
            foreach (TurretUnlockTableSO.Entry e in unlockTable.Entries
                         .Where(e => currentWave >= e.WaveToUnlock && unlockedTypes
                             .Add(e.Type)))
                changed = true;

            OnInventoryChanged?.Invoke();
            return changed;
        }

        public bool TryPurchase(TurretType type)
        {
            //if (!unlockedTypes.Contains(type)) return false; Used only with the wave requirement

            int countOwned = owned.Count(t => t.TurretType == type);
            if (countOwned >= 5)
                return false;

            ulong cost = GetCost(type, countOwned);

            if (GameManager.Instance.Money < cost)
                return false;
            GameManager.Instance.SpendMoney(cost);

            // create runtime copy from the original SO
            TurretInfoSO baseSO = turretLibrary.GetInfo(type);
            TurretStatsInstance inst = new(baseSO)
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
            TurretUnlockTableSO.Entry entry = unlockTable.Entries.First(e => e.Type == type);
            return entry.FirstCopyCost * (ulong)Mathf.Pow(2, Mathf.Max(0, currentOwned - 1));
        }

        public GameObject GetPrefab(TurretType t) => turretLibrary.GetPrefab(t);

        public TurretInfoSO GetInfoSO(TurretType type) => turretLibrary.GetInfo(type);

        // ---------- save / load ----------

        public TurretInventoryDTO ExportToDTO()
        {
            return new TurretInventoryDTO
            {
                Owned = owned,
                EquippedIds = TurretSlotManager.Instance.ExportEquipped(),
                UnlockedTypes = unlockedTypes.ToList(),
                SlotPurchased = TurretSlotManager.Instance.GetPurchasedFlags()
            };
        }

        public void ImportFromDTO(TurretInventoryDTO dto)
        {
            owned.Clear();
            owned.AddRange(dto.Owned ?? new List<TurretStatsInstance>());

            unlockedTypes.Clear();
            unlockedTypes.UnionWith(dto.UnlockedTypes ?? new List<TurretType>());

            if (TurretSlotManager.Instance != null)
            {
                TurretSlotManager.Instance.ImportEquipped(dto.EquippedIds);
                TurretSlotManager.Instance.ImportPurchasedFlags(dto.SlotPurchased);
            }
            else
            {
                pendingEquipped = dto.EquippedIds;
                pendingPurchased = dto.SlotPurchased;
            }
        }

    }
}