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
        public readonly HashSet<TurretType> unlockedTypes = new();
        [SerializeField] private TurretLibrarySO turretLibrary;   // assign the new asset

        public List<OwnedTurret> owned = new();
        public List<OwnedTurret> Owned => owned;

        [System.Serializable]
        public class OwnedTurret
        {
            public TurretType TurretType;
            public TurretStatsInstance Permanent;
            public TurretStatsInstance Runtime;
        }

        private List<int> pendingEquipped;
        private List<bool> pendingPurchased;

        private readonly Dictionary<TurretStatsInstance, GameObject> instanceToGO = new();

        private List<TurretStatsInstance> pendingRuntimeStats;

        public List<EquippedTurretDTO> EquippedTurrets; 


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
                //TurretSlotManager.Instance.ImportPurchasedFlags(pendingPurchased);
                pendingPurchased = null;
            }
            if (pendingRuntimeStats != null)
            {
                TurretSlotManager.Instance.ImportRuntimeStats(pendingRuntimeStats);
                pendingRuntimeStats = null;
            }

        }

        private void EnsureStarterTurret()
        {
            if (owned.Count > 0)
                return;

            TryUnlockByWave(0);
            if (!unlockedTypes.Contains(TurretType.MachineGun))
                return;

            TurretInfoSO baseSO = TurretLibrary.Instance.GetInfo(TurretType.MachineGun);
            var permanent = new TurretStatsInstance(baseSO)
            {
                TurretType = TurretType.MachineGun,
                IsUnlocked = true
            };
            var runtime = BaseTurret.CloneStatsWithoutLevels(permanent);
            owned.Add(new OwnedTurret
            {
                TurretType = TurretType.MachineGun,
                Permanent = permanent,
                Runtime = runtime
            });

            // Do not force-equip by default
            pendingEquipped = new List<int> { -1, -1, -1, -1, -1 };
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
            int countOwned = owned.Count(t => t.TurretType == type);
            if (countOwned >= 5) return false;

            ulong cost = GetCost(type, countOwned);
            if (!GameManager.Instance.TrySpendCurrency(Currency.Scraps, cost))
                return false;

            TurretInfoSO baseSO = TurretLibrary.Instance.GetInfo(type);
            var permanent = new TurretStatsInstance(baseSO)
            {
                TurretType = type,
                IsUnlocked = true
            };
            var runtime = BaseTurret.CloneStatsWithoutLevels(permanent);

            owned.Add(new OwnedTurret
            {
                TurretType = type,
                Permanent = permanent,
                Runtime = runtime
            });

            OnInventoryChanged?.Invoke();
            SaveGameManager.Instance.SaveGame();
            return true;
        }


        public ulong GetCost(TurretType type, int currentOwned)
        {
            TurretUnlockTableSO.Entry entry = unlockTable.Entries.First(e => e.Type == type);
            return entry.FirstCopyCost * (ulong)Mathf.Pow(2, Mathf.Max(0, currentOwned - 1));
        }

        public GameObject GetPrefab(TurretType t) => TurretLibrary.Instance.GetPrefab(t);

        public TurretInfoSO GetInfoSO(TurretType type) => TurretLibrary.Instance.GetInfo(type);

        public (Currency currency, ulong cost) GetCostAndCurrency(TurretType type, int owned)
        {
            Currency currency = Currency.Scraps;
            ulong cost = GetCost(type, owned); 

            return (currency, cost);
        }


        // ---------- save / load ----------

        public TurretInventoryDTO ExportToDTO()
        {
            // Persist both Permanent and Runtime for every owned turret.
            // NOTE: Requires TurretInventoryDTO to have matching fields for OwnedPermanent/OwnedRuntime
            // (or a composite DTO per owned entry). If your DTOs differ, mirror this split there.
            return new TurretInventoryDTO
            {
                OwnedPermanent = owned.Select(o => o.Permanent).ToList(),
                OwnedRuntime = owned.Select(o => o.Runtime).ToList(),
                OwnedTypes = owned.Select(o => o.TurretType).ToList(),

                EquippedIds = TurretSlotManager.Instance.ExportEquipped(),
                EquippedRuntimeStats = TurretSlotManager.Instance.ExportRuntimeStats(),
                EquippedTurrets = TurretSlotManager.Instance.ExportEquippedTurrets(),
                UnlockedTypes = unlockedTypes.ToList(),
                SlotPurchased = TurretSlotManager.Instance.GetPurchasedFlags()
            };
        }


        public void ImportFromDTO(TurretInventoryDTO dto)
        {
            owned.Clear();

            // Rebuild owned pairs from save
            var perm = dto.OwnedPermanent ?? new List<TurretStatsInstance>();
            var run = dto.OwnedRuntime ?? new List<TurretStatsInstance>();
            var types = dto.OwnedTypes ?? new List<TurretType>();

            int n = Mathf.Max(perm.Count, Mathf.Max(run.Count, types.Count));
            for (int i = 0; i < n; i++)
            {
                var p = i < perm.Count ? perm[i] : null;
                var r = i < run.Count ? run[i] : null;

                // If runtime missing, derive once from permanent; otherwise keep saved runtime.
                if (p != null && (r == null || r.TurretType != p.TurretType))
                    r = BaseTurret.CloneStatsWithoutLevels(p);

                var t = (i < types.Count) ? types[i] : (p != null ? p.TurretType : (r != null ? r.TurretType : TurretType.MachineGun));

                if (p != null) p.TurretType = t;
                if (r != null) r.TurretType = t;

                if (p != null || r != null)
                    owned.Add(new OwnedTurret { TurretType = t, Permanent = p, Runtime = r });
            }

            unlockedTypes.Clear();
            unlockedTypes.UnionWith(dto.UnlockedTypes ?? new List<TurretType>());

            if (TurretSlotManager.Instance != null)
            {
                if (dto.EquippedTurrets != null && dto.EquippedTurrets.Count > 0)
                {
                    // Full path still supported
                    TurretSlotManager.Instance.ImportEquippedTurrets(dto.EquippedTurrets);
                }
                else
                {
                    TurretSlotManager.Instance.ImportEquipped(dto.EquippedIds);
                    TurretSlotManager.Instance.ImportRuntimeStats(dto.EquippedRuntimeStats);
                }
            }
        }



        // Used to deactivate and activate object logic to replace destroy and instantiate

        public GameObject GetGameObjectForInstance(TurretStatsInstance stats)
        {
            instanceToGO.TryGetValue(stats, out var go);
            return go;
        }

        public void RegisterTurretInstance(TurretStatsInstance stats, GameObject go)
        {
            if (stats == null || go == null)
                return;

            if (!instanceToGO.ContainsKey(stats))
                instanceToGO.Add(stats, go);
            else
                instanceToGO[stats] = go;
        }


        public void UnregisterTurretInstance(TurretStatsInstance stats)
        {
            if (instanceToGO.ContainsKey(stats))
                instanceToGO.Remove(stats);
        }

        public void ClearUnusedTurrets()
        {
            var equippedStats = TurretSlotManager.Instance.GetEquippedStats()
                .Where(s => s != null)
                .ToHashSet();

            var keysToRemove = instanceToGO
                .Where(pair => !equippedStats.Contains(pair.Key))
                .ToList();

            foreach (var pair in keysToRemove)
            {
                if (pair.Value != null)
                    Destroy(pair.Value);

                instanceToGO.Remove(pair.Key);
            }

            // Extra cleanup for any lingering scene turrets (fallback)
            foreach (var obj in FindObjectsByType<BaseTurret>(FindObjectsSortMode.None))
            {
                if (!instanceToGO.ContainsValue(obj.gameObject))
                    Destroy(obj.gameObject);
            }
        }
        
        

    }
}