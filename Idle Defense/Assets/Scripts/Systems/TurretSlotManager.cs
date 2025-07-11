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

        private readonly Dictionary<int, TurretStatsInstance> _runtimeTempStats = new();

        private readonly SlotUnlock[] slotInfo =
        {
            new SlotUnlock(  1,     0),      // wave, cost
            new SlotUnlock( 20,  5000),
            new SlotUnlock( 50, 20000),
            new SlotUnlock(120, 50000),
            new SlotUnlock(300,250000)
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
        public bool Purchased(int slot) => slotInfo[slot].purchased;
        public TurretStatsInstance Get(int slot) => equipped[slot];

        private void Awake() { if (Instance == null) Instance = this; else Destroy(gameObject); }

        private void Start()
        {
            GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
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

                AnalyticsManager.Instance.SendCustomEvent("SlotPurchased", eventData);
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
            UIManager.Instance.wallUpgradePanel.gameObject.SetActive(true); // to help the tutorial progression
            if (GameManager.Instance.CurrentGameState == GameState.InGame)
            {
                // Check if this turret is already used in another slot
                int previousSlot = -1;
                for (int i = 0; i < equipped.Length; i++)
                {
                    if (i != slot && equipped[i] == inst)
                    {
                        previousSlot = i;
                        break;
                    }
                }

                if (previousSlot != -1)
                {
                    // Move same turret instance to new slot
                    equipped[slot] = equipped[previousSlot];
                    equipped[previousSlot] = null;
                    _runtimeTempStats[slot] = _runtimeTempStats[previousSlot];
                    _runtimeTempStats.Remove(previousSlot);
                }
                else if (_runtimeTempStats.TryGetValue(slot, out var existingTemp))
                {
                    equipped[slot] = existingTemp;
                }
                else
                {
                    var clone = BaseTurret.CloneStatsWithoutLevels(inst);
                    equipped[slot] = clone;
                    _runtimeTempStats[slot] = clone;
                }
            }
            else
            {
                equipped[slot] = inst; // Management phase  assign permanent reference
            }

            OnEquippedChanged?.Invoke(slot, equipped[slot]);

            SaveGameManager.Instance.SaveGame();
            return true;
        }

        public void Unequip(int slot)
        {
            equipped[slot] = null;
            SaveGameManager.Instance.SaveGame();
            OnEquippedChanged?.Invoke(slot, null);
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
            foreach (var inst in equipped)
                list.Add(inst);

            for (int i = list.Count; i < 5; i++)
            {
                Debug.Log("slot " + i + " is turret " + list[i].TurretType + " damage is " + list[i].Damage);
            }                

            return list;
        }

        public void ImportPurchasedFlags(List<bool> flags)
        {
            for (int i = 0; i < flags.Count && i < slotInfo.Length; i++)
                slotInfo[i].purchased = flags[i];
        }

        public event Action<int, TurretStatsInstance> OnEquippedChanged;

        public event Action OnSlotUnlocked;

        public List<int> ExportEquipped() => equipped.Select(inst => inst == null ? -1 : TurretInventoryManager.Instance.Owned.IndexOf(inst)).ToList();

        // TurretSlotManager.cs   – put it under GetPurchasedFlags()

        public void ImportEquipped(List<int> ids)
        {
            for (int i = 0; i < ids.Count && i < equipped.Length; i++)
            {
                equipped[i] = (ids[i] >= 0 && ids[i] < TurretInventoryManager.Instance.Owned.Count)
                              ? TurretInventoryManager.Instance.Owned[ids[i]]
                              : null;
                OnEquippedChanged?.Invoke(i, equipped[i]);   // refresh visuals
            }
        }

        public void ImportRuntimeStats(List<TurretStatsInstance> stats)
        {
            if (stats == null) return;
            
            for (int i = 0; i < stats.Count && i < equipped.Length; i++)
            {
                if (stats[i] == null) continue;
                equipped[i] = stats[i];
                _runtimeTempStats[i] = stats[i];     // mark as runtime copy
                OnEquippedChanged?.Invoke(i, stats[i]);
                Debug.Log($"Importing runtime stats for slot {i}: {stats[i].TurretType} with damage {stats[i].Damage}");
            }
        }

        private void HandleGameStateChanged(GameState newState)
        {
            if (newState == GameState.InGame)
            {
                
                _runtimeTempStats.Clear();            // fresh run forget old Scrap buffs
                TurretInventoryManager.Instance.ClearUnusedTurrets();
            }
        }

        public IEnumerable<TurretStatsInstance> GetEquippedStats()
        {
            return equipped.Where(e => e != null);
        }

        public List<EquippedTurretDTO> ExportEquippedTurrets()
        {
            var list = new List<EquippedTurretDTO>();
            for (int i = 0; i < equipped.Length; i++)
            {
                var inst = equipped[i];
                if (inst == null) continue;

                BaseTurret baseTurret = TurretInventoryManager.Instance.GetGameObjectForInstance(inst)?.GetComponent<BaseTurret>();
                if (baseTurret == null)
                {
                    Debug.LogWarning($"[Export] Missing BaseTurret component for equipped instance at slot {i}");
                    continue;
                }

                var dto = new EquippedTurretDTO
                {
                    Type = inst.TurretType,
                    SlotIndex = i,
                    PermanentStats = SaveDataDTOs.CreateTurretInfoDTO(baseTurret.PermanentStats),
                    RuntimeStats = SaveDataDTOs.CreateTurretInfoDTO(baseTurret.RuntimeStats),
                    PermanentBase = SaveDataDTOs.CreateTurretBaseInfoDTO(baseTurret.PermanentStats),
                    RuntimeBase = SaveDataDTOs.CreateTurretBaseInfoDTO(baseTurret.RuntimeStats),
                };

                list.Add(dto);
                Debug.Log($"[Export] Saved turret {dto.Type} in slot {i} with runtime damage {baseTurret.RuntimeStats?.Damage}");
                Debug.Log($"[Export] Saved turret {dto.Type} in slot {i} with permanent damage {baseTurret.PermanentStats?.Damage}");
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

            foreach (var dto in dtos)
            {
                GameObject prefab = TurretInventoryManager.Instance.GetPrefab(dto.Type);
                GameObject go = Instantiate(prefab);
                BaseTurret turret = go.GetComponent<BaseTurret>();

                if (turret == null)
                {
                    Debug.LogError($"[SlotMgr] Prefab for {dto.Type} is missing BaseTurret.");
                    Destroy(go);
                    continue;
                }

                turret.PermanentStats = LoadDataDTOs.CreateTurretStatsInstance(dto.PermanentStats, dto.PermanentBase);
                turret.RuntimeStats = LoadDataDTOs.CreateTurretStatsInstance(dto.RuntimeStats, dto.RuntimeBase);

                // Assign stats to slot
                equipped[dto.SlotIndex] = turret.RuntimeStats;
                _runtimeTempStats[dto.SlotIndex] = turret.RuntimeStats;

                // Save mapping to allow reuse
                TurretInventoryManager.Instance.RegisterTurretInstance(turret.RuntimeStats, go);

                Debug.Log($"[SlotMgr] Imported turret {dto.Type} into slot {dto.SlotIndex} with Runtime damage {turret.RuntimeStats.Damage}");
                Debug.Log($"[SlotMgr] Imported turret {dto.Type} into slot {dto.SlotIndex} with PErmanent damage {turret.PermanentStats.Damage}");
            }

            // Trigger UI refresh
            for (int i = 0; i < equipped.Length; i++)
                OnEquippedChanged?.Invoke(i, equipped[i]);
        }


        private void OnDestroy()
        {
            GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;

        }

    }
}