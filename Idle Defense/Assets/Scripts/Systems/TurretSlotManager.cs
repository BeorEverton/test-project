using Assets.Scripts.SO;
using Assets.Scripts.Systems.Audio;
using Assets.Scripts.Systems.Save;
using Assets.Scripts.Turrets;
using Assets.Scripts.UI;   // if needed for BaseTurret refs
using Assets.Scripts.WaveSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;

namespace Assets.Scripts.Systems
{
    public class TurretSlotManager : MonoBehaviour
    {
        public static TurretSlotManager Instance { get; private set; }

        // 5 equip slots
        public TurretStatsInstance[] equipped = new TurretStatsInstance[5];
        private Dictionary<int, EquippedTurret> _equippedTurrets = new();

        private readonly Dictionary<int, TurretStatsInstance> _runtimeTempStats = new();

        private readonly SlotUnlock[] slotInfo =
        {
            new SlotUnlock(-1,0), // wave, cost
            new SlotUnlock(-1,0),
            new SlotUnlock(-1,0),
            new SlotUnlock(-1,0),
            new SlotUnlock(-1,0)
        };

        [Serializable]
        public struct SlotUnlock
        {
            public int wave;
            public ulong cost;
            public bool purchased;          // saved
            public SlotUnlock(int w, ulong c)
            { wave = w; cost = c; purchased = false; }
        }

        // public helpers 
        public int WaveRequirement(int slot) => slotInfo[slot].wave;
        public ulong BuyCost(int slot) => slotInfo[slot].cost;
        public bool Purchased(int slot) => true;// slotInfo[slot].purchased; CHANGED TO NOT HAVE SLOTS LOCKED AT START
        public TurretStatsInstance Get(int slot) => equipped[slot];

        private void Awake() { if (Instance == null) Instance = this; else Destroy(gameObject); }

        private void Start()
        {
            GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
            for (int i = 0; i < slotInfo.Length; i++)
                slotInfo[i].purchased = true;

            OnSlotUnlocked?.Invoke();

        }

        public bool IsUnlocked(int slot) => Purchased(slot) && WaveManager.Instance.GetCurrentWaveIndex() >= slotInfo[slot].wave;

        // try to pay and mark purchased
        public bool UnlockSlot(int slot)
        {
            var info = slotInfo[slot];
            if (WaveManager.Instance.GetCurrentWaveIndex() < info.wave)
                return false;

            if (!info.purchased)
            {
                if (!GameManager.Instance.TrySpendCurrency(Currency.BlackSteel, info.cost))
                    return false;

                slotInfo[slot].purchased = true;

                #region Analytics
                var eventData = new Dictionary<string, string>
                {
                    { "SlotNumber",        slot.ToString() },
                    { "PurchasedOnWave",   WaveManager.Instance.GetCurrentWaveIndex().ToString() }
                };

                // add one entry per equip slot (safe even when some are null)
                for (int i = 0; i < equipped.Length; i++)
                {
                    var pair = AnalyticsForSlot(i);
                    eventData.Add(pair.Key, pair.Value);
                }

                #endregion

            }
            OnSlotUnlocked?.Invoke();
            SaveGameManager.Instance.SaveGame();
            return true;
        }

        private KeyValuePair<string, string> AnalyticsForSlot(int index)
        {
            var inst = equipped[index];
            return inst != null
                ? new KeyValuePair<string, string>(inst.TurretType.ToString(),
                                                   inst.TotalLevel().ToString())
                : new KeyValuePair<string, string>($"Slot{index + 1}", "Locked");
        }

        public bool Equip(int slot, TurretStatsInstance inst)
        {
            AudioManager.Instance.Play("Click");
            UIManager.Instance.DeactivateRightPanels();
            UIManager.Instance.wallUpgradePanel.gameObject.SetActive(true);

            // Find the owned entry containing this stats instance (either perm or runtime)
            var ownedList = TurretInventoryManager.Instance.Owned;
            TurretInventoryManager.OwnedTurret entry =
                ownedList.FirstOrDefault(o => ReferenceEquals(o.Permanent, inst) || ReferenceEquals(o.Runtime, inst));

            if (entry == null)
                return false;

            // Use the persistent runtime reference — no cloning
            var runtime = entry.Runtime;
            var permanent = entry.Permanent;

            _equippedTurrets[slot] = new EquippedTurret
            {
                Permanent = permanent,
                Runtime = runtime
            };

            _runtimeTempStats[slot] = runtime;
            equipped[slot] = runtime;

            OnEquippedChanged?.Invoke(slot, runtime);
            SaveGameManager.Instance.SaveGame();
            return true;
        }

        public void Unequip(int slot)
        {
            VFX.RangeOverlayManager.Instance.Hide();

            // Do NOT destroy or detach the runtime stats; we only clear the slot mapping.
            if (_equippedTurrets.ContainsKey(slot))
                _equippedTurrets.Remove(slot);

            _runtimeTempStats.Remove(slot);
            equipped[slot] = null;

            OnEquippedChanged?.Invoke(slot, null);
            SaveGameManager.Instance.SaveGame();
        }

        /// <summary>
        /// Unequips everything. If autoEquipStarter is true, equips the starter
        /// (MachineGun) into slot 0 so the run starts immediately.
        /// </summary>
        public void UnequipAll(bool autoEquipStarter = true)
        {
            for (int i = 0; i < equipped.Length; i++)
                Unequip(i);

            if (!autoEquipStarter) return;

            var inv = Assets.Scripts.Systems.TurretInventoryManager.Instance;
            if (inv == null) return;

            // Ensure a starter exists (your ResetAll already calls EnsureStarterTurret)
            var starter = inv.GetOwnedByType(TurretType.MachineGun);
            if (starter == null) return;

            // Slot 0 by default (change if you prefer another slot)
            Equip(0, starter);
        }



        public bool IsAnyTurretEquipped() => equipped.Any(t => t != null);

        public int UnlockedSlotCount()
        {
            int count = 0;
            for (int i = 0; i < 5; i++)
            {
                if (Purchased(i))
                    count++;
            }
            return count;
        }

        //  save helpers 
        public List<bool> GetPurchasedFlags() => slotInfo.Select(s => s.purchased).ToList();

        public List<TurretStatsInstance> ExportRuntimeStats()
        {
            var list = new List<TurretStatsInstance>(5);
            for (int i = 0; i < equipped.Length; i++)
                list.Add(equipped[i]);
            return list;
        }


        // CALLED ON THE TURRET INVENTORY MANAGER TO LOAD SLOTS PURCHASED -> CHANGED BEHAVIOUR TO START WITH ALL PURCHASED
        public void ImportPurchasedFlags(List<bool> flags)
        {
            for (int i = 0; i < flags.Count && i < slotInfo.Length; i++)
                slotInfo[i].purchased = flags[i];
        }

        public event Action<int, TurretStatsInstance> OnEquippedChanged;

        public event Action OnSlotUnlocked;

        // Export indices of the *permanent* instances inside Owned.        
        public List<int> ExportEquipped()
        {
            var owned = TurretInventoryManager.Instance.Owned;
            var result = new List<int>(5);

            for (int i = 0; i < 5; i++)
            {
                if (_equippedTurrets.TryGetValue(i, out var et) && et?.Permanent != null)
                {
                    int idx = owned.FindIndex(o => ReferenceEquals(o.Permanent, et.Permanent) || ReferenceEquals(o.Runtime, et.Runtime));
                    result.Add(idx);
                }
                else
                {
                    result.Add(-1);
                }
            }
            return result;
        }



        // Convenience: enumerate permanent instances currently equipped (null filtered).
        public IEnumerable<TurretStatsInstance> GetEquippedPermanents()
        {
            foreach (var kv in _equippedTurrets)
                if (kv.Value?.Permanent != null)
                    yield return kv.Value.Permanent;
        }
        public void ImportEquipped(List<int> ids)
        {
            var owned = TurretInventoryManager.Instance.Owned;

            for (int i = 0; i < equipped.Length; i++)
            {
                TurretStatsInstance runtime = null;

                if (ids != null && i < ids.Count)
                {
                    int id = ids[i];
                    if (id >= 0 && id < owned.Count)
                    {
                        var entry = owned[id];

                        _equippedTurrets[i] = new EquippedTurret
                        {
                            Permanent = entry.Permanent,
                            Runtime = entry.Runtime
                        };

                        runtime = entry.Runtime;
                        _runtimeTempStats[i] = runtime;
                    }
                    else
                    {
                        _equippedTurrets.Remove(i);
                        _runtimeTempStats.Remove(i);
                    }
                }

                equipped[i] = runtime;
                OnEquippedChanged?.Invoke(i, runtime);
            }
        }


        public void ImportRuntimeStats(List<TurretStatsInstance> stats)
        {
            if (stats == null) return;
            
            for (int i = 0; i < stats.Count && i < equipped.Length; i++)
            {
                if (stats[i] == null) continue;
                var incoming = stats[i];

                // If the loaded runtime has a default/invalid type, pull from the Permanent mapping
                if ((int)incoming.TurretType == 0 && _equippedTurrets.TryGetValue(i, out var et) && et?.Permanent != null)
                    incoming.TurretType = et.Permanent.TurretType;

                equipped[i] = incoming;
                _runtimeTempStats[i] = incoming;

                OnEquippedChanged?.Invoke(i, incoming);

            }
        }

        private void HandleGameStateChanged(GameState newState)
        {
            // Do nothing to runtime on run start; runtime persists until you explicitly copy from permanent.
            if (newState != GameState.InGame)
                return;

            // Keep current runtime references intact.
            foreach (var kvp in _equippedTurrets)
            {
                int slot = kvp.Key;
                var et = kvp.Value;
                if (et?.Runtime != null)
                {
                    _runtimeTempStats[slot] = et.Runtime;
                    OnEquippedChanged?.Invoke(slot, et.Runtime);
                }
            }
        }


        public IEnumerable<TurretStatsInstance> GetEquippedStats()
        {
            return equipped.Where(e => e != null);
        }

        public List<EquippedTurretDTO> ExportEquippedTurrets()
        {
            var list = new List<EquippedTurretDTO>();
            var owned = TurretInventoryManager.Instance.Owned;

            for (int i = 0; i < equipped.Length; i++)
            {
                if (!_equippedTurrets.TryGetValue(i, out var et) || et?.Runtime == null || et.Permanent == null)
                    continue;

                int ownedIndex = owned.FindIndex(o =>
                    ReferenceEquals(o.Permanent, et.Permanent) || ReferenceEquals(o.Runtime, et.Runtime));

                var dto = new EquippedTurretDTO
                {
                    Type = et.Permanent.TurretType,
                    SlotIndex = i,
                    OwnedIndex = ownedIndex, // NEW: tie slot to Owned[]
                    PermanentStats = SaveDataDTOs.CreateTurretInfoDTO(et.Permanent),
                    RuntimeStats = SaveDataDTOs.CreateTurretInfoDTO(et.Runtime),
                    PermanentBase = SaveDataDTOs.CreateTurretBaseInfoDTO(et.Permanent),
                    RuntimeBase = SaveDataDTOs.CreateTurretBaseInfoDTO(et.Runtime),
                };

                list.Add(dto);
            }

            return list;
        }



        public void ImportEquippedTurrets(List<EquippedTurretDTO> dtos)
        {
            if (dtos == null || dtos.Count == 0)
            {
                Debug.LogWarning("[SlotMgr] No EquippedTurrets to import.");
                return;
            }

            var inv = TurretInventoryManager.Instance;
            var owned = inv.Owned;
            var usedOwned = new HashSet<int>();

            foreach (var dto in dtos)
            {
                int slot = Mathf.Clamp(dto.SlotIndex, 0, equipped.Length - 1);

                // 1) Preferred: bind to Owned[OwnedIndex]
                TurretInventoryManager.OwnedTurret entry = null;
                if (dto.OwnedIndex >= 0 && dto.OwnedIndex < owned.Count && !usedOwned.Contains(dto.OwnedIndex))
                {
                    entry = owned[dto.OwnedIndex];
                    usedOwned.Add(dto.OwnedIndex);
                }
                else
                {
                    // 2) Fallback (legacy saves without OwnedIndex): pick first same-type not yet used
                    for (int i = 0; i < owned.Count; i++)
                    {
                        if (!usedOwned.Contains(i) && owned[i].TurretType == dto.Type)
                        {
                            entry = owned[i];
                            usedOwned.Add(i);
                            break;
                        }
                    }
                }

                if (entry != null)
                {
                    _equippedTurrets[slot] = new EquippedTurret
                    {
                        Permanent = entry.Permanent,
                        Runtime = entry.Runtime
                    };

                    equipped[slot] = entry.Runtime;
                    _runtimeTempStats[slot] = entry.Runtime;

                    OnEquippedChanged?.Invoke(slot, entry.Runtime);
                }
                else
                {
                    // 3) Last resort: rebuild (should be rare, e.g., corrupted saves)
                    var perm = LoadDataDTOs.CreateTurretStatsInstance(dto.PermanentStats, dto.PermanentBase);
                    var run = LoadDataDTOs.CreateTurretStatsInstance(dto.RuntimeStats, dto.RuntimeBase);
                    perm.TurretType = dto.Type;
                    run.TurretType = dto.Type;

                    _equippedTurrets[slot] = new EquippedTurret { Permanent = perm, Runtime = run };
                    equipped[slot] = run;
                    _runtimeTempStats[slot] = run;

                    OnEquippedChanged?.Invoke(slot, run);
                }
            }
        }


        public bool TryGetEquippedTurret(int slot, out EquippedTurret equippedTurret)
        {
            return _equippedTurrets.TryGetValue(slot, out equippedTurret);
        }

        private void OnDestroy()
        {
            GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;

        }
    }
}

public class EquippedTurret
{
    public TurretStatsInstance Permanent;
    public TurretStatsInstance Runtime;
}
