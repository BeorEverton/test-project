using Assets.Scripts.Systems.Audio;
using Assets.Scripts.Systems.Save;
using Assets.Scripts.Turrets;
using Assets.Scripts.UI;   // if needed for BaseTurret refs
using Assets.Scripts.WaveSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts.Systems
{
    public class TurretSlotManager : MonoBehaviour
    {
        public static TurretSlotManager Instance { get; private set; }

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

        private void Awake() { if (Instance == null) Instance = this; else Destroy(gameObject); }

        public bool IsUnlocked(int slot)
        {
            return Purchased(slot) &&
                   WaveManager.Instance.GetCurrentWaveIndex() >= slotInfo[slot].wave;
        }

        // try to pay and mark purchased
        public bool UnlockSlot(int slot)
        {
            var info = slotInfo[slot];
            if (WaveManager.Instance.GetCurrentWaveIndex() < info.wave)
                return false;

            if (!info.purchased)
            {
                if (GameManager.Instance.Money < info.cost)
                    return false;
                GameManager.Instance.SpendMoney(info.cost);
                slotInfo[slot].purchased = true;
            }
            OnSlotUnlocked?.Invoke();
            SaveGameManager.Instance.SaveGame();
            return true;
        }

        public bool Equip(int slot, TurretStatsInstance inst)
        {
            AudioManager.Instance.Play("Click");
            UIManager.Instance.DeactivateRightPanels();
            UIManager.Instance.wallUpgradePanel.gameObject.SetActive(true); // to help the tutorial progression
            equipped[slot] = inst;
            OnEquippedChanged?.Invoke(slot, inst);
            SaveGameManager.Instance.SaveGame();
            return true;
        }
        public void Unequip(int slot)
        {
            equipped[slot] = null;
            SaveGameManager.Instance.SaveGame();
            OnEquippedChanged?.Invoke(slot, null);
        }

        public bool IsAnyTurretEquipped()
        {
            for (int i = 0; i < equipped.Length; i++)
            {
                if (equipped[i] != null)
                    return true;
            }
            return false;
        }
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
        public void ImportPurchasedFlags(List<bool> flags)
        {
            for (int i = 0; i < flags.Count && i < slotInfo.Length; i++)
                slotInfo[i].purchased = flags[i];
        }

        public event Action<int, TurretStatsInstance> OnEquippedChanged;

        public event Action OnSlotUnlocked;

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