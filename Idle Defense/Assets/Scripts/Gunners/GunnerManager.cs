 using Assets.Scripts.Enemies;
using Assets.Scripts.Systems;
using Assets.Scripts.Systems.Save;
using Assets.Scripts.Turrets;
using Assets.Scripts.UI;
using Assets.Scripts.WaveSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Assets.Scripts.Enemies.Enemy;

public class GunnerManager : MonoBehaviour
{
    public static GunnerManager Instance { get; private set; }

    [Header("Authoring")]
    [SerializeField] private GunnerUnlockTableSO unlockTable;

    // Built from unlockTable at runtime for fast access
    private readonly List<GunnerSO> allGunners = new();
    private readonly Dictionary<string, GunnerSO> soById = new();

    [SerializeField] private GameObject gunnerBillboardPrefab; // simple SpriteRenderer child
    [SerializeField] private Vector3 onTurretLocalOffset = new Vector3(0f, 0.6f, 0f);

    [Header("Starter")]
    [SerializeField, Tooltip("Preferred starter gunner kept across prestiges.")]
    public GunnerSO preferredStarterGunner;

    [SerializeField] private int starterDefaultSlotIndex = 2;

    public int StarterSlotIndex => starterDefaultSlotIndex;

    // Player state
    private readonly Dictionary<string, GunnerRuntime> runtimes = new();
    // slotIndex -> GunnerId
    private readonly Dictionary<int, string> slotToGunner = new();

    // cache: GunnerId -> spawned visual (under turret)
    private readonly Dictionary<string, GameObject> visuals = new();

    // slot -> turret anchor (used to get world X of the gunner)
    private readonly Dictionary<int, Transform> slotAnchors = new();

    // bars by gunner id
    private readonly Dictionary<string, DualPhaseBarUI> healthBarByGunner = new();
    private readonly Dictionary<string, DualPhaseBarUI> limitBarByGunner = new();
    private readonly Dictionary<string, GunnerBillboardBinding> billboardByGunner = new();

    // Ownership (first copy)
    private readonly HashSet<string> ownedGunners = new();

    // External listeners (UI, etc.)
    public event System.Action OnRosterChanged;

    // event if all equipped gunners die like OnWaveFailed
    public event Action OnAllEquippedGunnersDead;

    public event Action<int> OnSlotGunnerChanged;
    public event Action<int> OnSlotGunnerStatsChanged;

    // slotIndex -> parked gunner id (slot has NO turret; gunner is hidden and available in UI)
    private readonly Dictionary<int, string> parkedSlotToGunner = new();

    // Helpers for the Limit Break    
    public GunnerSO GetSO(string gunnerId) => (soById.TryGetValue(gunnerId, out var so) ? so : null);

    // 3D model instances and “docked” state
    private readonly Dictionary<string, GunnerModelBinding> modelByGunner = new();
    private readonly HashSet<string> dockedGunners = new(); // bonuses apply only when docked

    public bool TryGetEquippedRuntime(int slotIndex, out GunnerRuntime rt)
    {
        rt = null;
        var id = GetEquippedGunnerId(slotIndex);
        if (string.IsNullOrEmpty(id)) return false;
        rt = GetRuntime(id);
        return rt != null;
    }

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Init or load
        // Build registry from unlock table so authoring is centralized
        allGunners.Clear();
        soById.Clear();

        if (unlockTable != null)
        {
            foreach (var e in unlockTable.Entries)
            {
                if (!e.Gunner) continue;
                var so = e.Gunner;
                if (soById.ContainsKey(so.GunnerId)) continue; // avoid dupes

                soById[so.GunnerId] = so;
                allGunners.Add(so);

                if (!runtimes.ContainsKey(so.GunnerId))
                    runtimes[so.GunnerId] = new GunnerRuntime(so);
            }
        }
        else
        {
            Debug.LogWarning("[GunnerManager] No unlock table assigned; manager will be empty.");
        }
    }

    private void Update()
    {
        // resolve quest timers → free up on completion
        foreach (var rt in runtimes.Values)
            rt.IsAvailableNow();
    }

    private void Start()
    {
        if (WaveManager.Instance != null)
            WaveManager.Instance.OnWaveStarted += Wave_OnWaveStarted;
    }

    private void OnDisable()
    {
        if (WaveManager.Instance != null)
            WaveManager.Instance.OnWaveStarted -= Wave_OnWaveStarted;
    }

    private void Wave_OnWaveStarted(object sender, WaveManager.OnWaveStartedEventArgs e)
    {
        // Heal all gunners at the start of each wave
        HealAllGunners(resetLimitBreak: false);
    }

    /* ===================== NEW GAME ===================== */
    /// <summary>Grants free ownership for each gunner id (no currency), safe to call multiple times.</summary>
    public void GrantOwnershipFree(IEnumerable<GunnerSO> starters)
    {
        if (starters == null) return;
        foreach (var so in starters)
        {
            if (so == null) continue;
            ownedGunners.Add(so.GunnerId);
            if (!runtimes.ContainsKey(so.GunnerId))
                runtimes[so.GunnerId] = new GunnerRuntime(so);
        }
        Save();
        OnRosterChanged?.Invoke();
    }

    /// <summary>Change the preferred starter gunner at runtime (e.g., from selection UI) and persist it.</summary>
    public void SetPreferredStarter(GunnerSO so)
    {
        preferredStarterGunner = so;
        if (so != null)
            ownedGunners.Add(so.GunnerId); // grant ownership (free)

        // Persist through your normal save file, not PlayerPrefs
        Save();
        OnRosterChanged?.Invoke();
    }


    /* ===================== EQUIP / UNEQUIP ===================== */

    public bool IsEquipped(string gunnerId) => runtimes.TryGetValue(gunnerId, out var rt) && rt.EquippedSlot >= 0;

    public bool IsAvailable(string gunnerId)
    {
        if (!IsOwned(gunnerId)) return false;
        if (!runtimes.TryGetValue(gunnerId, out var rt)) return false;
        if (rt != null & rt.IsDead) return false;
        return !IsEquipped(gunnerId) && rt.IsAvailableNow();
    }


    public bool EquipToSlot(string gunnerId, int slotIndex, Transform turretAnchor)
    {
        if (!runtimes.TryGetValue(gunnerId, out var rt)) return false;
        if (!rt.IsAvailableNow()) return false;

        // one per slot
        if (slotToGunner.TryGetValue(slotIndex, out var existing))
            UnequipFromSlot(slotIndex);

        // gunner may be parked somewhere: clear that mapping
        ClearParkForGunner(gunnerId);

        // clear previous active slot
        if (rt.EquippedSlot >= 0)
            slotToGunner.Remove(rt.EquippedSlot);

        slotToGunner[slotIndex] = gunnerId;
        rt.EquippedSlot = slotIndex;

        AttachEquippedGunnerVisual_Internal(gunnerId, turretAnchor, slotIndex);

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

        // 3D model: run out, then destroy
        if (modelByGunner.TryGetValue(gunnerId, out var model) && model != null)
        {
            Vector3 exit = slotAnchors.TryGetValue(slotIndex, out var t) && t != null
                ? t.position + new Vector3(0f, 0f, -8f)
                : model.transform.position + new Vector3(0f, 0f, -8f);

            dockedGunners.Remove(gunnerId);
            model.RunOut(exit, 0.1f, onExit: () =>
            {
                modelByGunner.Remove(gunnerId);
            });
        }

        // Bars/billboard cleanup (keep order)
        if (visuals.TryGetValue(gunnerId, out var go) && go != null)
        {
            Destroy(go);
            visuals.Remove(gunnerId);
        }
        healthBarByGunner.Remove(gunnerId);
        limitBarByGunner.Remove(gunnerId);
        slotAnchors.Remove(slotIndex);
        billboardByGunner.Remove(gunnerId);


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
        AttachEquippedGunnerVisual_Internal(id, turretAnchor, slotIndex);

    }

    private void AttachEquippedGunnerVisual_Internal(string gunnerId, Transform turretAnchor, int slotIndex)
    {
        if (turretAnchor == null) return;
        var so = allGunners.FirstOrDefault(g => g.GunnerId == gunnerId);
        if (so == null) return;

        slotAnchors[slotIndex] = turretAnchor;

        // 1) Billboard (bars only). No portrait sprite.
        GameObject card;
        if (!visuals.TryGetValue(gunnerId, out card) || card == null)
        {
            card = gunnerBillboardPrefab != null
                ? Instantiate(gunnerBillboardPrefab, turretAnchor, false)
                : new GameObject("GunnerBillboard_" + gunnerId);

            card.transform.localPosition = onTurretLocalOffset;

            // Ensure no portrait is shown
            var sr = card.GetComponent<SpriteRenderer>() ?? card.AddComponent<SpriteRenderer>();
            sr.sprite = null;
            sr.enabled = false; // just the bars/UI

            visuals[gunnerId] = card;
        }
        else
        {
            card.transform.SetParent(turretAnchor, false);
            card.transform.localPosition = onTurretLocalOffset;
            card.SetActive(true);
        }

        var rt = runtimes.TryGetValue(gunnerId, out var rtt) ? rtt : null;
        var bind = card.GetComponent<GunnerBillboardBinding>();
        if (bind != null && rt != null)
        {
            bind.InitializeFromRuntime(rt, GetSkillIcon(so));
            if (bind.HealthBar) healthBarByGunner[gunnerId] = bind.HealthBar;
            if (bind.LimitBreakBar) limitBarByGunner[gunnerId] = bind.LimitBreakBar;
            billboardByGunner[gunnerId] = bind;
        }

        // 2) Spawn / move 3D model and run into place
        dockedGunners.Remove(gunnerId); // will become active once docked

        // Compute an off-screen start (in front of the player, positive -Z)
        Vector3 start = turretAnchor.position + new Vector3(0f, 0f, -8f);
        Vector3 exit = turretAnchor.position + new Vector3(0f, 0f, -8f);

        GunnerModelBinding model;
        if (!modelByGunner.TryGetValue(gunnerId, out model) || model == null)
        {
            if (so.ModelPrefab == null)
            {
                Debug.LogWarning($"[GunnerManager] Gunner {gunnerId} has no ModelPrefab assigned. Using billboard-only fallback.");
                return;
            }

            var go = Instantiate(so.ModelPrefab, start, Quaternion.LookRotation(turretAnchor.position - start));
            model = go.GetComponent<GunnerModelBinding>();
            if (model == null) model = go.AddComponent<GunnerModelBinding>();
            model.Initialize(so.RunSpeed, so.LimitBreakReadyVfx);
            modelByGunner[gunnerId] = model;

            // If the billboard has a binding, link it so LB VFX are synced
            if (bind != null) bind.ModelBinding = model;
        }
        else
        {
            model.transform.position = start;
            model.gameObject.SetActive(true);
        }

        // Begin run-in
        model.RunTo(turretAnchor, so.ArrivalSnapDistance, onArrive: () =>
        {
            // Snap and offset
            model.transform.position = turretAnchor.position + so.ModelOffsetOnTurret;
            dockedGunners.Add(gunnerId);
            // Recompute turret effective stats now that the gunner is actually present
            OnSlotGunnerStatsChanged?.Invoke(slotIndex);
        });
    }


    private static void ApplySprite(GameObject go, Sprite sprite)
    {
        var sr = go.GetComponent<SpriteRenderer>();
        if (sr == null) sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
    }

    private void NotifyTurretOfChange(int slotIndex)
    {
        // For now, turrets query bonuses every Update and stay correct.
    }

    public void NotifyTurretAttack(int slotIndex)
    {
        if (!slotToGunner.TryGetValue(slotIndex, out var gid)) return;
        if (modelByGunner.TryGetValue(gid, out var model) && model != null)
            model.PlayAttack();
    }


    // Used for the chat system
    /// <summary>
    /// Returns a unique list of all currently equipped GunnerSOs.    
    /// </summary>
    public List<GunnerSO> GetAllEquippedGunners()
    {
        var list = new List<GunnerSO>();
        foreach (var kv in slotToGunner)
        {
            var so = GetSO(kv.Value);
            if (so != null && !list.Contains(so))
                list.Add(so);
        }
        return list;
    }

    /// <summary>All gunners that are not equipped and available (i.e., not on a quest, not dead).</summary>
    public List<GunnerSO> GetAllIdleGunners()
    {
        var list = new List<GunnerSO>();
        foreach (var so in allGunners)
        {
            if (so == null) continue;
            if (IsAvailable(so.GunnerId)) // already checks not equipped + IsAvailableNow()
                list.Add(so);
        }
        return list;
    }

    // Used for the 'parked' gunner slots (no turret; just in UI)
    /// <summary>Called by slot UI when a turret presence changes.</summary>
    public void NotifyTurretPresent(int slotIndex, Transform turretAnchor, bool hasTurret)
    {
        // Always update/remember the latest anchor so run-in/out has a valid target.
        if (turretAnchor != null)
            slotAnchors[slotIndex] = turretAnchor;

        if (hasTurret)
        {
            // Auto-restore parked gunner if still available and not equipped elsewhere
            if (parkedSlotToGunner.TryGetValue(slotIndex, out var gid))
            {
                if (IsAvailable(gid))
                {
                    EquipToSlot(gid, slotIndex, turretAnchor);
                }
                parkedSlotToGunner.Remove(slotIndex);
            }
        }
        else
        {
            // If a gunner is currently on this slot, park them (will also run out the 3D model)
            ParkGunnerFromSlot(slotIndex);
        }
    }


    /// <summary>Move the currently equipped gunner on this slot into a hidden/parked state.</summary>
    private void ParkGunnerFromSlot(int slotIndex)
    {
        if (!slotToGunner.TryGetValue(slotIndex, out var gunnerId)) return;

        // remove from active mapping (so enemies cannot pick them)
        slotToGunner.Remove(slotIndex);

        // mark runtime as not equipped, so UI considers them free
        if (runtimes.TryGetValue(gunnerId, out var rt))
            rt.EquippedSlot = -1;

        // remember who lived here (so we can auto-restore when a new turret appears)
        parkedSlotToGunner[slotIndex] = gunnerId;

        // 3D model: play run-out then destroy & clear docked state
        if (modelByGunner.TryGetValue(gunnerId, out var model) && model != null)
        {
            Vector3 exit = slotAnchors.TryGetValue(slotIndex, out var t) && t != null
                ? t.position + new Vector3(0f, 0f, -8f)
                : model.transform.position + new Vector3(0f, 0f, -8f);

            dockedGunners.Remove(gunnerId);
            model.RunOut(exit, 0.1f, onExit: () =>
            {
                modelByGunner.Remove(gunnerId);
            });
        }

        // remove billboard/card UI (health/limit bars)
        if (visuals.TryGetValue(gunnerId, out var go) && go != null)
        {
            Destroy(go);
            visuals.Remove(gunnerId);
        }
        healthBarByGunner.Remove(gunnerId);
        limitBarByGunner.Remove(gunnerId);
        billboardByGunner.Remove(gunnerId);

        Save();
        OnSlotGunnerChanged?.Invoke(slotIndex);
        OnRosterChanged?.Invoke();
    }


    /// <summary>Clear any parked mapping for this gunner (used when equipping them elsewhere).</summary>
    private void ClearParkForGunner(string gunnerId)
    {
        int? foundSlot = null;
        foreach (var kv in parkedSlotToGunner)
        {
            if (kv.Value == gunnerId) { foundSlot = kv.Key; break; }
        }
        if (foundSlot.HasValue) parkedSlotToGunner.Remove(foundSlot.Value);
    }

    /* ===================== BUY / UNLOCK ===================== */
    private bool TryGetEntry(string id, out GunnerUnlockTableSO.Entry e)
    {
        if (unlockTable != null && unlockTable.TryGet(id, out e))
            return true;

        e = default;
        return false;
    }

    public bool RequiresPrestigeUnlock(string id)
        => TryGetEntry(id, out var e) && e.RequirePrestigeUnlock;

    public bool IsOwned(string id) => ownedGunners.Contains(id);
    public ulong GetFirstCopyCost(string id)
    {
        return TryGetEntry(id, out var e) ? e.unlockCost : 0UL;
    }

    public Currency GetPurchaseCurrency(string id)
    {
        if (TryGetEntry(id, out var e)) return e.unlockCurrency;
        return Currency.BlackSteel; 
    }

    public bool TryPurchaseGunner(string id)
    {
        if (IsOwned(id)) return false;                 // already own
        if (!IsPurchasableNow(id)) return false;       // wave/prestige gates

        ulong cost = GetFirstCopyCost(id);
        if (cost > 0)
        {
            var currency = GetPurchaseCurrency(id);
            if (!GameManager.Instance.TrySpendCurrency(currency, cost))
                return false;
        }

        ownedGunners.Add(id);
        Save();
        OnRosterChanged?.Invoke();
        return true;
    }

    public bool IsPurchasableNow(string id)
    {
        if (!TryGetEntry(id, out var e)) return true; // default permissive

        // wave gate
        int curWave = WaveManager.Instance != null ? WaveManager.Instance.GetCurrentWaveIndex() : 0;
        if (curWave < e.WaveToUnlock) return false;

        // prestige gate
        if (e.RequirePrestigeUnlock && (PrestigeManager.Instance == null || !PrestigeManager.Instance.IsGunnerUnlocked(id)))
            return false;

        return true;
    }

    public void NotifyExternalUnlocksChanged()
    {
        OnRosterChanged?.Invoke();
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

    // New: grant XP only to currently equipped gunners (unique per slot)
    public void GrantXpToEquipped(float xp)
    {
        var given = new HashSet<string>();
        foreach (var kv in slotToGunner)
        {
            var gid = kv.Value;
            if (!given.Add(gid)) continue; // a gunner can’t be in two slots, but guard anyway

            if (!runtimes.TryGetValue(gid, out var rt)) continue;
            var so = allGunners.FirstOrDefault(g => g.GunnerId == gid);
            if (so == null) continue;

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

    public void NotifySlotStatsChanged(int slotIndex)
    {
        if (slotIndex < 0) return;
        OnSlotGunnerStatsChanged?.Invoke(slotIndex);
    }

    public GunnerBonus ComputeBonusForSlot(int slotIndex)
    {
        var bonus = new GunnerBonus();
        if (!slotToGunner.TryGetValue(slotIndex, out var id)) return bonus;
        if (!runtimes.TryGetValue(id, out var rt)) return bonus;
        if (rt.IsDead || rt.IsOnQuest) return bonus;
        // Do not apply any bonus until the model actually docked on the turret
        if (!dockedGunners.Contains(id)) return bonus;

        var so = allGunners.FirstOrDefault(g => g.GunnerId == id);
        if (so == null) return bonus;

        // Use upgraded values from GunnerUpgradeManager (base + growth)
        // Use upgraded values if the manager exists; otherwise fall back to zeros to avoid NRE
        var up = GunnerUpgradeManager.Instance;
        float Get(GunnerStatKey k) => (up != null) ? up.GetEffectiveStatValue(so, rt, k) : 0f;

        float dmg = Get(GunnerStatKey.Damage);
        float fr = Get(GunnerStatKey.FireRate);
        float rng = Get(GunnerStatKey.Range);
        float ramp = Get(GunnerStatKey.PercentBonusDamagePerSec);
        float slow = Get(GunnerStatKey.SlowEffect);
        float cc = Get(GunnerStatKey.CriticalChance);
        float cd = Get(GunnerStatKey.CriticalDamageMultiplier);
        float kb = Get(GunnerStatKey.KnockbackStrength);
        float sp = Get(GunnerStatKey.SplashDamage);
        float pc = Get(GunnerStatKey.PierceChance);
        float pf = Get(GunnerStatKey.PierceDamageFalloff);
        float ap = Get(GunnerStatKey.ArmorPenetration);

        // Only contribute stats that are unlocked for this gunner
        bool Unl(GunnerStatKey k) => rt.Unlocked != null && rt.Unlocked.Contains(k);

        // assign into bonus (additive to turret)
        if (Unl(GunnerStatKey.Damage)) bonus.Damage += dmg;
        if (Unl(GunnerStatKey.FireRate)) bonus.FireRate += fr;
        if (Unl(GunnerStatKey.Range)) bonus.Range += rng;
        if (Unl(GunnerStatKey.PercentBonusDamagePerSec)) bonus.PercentBonusDamagePerSec += ramp;
        if (Unl(GunnerStatKey.SlowEffect)) bonus.SlowEffect += slow;
        if (Unl(GunnerStatKey.CriticalChance)) bonus.CriticalChance += cc;
        if (Unl(GunnerStatKey.CriticalDamageMultiplier)) bonus.CriticalDamageMultiplier += cd;
        if (Unl(GunnerStatKey.KnockbackStrength)) bonus.KnockbackStrength += kb;
        if (Unl(GunnerStatKey.SplashDamage)) bonus.SplashDamage += sp;
        if (Unl(GunnerStatKey.PierceChance)) bonus.PierceChance += pc;
        if (Unl(GunnerStatKey.PierceDamageFalloff)) bonus.PierceDamageFalloff += pf;
        if (Unl(GunnerStatKey.ArmorPenetration)) bonus.ArmorPenetration += ap;

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

        if (runtimes[gunnerId].IsDead)
            return; // dead gunners give no bonuses

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
        GrantXpToEquipped(xpReward);
    }

    public void OnEnemySpawned(Enemy enemy)
    {
        enemy.OnDeath += HandleEnemyDeathForXp;
    }

    private void HandleEnemyDeathForXp(object sender, System.EventArgs e)
    {
        if (e is OnDeathEventArgs death)
            OnEnemyKilled(death.XPDropAmount);
    }

    /* ===================== ENEMY HELPER ===================== */
    public bool ApplyDamageOnSlot(int slotIndex, float damage)
    {
        if (!slotToGunner.TryGetValue(slotIndex, out var gid)) return false;
        if (!runtimes.TryGetValue(gid, out var rt)) return false;
        if (rt.IsDead) return false;

        if (LimitBreakManager.Instance != null)
        {
            float red = Mathf.Clamp(LimitBreakManager.Instance.GunnerDamageReductionPct, 0f, 95f);
            damage *= (1f - red / 100f);
        }

        bool diedNow = rt.TakeDamage(damage, out float actual);

        if (diedNow)
        {
            Debug.Log($"[GunnerManager] Gunner {gid} has died.");
            // If this gunner was powering any Limit Break session(s), stop them immediately.
            LimitBreakManager.Instance?.StopAllForGunner(gid);
        }


        // Visual: either flash or play death, depending on result
        if (modelByGunner.TryGetValue(gid, out var model) && model != null)
        {
            if (diedNow)
            {
                // Stop any queued flashes and play death animation
                model.PlayDeath();
            }
            else if (actual > 0f)
            {
                model.PlayHitFlash();
            }
        }


        // Tell the equipped turret (for this slot) to rebuild its effective stats immediately
        OnSlotGunnerStatsChanged?.Invoke(slotIndex);

        // Update bars if we have them
        if (healthBarByGunner.TryGetValue(gid, out var hb) && hb != null)
            hb.SetValue(rt.CurrentHealth);

        if (limitBarByGunner.TryGetValue(gid, out var lb) && lb != null)
        {
            // Max rarely changes here; Set once at init / heal. Just push the value.
            lb.SetValue(rt.LimitBreakCurrent);
        }

        // Notify billboard to handle button/icon/VFX (will also snap if full)
        if (billboardByGunner.TryGetValue(gid, out var bb) && bb != null)
        {
            bb.RefreshLimitBreak(rt.LimitBreakCurrent);
        }


        // If all equipped are dead, signal once (consumer can fail wave)
        bool anyAlive = false;
        foreach (var kvp in slotToGunner)
        {
            var g = kvp.Value;
            if (runtimes.TryGetValue(g, out var rtx) && !rtx.IsDead) { anyAlive = true; break; }
        }
        if (!anyAlive) OnAllEquippedGunnersDead?.Invoke();

        Save(); // persist HP changes with your central save
        return true;
    }

    public bool TryApplyDamageForEnemy(Enemy enemy, float damage)
    {
        if (enemy == null) return false;

        // choose nearest alive target by X at the moment of attack
        int slot = GetNearestAliveSlotByX(enemy.transform.position.x);
        if (slot < 0) return false;

        return ApplyDamageOnSlot(slot, damage);
    }

    public int GetNearestAliveSlotByX(float worldX)
    {
        int bestSlot = -1;
        float bestDist = float.MaxValue;

        foreach (var kvp in slotToGunner)
        {
            int slot = kvp.Key;
            string gid = kvp.Value;

            if (!runtimes.TryGetValue(gid, out var rt) || rt.IsDead) continue;
            if (!slotAnchors.TryGetValue(slot, out var anchor) || anchor == null) continue;

            float dx = Mathf.Abs(anchor.position.x - worldX);
            if (dx < bestDist)
            {
                bestDist = dx;
                bestSlot = slot;
            }
        }
        return bestSlot;
    }

    public float GetSlotAnchorX(int slotIndex)
    {
        // Fast path
        if (slotAnchors.TryGetValue(slotIndex, out var t) && t != null)
            return t.position.x;

        // Lazy-build anchors from the scene if missing
        var slots = FindObjectsByType<Assets.Scripts.UI.SlotWorldButton>(FindObjectsSortMode.None);
        for (int i = 0; i < slots.Length; i++)
        {
            var swb = slots[i];
            if (swb == null) continue;
            slotAnchors[swb.slotIndex] = swb.barrelAnchor; // may be null for safety, but usually valid
        }

        // Try again
        if (slotAnchors.TryGetValue(slotIndex, out t) && t != null)
            return t.position.x;

        // Final fallback: 0 (keeps math safe, but you probably want anchors set by SlotWorldButton)
        return 0f;
    }

    public float GetSlotAnchorDepth(int slotIndex)
    {
        // Fast path
        if (slotAnchors.TryGetValue(slotIndex, out var t) && t != null)
            return t.position.z;

        // Lazy-build anchors from the scene if missing
        var slots = FindObjectsByType<Assets.Scripts.UI.SlotWorldButton>(FindObjectsSortMode.None);
        for (int i = 0; i < slots.Length; i++)
        {
            var swb = slots[i];
            if (swb == null) continue;
            slotAnchors[swb.slotIndex] = swb.barrelAnchor;
        }

        // Try again
        if (slotAnchors.TryGetValue(slotIndex, out t) && t != null)
            return t.position.z;

        // Fallback if truly unresolved
        return 0f;
    }

    /// <summary>
    /// Returns true if the slot currently has a living, equipped gunner.
    /// </summary>
    public bool IsSlotAlive(int slotIndex)
    {
        if (!slotToGunner.TryGetValue(slotIndex, out var gid)) return false;
        if (!runtimes.TryGetValue(gid, out var rt)) return false;
        return !rt.IsDead;
    }

    /// <summary>
    /// Applies an enemy attack with optional boss "prefer middle" and sweep hits.
    /// Returns true if at least one slot was hit.
    /// </summary>
    public bool TryApplySweepDamageForEnemy(Enemy enemy, float damage, bool preferMiddleSlot)
    {
        if (enemy == null) return false;

        // 1) Choose main slot
        int mainSlot = -1;

        if (preferMiddleSlot)
        {
            // If slot 2 has a living gunner, prefer it; else fallback to nearest alive by X
            mainSlot = 2;
            bool slot2Alive = ApplyDamageOnSlot(2, 0f); // probe for presence (0 damage)
            if (!slot2Alive)
                mainSlot = GetNearestAliveSlotByX(enemy.transform.position.x);
        }
        else
        {
            mainSlot = GetNearestAliveSlotByX(enemy.transform.position.x);
        }

        if (mainSlot < 0) return false;

        // 2) Compute sweep slots based on enemy Info (1..5)
        int sweepTargets = Mathf.Clamp(enemy.Info.SweepTargets, 1, 5);
        List<int> hits = new List<int>(sweepTargets) { mainSlot };

        if (sweepTargets > 1)
        {
            // Rule you specified:
            // - If main=0 → hit 0 and 1
            // - If main=4 → hit 4 and 3
            // - Otherwise: hit main and the neighbor leaning toward the right for slots >=2, else left.
            if (mainSlot == 0) hits.Add(1);
            else if (mainSlot == 4) hits.Add(3);
            else
            {
                int right = mainSlot + 1;
                int left = mainSlot - 1;
                hits.Add(mainSlot >= 2 ? right : left);
            }

            // If in future you allow 3–5 targets, extend neighbor picking here (e.g., add the other side, then expand outwards).
        }

        // 3) Apply damage to selected slots
        bool any = false;
        for (int i = 0; i < hits.Count; i++)
            any |= ApplyDamageOnSlot(hits[i], damage);

        return any;
    }


    /* ===================== HEAL / LIMIT BREAK ===================== */
    /// <summary>
    /// Heal all gunners to full HP. Optionally reset Limit Break gauge.
    /// Updates billboard bars for equipped gunners. No save here; WaveManager saves after starting the wave.
    /// </summary>
    public void HealAllGunners(bool resetLimitBreak = false)
    {
        // Track which gunners were dead and are now revived
        var revivedIds = new List<string>();

        // Heal every known gunner (equipped or not)
        foreach (var kv in runtimes)
        {
            string gid = kv.Key;
            var rt = kv.Value;

            bool wasDead = rt.IsDead;
            rt.Heal(rt.MaxHealth);
            if (resetLimitBreak) rt.ResetLimitBreak();

            if (wasDead && !rt.IsDead)
                revivedIds.Add(gid);
        }

        // Refresh bars only for equipped (those have billboards)
        foreach (var map in slotToGunner)
        {
            string gid = map.Value;
            if (!runtimes.TryGetValue(gid, out var rt)) continue;

            if (healthBarByGunner.TryGetValue(gid, out var hb) && hb != null)
            {
                hb.SetMax(rt.MaxHealth);
                hb.SetValue(rt.CurrentHealth);
            }
            if (limitBarByGunner.TryGetValue(gid, out var lb) && lb != null)
            {
                lb.SetMax(rt.LimitBreakMax);
                lb.SetValue(rt.LimitBreakCurrent);
            }
        }

        // Play revive animation for any equipped gunners that came back to life
        for (int i = 0; i < revivedIds.Count; i++)
        {
            string gid = revivedIds[i];
            if (modelByGunner.TryGetValue(gid, out var model) && model != null)
            {
                model.PlayRevive();
            }
        }

        // Ping turrets so they re-merge (important if a dead gunner was just healed)
        if (OnSlotGunnerStatsChanged != null)
        {
            foreach (var s in slotToGunner.Keys)
                OnSlotGunnerStatsChanged.Invoke(s);
        }
    }



    // Delegate to the new manager
    public bool TryActivateLimitBreak(string gunnerId)
    {
        return LimitBreakManager.Instance != null
            ? LimitBreakManager.Instance.TryActivate(gunnerId)
            : false;
    }

    public void NotifyLimitBreakChanged(string gunnerId)
    {
        if (!runtimes.TryGetValue(gunnerId, out var rt)) return;
        if (limitBarByGunner.TryGetValue(gunnerId, out var lb) && lb) { lb.SetMax(rt.LimitBreakMax); lb.SetValue(rt.LimitBreakCurrent); }
        if (billboardByGunner.TryGetValue(gunnerId, out var bb) && bb) { bb.RefreshLimitBreak(rt.LimitBreakCurrent); }
    }

    public LimitBreakSkillSO ResolveLimitBreakFor(GunnerSO so)
    {
        // Delegate to registry for a single source of truth
        return (LimitBreakManager.Instance != null)
            ? LimitBreakManager.Instance.ResolveFor(so)
            : so?.LimitBreakSkill;
    }

    public static Sprite GetSkillIcon(GunnerSO so)
    {
        if (so == null) return null;
        return (LimitBreakManager.Instance != null)
            ? LimitBreakManager.Instance.GetIconFor(so)
            : (so.LimitBreakSkill != null ? so.LimitBreakSkill.Icon : null);
    }

    /// <summary>Heals each equipped gunner by % of their MaxHealth and refreshes bars.</summary>
    public void HealEquippedPercent(float pct)
    {
        pct = Mathf.Max(0f, pct);
        foreach (var kv in slotToGunner)
        {
            string gid = kv.Value;
            if (!runtimes.TryGetValue(gid, out var rt)) continue;
            float add = rt.MaxHealth * (pct / 100f);
            rt.Heal(add);

            if (healthBarByGunner.TryGetValue(gid, out var hb) && hb != null)
            {
                hb.SetMax(rt.MaxHealth);
                hb.SetValue(rt.CurrentHealth);
            }
        }
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
            int upCount = rt.UpgradeLevels?.Count ?? 0;
            int[] upKeys = new int[upCount];
            int[] upLvls = new int[upCount];
            for (int i = 0; i < upCount; i++)
            {
                upKeys[i] = (int)rt.UpgradeLevels[i].Key;
                upLvls[i] = rt.UpgradeLevels[i].Level;
            }

            dto.Runtimes.Add(new GunnerRuntimeDTO
            {
                Id = kvp.Key,
                CurHP = rt.CurrentHealth,
                MaxHP = rt.MaxHealth,
                Level = rt.Level,
                Xp = rt.CurrentXp,
                Points = rt.UnspentSkillPoints,
                Unlocked = rt.Unlocked.Select(u => (int)u).ToArray(),
                UpKeys = upKeys,
                UpLevels = upLvls,
                OnQuest = rt.IsOnQuest,
                QuestEnd = rt.QuestEndUnixTime,
                EquippedSlot = rt.EquippedSlot,

                // kept for backward-compatibility with old saves that didn’t have top-level field
                PreferredStarterId = (preferredStarterGunner != null && rt.GunnerId == preferredStarterGunner.GunnerId)
                                     ? preferredStarterGunner.GunnerId
                                     : null
            });
        }

        // slot map & ownership
        foreach (var kvp in slotToGunner)
            dto.SlotMap.Add(new SlotGunnerDTO { SlotIndex = kvp.Key, GunnerId = kvp.Value });
        dto.OwnedGunners.AddRange(ownedGunners);

        dto.PreferredStarterId = preferredStarterGunner != null ? preferredStarterGunner.GunnerId : null;

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

            // rebuild per-stat upgrade levels
            rt.UpgradeLevels.Clear();
            if (r.UpKeys != null && r.UpLevels != null)
            {
                int n = Mathf.Min(r.UpKeys.Length, r.UpLevels.Length);
                for (int i = 0; i < n; i++)
                {
                    rt.UpgradeLevels.Add(new GunnerRuntime.GunnerStatLevel
                    {
                        Key = (GunnerStatKey)r.UpKeys[i],
                        Level = r.UpLevels[i]
                    });
                }
            }

            Transform attachAnchor = null;
            foreach (var slot in FindObjectsByType<SlotWorldButton>(FindObjectsSortMode.None))
            {
                if (slot.slotIndex == rt.EquippedSlot)
                {
                    attachAnchor = slot.barrelAnchor;
                    break;
                }
            }
            AttachEquippedGunnerVisual_Internal(rt.GunnerId, attachAnchor, rt.EquippedSlot);
        }

        slotToGunner.Clear();
        for (int i = 0; i < dto.SlotMap.Count; i++)
        {
            var s = dto.SlotMap[i];
            slotToGunner[s.SlotIndex] = s.GunnerId;
        }

        ownedGunners.Clear();
        if (dto.OwnedGunners != null)
        {
            foreach (var id in dto.OwnedGunners)
                ownedGunners.Add(id);
        }

        // Resolve preferred starter:
        // 1) Prefer the new top-level field
        string prefId = dto.PreferredStarterId;

        // 2) Backward-compat: if missing, scan per-runtime legacy field
        if (string.IsNullOrEmpty(prefId))
        {
            foreach (var r in dto.Runtimes)
            {
                if (!string.IsNullOrEmpty(r.PreferredStarterId)) { prefId = r.PreferredStarterId; break; }
            }
        }

        if (!string.IsNullOrEmpty(prefId) && soById.TryGetValue(prefId, out var soPreferd))
        {
            preferredStarterGunner = soPreferd;
            ownedGunners.Add(soPreferd.GunnerId); // ensure ownership
        }
        else
        {
            preferredStarterGunner = null;
        }

        OnSlotGunnerChanged?.Invoke(-1); // bulk refresh
        OnRosterChanged?.Invoke();
    }

    /* ===================== PRESTIGE RESET ===================== */
    /// <summary>
    /// Reset gunners for prestige. If wipeOwnership = true, removes ownership
    /// and unequips everyone. If only wipeUpgrades = true, keeps ownership but
    /// resets level, points, learned stats and per-stat upgrade levels.
    /// </summary>
    public void ResetAll(bool wipeOwnership, bool wipeUpgrades, bool resetLevels)
    {
        // Always unequip visuals/slots first
        var slots = new List<int>(slotToGunner.Keys);
        for (int i = 0; i < slots.Count; i++)
            UnequipFromSlot(slots[i]);

        // Clear any “memory” of who used to be where
        parkedSlotToGunner.Clear();

        if (wipeOwnership)
        {            
            ownedGunners.Clear();

            // Reset EVERY runtime to a fresh baseline so no old levels/stats leak through
            foreach (var kv in runtimes)
                ResetRuntimeToFresh(kv.Key, kv.Value);

            if (preferredStarterGunner != null)
            {
                var pid = preferredStarterGunner.GunnerId;
                ownedGunners.Add(pid);

                if (!runtimes.TryGetValue(pid, out var rt))
                {
                    rt = new GunnerRuntime(preferredStarterGunner);
                    runtimes[pid] = rt;
                }
                ResetRuntimeToFresh(pid, rt);
                EquipToFirstFreeSlot(pid, preferSlotIndex: 2);
                
            }
            else
            {
                // Ensure no accidental carry-over when prefs were cleared by DeleteSave
                preferredStarterGunner = null;
            }

            Save();
            OnRosterChanged?.Invoke();
            //DebugDumpAll("POST (wipe ownership)");
            return;
        }


        foreach (var kv in runtimes)
        {
            string gid = kv.Key;
            var rt = kv.Value;

           // DebugDumpRuntime("BEFORE", gid, rt);

            if (resetLevels)
            {
                rt.Level = 1;
                rt.CurrentXp = 0f;
                rt.UnspentSkillPoints = 0;

                // Revert to StartingUnlocked only (remove level-milestone unlocks)
                var so = GetSO(gid);
                if (so != null)
                    rt.Unlocked = new HashSet<GunnerStatKey>(so.StartingUnlocked);
            }

            if (wipeUpgrades)
            {
                // Wipe per-stat investment completely
                rt.UpgradeLevels?.Clear();

                // Optional: if wiping upgrades should also wipe *all* unlocks (not just level-based), keep this line:
                // rt.Unlocked?.Clear();
                // Otherwise we rely on the resetLevels step to rebuild Unlocked to StartingUnlocked when requested.
            }

            // Always recompute derived HP from current state (after any changes above)
            RecomputeDerivedStats(gid, rt);

            // Clear any ongoing quest state on prestige
            rt.IsOnQuest = false;
            rt.QuestEndUnixTime = 0;

            //DebugDumpRuntime("AFTER", gid, rt);
        }

        Save();
        OnRosterChanged?.Invoke();
        //DebugDumpAll("POST");
    }
    /// <summary>Computes base value for a stat using level 0 upgrades.</summary>
    private float GetEffectiveStatValueForReset(string gunnerId, GunnerStatKey key)
    {
        var so = GetSO(gunnerId);
        if (so == null) return 0f;
        return GunnerUpgradeManager.Instance != null
            ? GunnerUpgradeManager.Instance.GetEffectiveStatValueAtLevel(so, key, 0)
            : 0f;
    }

    /// <summary>Equip the given gunner to the preferred slot (0 by default).
    /// If the anchor dictionary is empty (common right after resets), we rebuild it
    /// from SlotWorldButton in the scene. If still missing, we set the mapping now
    /// and defer the visual attach until anchors are available.</summary>
    public void EquipToFirstFreeSlot(string gunnerId, int preferSlotIndex = 2)
    {
        //Debug.Log($"EquipToFirstFreeSlot called for gunner {gunnerId} preferring slot {preferSlotIndex}.");
        if (string.IsNullOrEmpty(gunnerId)) return;

        // Resolve anchor safely
        if (!slotAnchors.TryGetValue(preferSlotIndex, out var slotBarrel) || slotBarrel == null)
        {
            var slots = FindObjectsByType<SlotWorldButton>(FindObjectsSortMode.None);
            for (int i = 0; i < slots.Length; i++)
                slotAnchors[slots[i].slotIndex] = slots[i].barrelAnchor;

            slotBarrel = slotAnchors.TryGetValue(preferSlotIndex, out var t) ? t : null;
        }

        if (slotBarrel == null)
        {
            Debug.LogWarning($"[GunnerManager] No anchor for slot {preferSlotIndex}. Deferring visual; mapping only.");
            // set slot mapping now so gameplay continues
            slotToGunner[preferSlotIndex] = gunnerId;
            if (runtimes.TryGetValue(gunnerId, out var rt)) rt.EquippedSlot = preferSlotIndex;
            OnSlotGunnerChanged?.Invoke(preferSlotIndex);
            return;
        }
        //Debug.Log("Preerred slot anchor found; equipping gunner visually now. " + preferSlotIndex);
        if (!EquipToSlot(gunnerId, preferSlotIndex, slotBarrel))
            Debug.LogWarning($"[GunnerManager] Could not equip gunner {gunnerId} to preferred slot {preferSlotIndex} (anchor ok).");
    }


    /// <summary>Recompute derived runtime stats from current UpgradeLevels/unlocks.</summary>
    private void RecomputeDerivedStats(string gunnerId, GunnerRuntime rt)
    {
        // Health is the only one we persist directly on the runtime (others are merged into turrets on demand).
        rt.MaxHealth = GetEffectiveStatValueForReset(gunnerId, GunnerStatKey.Health);
        rt.MaxHealth = Mathf.Max(1f, rt.MaxHealth);
        rt.CurrentHealth = Mathf.Min(rt.CurrentHealth, rt.MaxHealth);
    }

    /// <summary>Hard reset a runtime to a fresh state based on its SO.</summary>
    private void ResetRuntimeToFresh(string gid, GunnerRuntime rt)
    {
        var so = GetSO(gid);
        rt.Level = 1;
        rt.CurrentXp = 0f;
        rt.UnspentSkillPoints = 0;
        rt.Unlocked = (so != null)
            ? new HashSet<GunnerStatKey>(so.StartingUnlocked)
            : new HashSet<GunnerStatKey>();
        rt.UpgradeLevels?.Clear();
        rt.IsOnQuest = false;
        rt.QuestEndUnixTime = 0;
        RecomputeDerivedStats(gid, rt);
        //Debug.Log($"[GunnerReset] Fresh runtime applied -> id={gid} Lvl={rt.Level} HP={rt.MaxHealth} Unlocked={rt.Unlocked.Count} Upgrades={rt.UpgradeLevels.Count}");
    }


    #region DEBUG
    private void DebugDumpRuntime(string title, string gid, GunnerRuntime rt)
    {
        var upCount = rt.UpgradeLevels?.Count ?? 0;
        var unCount = rt.Unlocked?.Count ?? 0;
        Debug.Log($"[GunnerReset] {title} id={gid} " +
                  $"Lvl={rt.Level} XP={rt.CurrentXp} Pts={rt.UnspentSkillPoints} " +
                  $"MaxHP={rt.MaxHealth} CurHP={rt.CurrentHealth} " +
                  $"Upgrades={upCount} Unlocked={unCount} " +
                  $"Quest={(rt.IsOnQuest ? "Y" : "N")} Slot={rt.EquippedSlot}");
    }

    private void DebugDumpAll(string title)
    {
        Debug.Log($"[GunnerReset] ---- {title} ---- runtimes={runtimes.Count} owned={ownedGunners.Count}");
        foreach (var kv in runtimes)
            DebugDumpRuntime(title, kv.Key, kv.Value);
    }
    #endregion

}
