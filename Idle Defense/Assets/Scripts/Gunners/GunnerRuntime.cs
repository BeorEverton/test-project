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
}
