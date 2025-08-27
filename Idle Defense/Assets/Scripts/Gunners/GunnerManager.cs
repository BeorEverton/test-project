using Assets.Scripts.Enemies;
using Assets.Scripts.Turrets;
using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Systems.Save;
using UnityEngine;

public class GunnerManager : MonoBehaviour
{
    public static GunnerManager Instance { get; private set; }

    [Header("Authoring")]
    [SerializeField] private List<GunnerSO> allGunners = new List<GunnerSO>();
    [SerializeField] private GameObject gunnerBillboardPrefab; // simple SpriteRenderer child
    [SerializeField] private Vector3 onTurretLocalOffset = new Vector3(0f, 0.6f, 0f);

    // Player state
    private readonly Dictionary<string, GunnerRuntime> runtimes = new();
    // slotIndex -> GunnerId
    private readonly Dictionary<int, string> slotToGunner = new();

    // cache: GunnerId -> spawned visual (under turret)
    private readonly Dictionary<string, GameObject> visuals = new();

    public event System.Action<int> OnSlotGunnerChanged;
    public event System.Action<int> OnSlotGunnerStatsChanged;


    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Init or load
        foreach (var so in allGunners)
        {
            if (!runtimes.ContainsKey(so.GunnerId))
                runtimes[so.GunnerId] = new GunnerRuntime(so);
        }        
    }

    private void Update()
    {
        // resolve quest timers → free up on completion
        foreach (var rt in runtimes.Values)
            rt.IsAvailableNow();
    }

    /* ===================== EQUIP / UNEQUIP ===================== */

    public bool IsEquipped(string gunnerId) => runtimes.TryGetValue(gunnerId, out var rt) && rt.EquippedSlot >= 0;

    public bool IsAvailable(string gunnerId)
    {
        if (!runtimes.TryGetValue(gunnerId, out var rt)) return false;
        return !IsEquipped(gunnerId) && rt.IsAvailableNow();
    }

    public bool EquipToSlot(string gunnerId, int slotIndex, Transform turretAnchor)
    {
        if (!runtimes.TryGetValue(gunnerId, out var rt)) return false;
        if (!rt.IsAvailableNow()) return false;

        // one per slot
        if (slotToGunner.TryGetValue(slotIndex, out var existing))
            UnequipFromSlot(slotIndex);

        // clear previous slot if any
        if (rt.EquippedSlot >= 0)
            slotToGunner.Remove(rt.EquippedSlot);

        slotToGunner[slotIndex] = gunnerId;
        rt.EquippedSlot = slotIndex;

        AttachEquippedGunnerVisual_Internal(gunnerId, turretAnchor);
        NotifyTurretOfChange(slotIndex);

        Save();
        OnSlotGunnerChanged?.Invoke(slotIndex);
        return true;
    }

    public void UnequipFromSlot(int slotIndex)
    {
        if (!slotToGunner.TryGetValue(slotIndex, out var gunnerId)) return;

        slotToGunner.Remove(slotIndex);
        if (runtimes.TryGetValue(gunnerId, out var rt))
            rt.EquippedSlot = -1;

        // destroy visual
        if (visuals.TryGetValue(gunnerId, out var go) && go != null)
        {
            Destroy(go);
            visuals.Remove(gunnerId);
        }

        NotifyTurretOfChange(slotIndex);
        Save();
        OnSlotGunnerChanged?.Invoke(slotIndex);

    }

    public string GetEquippedGunnerId(int slotIndex)
        => slotToGunner.TryGetValue(slotIndex, out var id) ? id : null;

    public GunnerRuntime GetRuntime(string gunnerId)
        => runtimes.TryGetValue(gunnerId, out var rt) ? rt : null;

    public void AttachEquippedGunnerVisual(int slotIndex, Transform turretAnchor)
    {
        var id = GetEquippedGunnerId(slotIndex);
        if (string.IsNullOrEmpty(id)) return;
        AttachEquippedGunnerVisual_Internal(id, turretAnchor);
    }

    private void AttachEquippedGunnerVisual_Internal(string gunnerId, Transform turretAnchor)
    {
        if (turretAnchor == null) return;
        var so = allGunners.FirstOrDefault(g => g.GunnerId == gunnerId);
        if (so == null) return;

        if (visuals.TryGetValue(gunnerId, out var existing) && existing != null)
        {
            existing.transform.SetParent(turretAnchor, false);
            existing.transform.localPosition = onTurretLocalOffset;
            existing.SetActive(true);
            ApplySprite(existing, so.OnTurretSprite);
            return;
        }

        GameObject card = gunnerBillboardPrefab != null
            ? Instantiate(gunnerBillboardPrefab, turretAnchor, false)
            : new GameObject("GunnerBillboard_" + gunnerId);

        card.transform.localPosition = onTurretLocalOffset;
        var sr = card.GetComponent<SpriteRenderer>();
        if (sr == null) sr = card.AddComponent<SpriteRenderer>();
        sr.sprite = so.OnTurretSprite;

        visuals[gunnerId] = card;
    }

    private static void ApplySprite(GameObject go, Sprite sprite)
    {
        var sr = go.GetComponent<SpriteRenderer>();
        if (sr == null) sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
    }

    private void NotifyTurretOfChange(int slotIndex)
    {
        // If your SlotWorldButton/scene structure keeps the turret under a known anchor,
        // you can find the BaseTurret and nudge it to rebuild effects if needed.
        // For now, turrets will query bonuses every Update and stay correct.
    }

    /* ===================== XP / LEVEL ===================== */

    public void GrantXpToAll(float xp)
    {
        foreach (var so in allGunners)
        {
            var rt = runtimes[so.GunnerId];
            rt.CurrentXp += Mathf.Max(0, xp);
            TryLevelUp(so, rt);
        }
        Save();
    }

    private void TryLevelUp(GunnerSO so, GunnerRuntime rt)
    {
        bool leveled = false;

        // Loop in case we cross multiple levels at once
        while (rt.CurrentXp >= so.XpRequiredForLevel(rt.Level))
        {
            float need = so.XpRequiredForLevel(rt.Level);
            rt.CurrentXp -= need;
            rt.Level++;
            rt.UnspentSkillPoints += so.SkillPointsPerLevel;
            leveled = true;

            // Apply level-based unlocks
            for (int i = 0; i < so.LevelUnlocks.Count; i++)
            {
                var group = so.LevelUnlocks[i];
                if (group.Level == rt.Level)
                {
                    for (int k = 0; k < group.Unlocks.Count; k++)
                        rt.Unlocked.Add(group.Unlocks[k]);
                }
            }
        }

        if (leveled)
        {
            Save();
            OnSlotGunnerStatsChanged?.Invoke(rt.EquippedSlot);

        }
    }


    /* ===================== QUESTS ===================== */

    public bool SendOnQuest(string gunnerId, TimeSpan duration)
    {
        if (!runtimes.TryGetValue(gunnerId, out var rt)) return false;
        if (IsEquipped(gunnerId)) return false;
        if (!rt.IsAvailableNow()) return false;

        rt.IsOnQuest = true;
        rt.QuestEndUnixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + (long)duration.TotalSeconds;
        Save();
        return true;
    }

    /* ===================== EFFECTIVE STATS ===================== */

    public GunnerBonus ComputeBonusForSlot(int slotIndex)
    {
        var bonus = new GunnerBonus();
        if (!slotToGunner.TryGetValue(slotIndex, out var id)) return bonus;
        if (!runtimes.TryGetValue(id, out var rt)) return bonus;

        var so = allGunners.FirstOrDefault(g => g.GunnerId == id);
        if (so == null) return bonus;

        // base → only if unlocked
        if (rt.Unlocked.Contains(GunnerStatKey.Damage)) bonus.Damage += so.BaseDamage;
        if (rt.Unlocked.Contains(GunnerStatKey.FireRate)) bonus.FireRate += so.BaseFireRate;
        if (rt.Unlocked.Contains(GunnerStatKey.Range)) bonus.Range += so.BaseRange;
        if (rt.Unlocked.Contains(GunnerStatKey.PercentBonusDamagePerSec)) bonus.PercentBonusDamagePerSec += so.BasePercentBonusDamagePerSec;
        if (rt.Unlocked.Contains(GunnerStatKey.SlowEffect)) bonus.SlowEffect += so.BaseSlowEffect;

        if (rt.Unlocked.Contains(GunnerStatKey.CriticalChance)) bonus.CriticalChance += so.BaseCriticalChance;
        if (rt.Unlocked.Contains(GunnerStatKey.CriticalDamageMultiplier)) bonus.CriticalDamageMultiplier += so.BaseCriticalDamage;
        if (rt.Unlocked.Contains(GunnerStatKey.KnockbackStrength)) bonus.KnockbackStrength += so.BaseKnockback;
        if (rt.Unlocked.Contains(GunnerStatKey.SplashDamage)) bonus.SplashDamage += so.BaseSplash;
        if (rt.Unlocked.Contains(GunnerStatKey.PierceChance)) bonus.PierceChance += so.BasePierceChance;
        if (rt.Unlocked.Contains(GunnerStatKey.PierceDamageFalloff)) bonus.PierceDamageFalloff += so.BasePierceFalloff;
        if (rt.Unlocked.Contains(GunnerStatKey.ArmorPenetration)) bonus.ArmorPenetration += so.BaseArmorPenetration;

        return bonus;
    }

    /* Convenience used by BaseTurret */
    public void ApplyTo(TurretStatsInstance baseStats, int slotIndex, TurretStatsInstance intoScratch)
    {
        // 1) copy
        CopyStats(baseStats, intoScratch);

        // If no gunner in this slot, we are done
        if (!slotToGunner.TryGetValue(slotIndex, out var gunnerId) || !runtimes.ContainsKey(gunnerId))
            return;

        // 2) add bonuses
        var b = ComputeBonusForSlot(slotIndex);
        intoScratch.Damage += b.Damage;
        intoScratch.FireRate += b.FireRate;
        intoScratch.Range += b.Range;
        intoScratch.PercentBonusDamagePerSec += b.PercentBonusDamagePerSec;
        intoScratch.SlowEffect += b.SlowEffect;
        intoScratch.CriticalChance += b.CriticalChance;
        intoScratch.CriticalDamageMultiplier += b.CriticalDamageMultiplier;
        intoScratch.KnockbackStrength += b.KnockbackStrength;
        intoScratch.SplashDamage += b.SplashDamage;
        intoScratch.PierceChance += b.PierceChance;
        intoScratch.PierceDamageFalloff += b.PierceDamageFalloff;
        intoScratch.ArmorPenetration += b.ArmorPenetration;
    }

    private static void CopyStats(TurretStatsInstance src, TurretStatsInstance dst)
    {
        // shallow copy every runtime field you actually read during combat.
        // For brevity, we copy the ones your BaseTurret uses frequently.
        dst.IsUnlocked = src.IsUnlocked;
        dst.TurretType = src.TurretType;

        dst.Damage = src.Damage;
        dst.FireRate = src.FireRate;
        dst.Range = src.Range;
        dst.RotationSpeed = src.RotationSpeed;
        dst.AngleThreshold = src.AngleThreshold;

        dst.CriticalChance = src.CriticalChance;
        dst.CriticalDamageMultiplier = src.CriticalDamageMultiplier;

        dst.KnockbackStrength = src.KnockbackStrength;
        dst.SplashDamage = src.SplashDamage;
        dst.PierceChance = src.PierceChance;
        dst.PierceDamageFalloff = src.PierceDamageFalloff;

        dst.PercentBonusDamagePerSec = src.PercentBonusDamagePerSec;
        dst.SlowEffect = src.SlowEffect;
        dst.CanHitFlying = src.CanHitFlying;
        dst.ArmorPenetration = src.ArmorPenetration;

        // patterns that matter to ExecuteAttack can also be mirrored if needed
        dst.BounceCount = src.BounceCount;
        dst.BounceRange = src.BounceRange;
        dst.BounceDelay = src.BounceDelay;
        dst.BounceDamagePct = src.BounceDamagePct;

        dst.ConeAngle = src.ConeAngle;
        dst.ExplosionDelay = src.ExplosionDelay;
    }

    /* ===================== ENEMY XP HOOK ===================== */

    public void OnEnemyKilled(float xpReward = 1f)
    {
        GrantXpToAll(xpReward);
    }

    public void OnEnemySpawned(Enemy enemy)
    {
        enemy.OnDeath += HandleEnemyDeathForXp;
    }

    private void HandleEnemyDeathForXp(object sender, System.EventArgs e)
    {
        OnEnemyKilled(1f);
    }

    /* ===================== SAVE / LOAD ===================== */
    private void Save()
    {
        if (SaveGameManager.Instance != null)
            SaveGameManager.Instance.SaveGame();
    }

    public GunnerInventoryDTO ExportToDTO()
    {
        var dto = new GunnerInventoryDTO();

        // runtimes
        foreach (var kvp in runtimes)
        {
            var rt = kvp.Value;
            dto.Runtimes.Add(new GunnerRuntimeDTO
            {
                Id = kvp.Key,
                CurHP = rt.CurrentHealth,
                MaxHP = rt.MaxHealth,
                Level = rt.Level,
                Xp = rt.CurrentXp,
                Points = rt.UnspentSkillPoints,
                Unlocked = rt.Unlocked.Select(u => (int)u).ToArray(),
                OnQuest = rt.IsOnQuest,
                QuestEnd = rt.QuestEndUnixTime,
                EquippedSlot = rt.EquippedSlot
            });
        }

        // slot map
        foreach (var kvp in slotToGunner)
            dto.SlotMap.Add(new SlotGunnerDTO { SlotIndex = kvp.Key, GunnerId = kvp.Value });

        return dto;
    }

    public void ImportFromDTO(GunnerInventoryDTO dto)
    {
        if (dto == null) return;

        // restore runtimes
        foreach (var r in dto.Runtimes)
        {
            if (!runtimes.TryGetValue(r.Id, out var rt))
            {
                var so = allGunners.FirstOrDefault(g => g.GunnerId == r.Id);
                if (so == null) continue;
                rt = new GunnerRuntime(so);
                runtimes[r.Id] = rt;
            }

            rt.CurrentHealth = r.CurHP;
            rt.MaxHealth = r.MaxHP;
            rt.Level = r.Level;
            rt.CurrentXp = r.Xp;
            rt.UnspentSkillPoints = r.Points;
            rt.Unlocked = new HashSet<GunnerStatKey>(r.Unlocked.Select(i => (GunnerStatKey)i));
            rt.IsOnQuest = r.OnQuest;
            rt.QuestEndUnixTime = r.QuestEnd;
            rt.EquippedSlot = r.EquippedSlot;
        }

        slotToGunner.Clear();
        for (int i = 0; i < dto.SlotMap.Count; i++)
        {
            var s = dto.SlotMap[i];
            slotToGunner[s.SlotIndex] = s.GunnerId;
        }
    }

}
