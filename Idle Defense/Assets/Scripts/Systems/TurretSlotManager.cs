using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Assets.Scripts.Turrets;
using Assets.Scripts.WaveSystem;
using Assets.Scripts.Systems.Save;   // if needed for BaseTurret refs

namespace Assets.Scripts.Systems
{
    public class TurretSlotManager : MonoBehaviour
    {
        public static TurretSlotManager I { get; private set; }

        // 5 equip slots
        private readonly TurretStatsInstance[] equipped = new TurretStatsInstance[5];

        private readonly SlotUnlock[] slotInfo =
        {
            new SlotUnlock(  1,      0),      // wave, cost
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
        void Awake() { if (I == null) I = this; }

        public bool IsUnlocked(int slot)
        {
            return Purchased(slot) &&
                   WaveManager.Instance.GetCurrentWaveIndex() >= slotInfo[slot].wave;
        }

        // try to pay and mark purchased
        public bool UnlockSlot(int slot)
        {
            var info = slotInfo[slot];
            if (WaveManager.Instance.GetCurrentWaveIndex() < info.wave) return false;

            if (!info.purchased)
            {
                if (GameManager.Instance.Money < info.cost) return false;
                GameManager.Instance.SpendMoney(info.cost);
                slotInfo[slot].purchased = true;
            }
            SaveGameManager.Instance.SaveGame();
            return true;
        }

        public bool Equip(int slot, TurretStatsInstance inst)
        {
            Debug.Log($"Equip: {inst.TurretType} into slot {slot}");
            equipped[slot] = inst;
            SaveGameManager.Instance.SaveGame();
            OnEquippedChanged?.Invoke(slot, inst);
            return true;
        }
        public void Unequip(int slot)
        {
            equipped[slot] = null;
            SaveGameManager.Instance.SaveGame();
            OnEquippedChanged?.Invoke(slot, null);
        }

        //  save helpers 
        public List<bool> GetPurchasedFlags() => slotInfo.Select(s => s.purchased).ToList();
        public void ImportPurchasedFlags(List<bool> flags)
        {
            for (int i = 0; i < flags.Count && i < slotInfo.Length; i++)
                slotInfo[i].purchased = flags[i];
        }

        public event Action<int, TurretStatsInstance> OnEquippedChanged;

        public List<int> ExportEquipped() =>
    equipped.Select(inst =>
        inst == null ? -1 : TurretInventoryManager.I.Owned.IndexOf(inst)
    ).ToList();

        // TurretSlotManager.cs   – put it under GetPurchasedFlags()

        public void ImportEquipped(List<int> ids)
        {
            for (int i = 0; i < ids.Count && i < equipped.Length; i++)
            {
                equipped[i] = (ids[i] >= 0 && ids[i] < TurretInventoryManager.I.Owned.Count)
                              ? TurretInventoryManager.I.Owned[ids[i]]
                              : null;
                OnEquippedChanged?.Invoke(i, equipped[i]);   // refresh visuals
            }
        }


    }
}
