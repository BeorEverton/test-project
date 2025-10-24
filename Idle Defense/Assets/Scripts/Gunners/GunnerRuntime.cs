using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class GunnerRuntime
{
    public string GunnerId;
    public float CurrentHealth;
    public float MaxHealth;
    public int Level = 1;
    public float CurrentXp = 0f;
    public int UnspentSkillPoints = 0;
    public HashSet<GunnerStatKey> Unlocked = new HashSet<GunnerStatKey>();

    // Availability
    public bool IsOnQuest = false;
    public long QuestEndUnixTime = 0; // seconds

    // Equipped state (by turret slot index). -1 = not equipped
    public int EquippedSlot = -1;

    // Limit Break
    public float LimitBreakCurrent = 0f;
    public float LimitBreakMax => Mathf.Max(1f, MaxHealth * 0.5f);

    public bool IsDead => CurrentHealth <= 0f;

    [Serializable]
    public struct GunnerStatLevel
    {
        public GunnerStatKey Key;
        public int Level;
    }

    public List<GunnerStatLevel> UpgradeLevels = new List<GunnerStatLevel>();

    public int GetUpgradeLevel(GunnerStatKey key)
    {
        for (int i = 0; i < UpgradeLevels.Count; i++)
            if (UpgradeLevels[i].Key == key) return UpgradeLevels[i].Level;
        return 0;
    }

    public GunnerRuntime() { }
    public GunnerRuntime(GunnerSO so)
    {
        GunnerId = so.GunnerId;
        MaxHealth = Mathf.Max(1f, so.BaseHealth);
        CurrentHealth = MaxHealth;
        Unlocked = new HashSet<GunnerStatKey>(so.StartingUnlocked);
    }

    public bool IsAvailableNow()
    {
        if (!IsOnQuest) return true;
        if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() >= QuestEndUnixTime)
        {
            IsOnQuest = false;
            QuestEndUnixTime = 0;
            return true;
        }
        return false;
    }

    // Returns true if the gunner died on this call.
    public bool TakeDamage(float amount, out float actualDamage)
    {
        if (IsDead)
        {
            actualDamage = 0f;
            return false;
        }

        actualDamage = Mathf.Max(0f, amount);
        float newHp = CurrentHealth - actualDamage;
        if (newHp < 0f)
        {
            actualDamage = CurrentHealth; // overflow canceled
            newHp = 0f;
        }
        CurrentHealth = newHp;

        // Fill limit break by the exact damage amount (clamped to cap)
        LimitBreakCurrent = Mathf.Min(LimitBreakCurrent + actualDamage, LimitBreakMax);

        if (CurrentHealth <= 0f)
        {
            ResetLimitBreak();
        }

        return CurrentHealth <= 0f;
    }

    public float Heal(float amount)
    {
        float before = CurrentHealth;
        CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + Mathf.Max(0f, amount));
        return CurrentHealth - before;
    }

    public void ResetLimitBreak() => LimitBreakCurrent = 0f;

    public void AddUpgradeLevel(GunnerStatKey key, int delta)
    {
        for (int i = 0; i < UpgradeLevels.Count; i++)
            if (UpgradeLevels[i].Key == key)
            {
                var p = UpgradeLevels[i];
                p.Level = Mathf.Max(0, p.Level + delta);
                UpgradeLevels[i] = p;
                return;
            }
        UpgradeLevels.Add(new GunnerStatLevel { Key = key, Level = Mathf.Max(0, delta) });
    }

}
