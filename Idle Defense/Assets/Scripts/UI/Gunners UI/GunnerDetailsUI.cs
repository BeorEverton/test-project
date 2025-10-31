using Assets.Scripts.UI;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GunnerDetailsUI : MonoBehaviour
{
    [Header("Data")]
    public List<GunnerSO> roster;          // optional: can be fed by GunnerManager
    public int currentIndex;

    [Header("Ordering")]
    [SerializeField] private GunnerUnlockTableSO unlockTable;   // put your table here
    [SerializeField] private bool useUnlockTableOrder = true;

    [Header("Top")]
    public TextMeshProUGUI gunnerNameText;
    public TextMeshProUGUI levelText;
    public Slider xpSlider;
    public TextMeshProUGUI xpValueText;
    public TextMeshProUGUI pointsText; // shows available skill points for this gunner

    [Header("Center")]
    public Image portraitImage;            // big gunner art
    public Button prevButton;
    public Button nextButton;

    [Header("Limit Break")]
    public Image limitIcon;
    public TextMeshProUGUI limitDescText;

    [Header("Controls")]
    public TextMeshProUGUI equipText;
    public Button levelUpButton;
    public Button equipButton;
    public Button unequipButton;

    [Header("Purchase UI")]
    [SerializeField] private TextMeshProUGUI buyPriceText;
    [SerializeField] private TextMeshProUGUI currencyIconText;
    [SerializeField] private GameObject currencyIconGO;

    [Header("Stats List")]
    public Transform statListRoot;         // parent of rows
    public GunnerStatRowUI statRowPrefab;

    [Header("Equip Ghost (optional)")]
    public GunnerCursorGhost cursorGhost;

    [Header("Panel Root (optional)")]
    public GameObject panelRoot;        // assign the panel GameObject if this script lives elsewhere
    public CanvasGroup panelGroup;      // optional; gives nicer show/hide

    [Header("Behaviour")]
    public bool closePanelOnEquip = true;

    public event Action Opened;
    public event Action Closed;

    private bool _isOpen = false;

    // local working
    private GunnerSO _so;
    private GunnerRuntime _rt;

    public void Open(List<GunnerSO> list = null, int startIndex = 0)
    {
        panelRoot.SetActive(true);
        // Seed list priority: explicit arg > existing roster > manager’s list (if exposed)
        var seed = list ?? roster;

        if (useUnlockTableOrder && unlockTable != null)
            roster = BuildRosterFromUnlockTable(seed);
        else
            roster = seed ?? roster;

        if (roster == null || roster.Count == 0)
        {
            Debug.LogWarning("GunnerDetailsUI.Open: roster is empty. Assign a list or the GunnerUnlockTable.");
            return;
        }

        currentIndex = Mathf.Clamp(startIndex, 0, roster.Count - 1);

        WireListeners();
        SetVisible(true);
        BindByIndex(currentIndex);
        _isOpen = true;
        Opened?.Invoke();
    }

    public void Open(GunnerSO single)
    {
        if (single != null)
        {
            if (roster == null) roster = new List<GunnerSO> { single };
            currentIndex = Mathf.Max(0, roster.IndexOf(single));
        }
        Open(roster, currentIndex);
    }

    public void Close()
    {
        if (!_isOpen) return;

        UnwireListeners();

        SetVisible(false);
        _isOpen = false;
        Closed?.Invoke();
    }

    // internal wiring helpers
    private void WireListeners()
    {
        if (prevButton) prevButton.onClick.AddListener(OnClickPrev);
        if (nextButton) nextButton.onClick.AddListener(OnClickNext);
        if (levelUpButton) levelUpButton.onClick.AddListener(OnClickOpenUpgrades);
        if (equipButton) equipButton.onClick.AddListener(OnEquip);
        if (unequipButton) unequipButton.onClick.AddListener(OnUnequip);
    }

    private void UnwireListeners()
    {
        if (prevButton) prevButton.onClick.RemoveListener(OnClickPrev);
        if (nextButton) nextButton.onClick.RemoveListener(OnClickNext);
        if (levelUpButton) levelUpButton.onClick.RemoveListener(OnClickOpenUpgrades);
        if (equipButton) equipButton.onClick.RemoveListener(OnEquip);
        if (unequipButton) unequipButton.onClick.RemoveListener(OnUnequip);
    }

    private void HandleExternalChange()
    {
        // Rebind current gunner so the panel flips from Locked → Buy → Equip states when needed
        if (_so != null) Bind(_so);
    }

    private void SetVisible(bool show)
    {
        var root = panelRoot ? panelRoot : gameObject;
        if (panelGroup)
        {
            panelGroup.alpha = show ? 1f : 0f;
            panelGroup.interactable = show;
            panelGroup.blocksRaycasts = show;
            root.SetActive(true); // keep GO alive for CanvasGroup to work
        }
        else
        {
            root.SetActive(show);
        }
    }

    private List<GunnerSO> BuildRosterFromUnlockTable(List<GunnerSO> seed)
    {
        var ordered = new List<GunnerSO>();
        var seen = new HashSet<string>();

        // Try to resolve SOs by id in table order
        if (unlockTable != null)
        {
            foreach (var e in unlockTable.Entries)
            {
                GunnerSO so = null;

                // Prefer manager lookup (fast), else from the seed list
                if (GunnerManager.Instance != null)
                    so = GunnerManager.Instance.GetSO(e.GunnerId);

                if (so == null && seed != null)
                    so = seed.Find(x => x != null && x.GunnerId == e.GunnerId);

                if (so != null && seen.Add(so.GunnerId))
                    ordered.Add(so);
            }
        }

        // Append any seed gunners not present in the table (keeps things robust)
        if (seed != null)
        {
            foreach (var so in seed)
                if (so != null && seen.Add(so.GunnerId))
                    ordered.Add(so);
        }

        return ordered;
    }

    // PUBLIC API -------------------------------------------------------------

    public void BindByIndex(int idx)
    {
        if (roster == null || roster.Count == 0) return;
        currentIndex = Mathf.Clamp(idx, 0, roster.Count - 1);
        var so = roster[currentIndex];
        Bind(so);
    }

    public void Bind(GunnerSO so)
    {
        _so = so;
        _rt = (GunnerManager.Instance != null)
            ? GunnerManager.Instance.GetRuntime(so.GunnerId)
            : null; // guard for null instance (editor preview etc.)

        // Top
        if (gunnerNameText) gunnerNameText.text = so.DisplayName;
        if (levelText) levelText.text = "Lv " + (_rt != null ? _rt.Level : 1);

        float curXp = _rt != null ? _rt.CurrentXp : 0f;
        float nextXp = so.XpRequiredForLevel(_rt != null ? _rt.Level : 1);
        if (xpSlider)
        {
            xpSlider.minValue = 0f;
            xpSlider.maxValue = Mathf.Max(1f, nextXp);
            xpSlider.value = Mathf.Clamp(curXp, 0f, nextXp);
        }
        if (xpValueText) xpValueText.text = Mathf.FloorToInt(curXp) + " / " + Mathf.FloorToInt(nextXp);

        // Center
        if (portraitImage) portraitImage.sprite = so.gunnerSprite;

        // Limit Break UI (always go through the registry)
        LimitBreakSkillSO lb = (LimitBreakManager.Instance != null)
            ? LimitBreakManager.Instance.ResolveFor(so)
            : (GunnerManager.Instance != null ? GunnerManager.Instance.ResolveLimitBreakFor(so) : so.LimitBreakSkill);

        if (lb != null)
        {
            if (limitIcon) limitIcon.sprite = lb.Icon;

            if (limitDescText)
            {
                string name = string.IsNullOrEmpty(lb.DisplayName) ? "Limit Break" : lb.DisplayName;

                if (!string.IsNullOrEmpty(lb.Description))
                {
                    limitDescText.text = lb.Description;
                }
                else
                {
                    limitDescText.text = $"{name} — x{lb.Magnitude:0.##} for {lb.Duration:0.#}s";
                }
            }
        }
        else
        {
            Debug.LogWarning($"GunnerDetailsUI: Gunner {so.GunnerId} has no LimitBreakSkill assigned or resolved.");
            if (limitIcon) limitIcon.sprite = null;
            if (limitDescText) limitDescText.text = "Limit Break: none";
        }

        // Points + controls
        int points = (_rt != null) ? _rt.UnspentSkillPoints : 0;
        if (pointsText) pointsText.text = points + "" + (points == 1 ? "" : "");
        if (levelUpButton) levelUpButton.gameObject.SetActive(false); // replaced by per-row '+'

        UpdatePurchaseAndEquipUI();

        // Stats list
        RebuildStatsList();

        // Nav
        if (prevButton) prevButton.interactable = roster != null && roster.Count > 1;
        if (nextButton) nextButton.interactable = roster != null && roster.Count > 1;
    }

    public void OnClickPrev()
    {
        if (roster == null || roster.Count == 0) return;
        currentIndex = (currentIndex - 1 + roster.Count) % roster.Count;
        BindByIndex(currentIndex);
    }

    public void OnClickNext()
    {
        if (roster == null || roster.Count == 0) return;
        currentIndex = (currentIndex + 1) % roster.Count;
        BindByIndex(currentIndex);
    }


    // INTERNAL ---------------------------------------------------------------

    private void RebuildStatsList()
    {
        if (!statListRoot || !statRowPrefab || _so == null) return;

        // Clear list first.
        for (int i = statListRoot.childCount - 1; i >= 0; --i)
            Destroy(statListRoot.GetChild(i).gameObject);

        // Only stats this gunner actually has (StartingUnlocked LevelUnlocks)
        var keysToShow = CollectAllStatKeys(_so);

        foreach (var key in keysToShow)
        {
            var row = Instantiate(statRowPrefab, statListRoot);
            int unlockLevel = GetUnlockLevel(_so, key);

            // Prefer runtime info; fall back to SO + current level.
            bool unlocked = _rt != null && _rt.Unlocked != null && _rt.Unlocked.Contains(key);
            if (!unlocked)
            {
                int curLevel = _rt != null ? _rt.Level : 0;
                bool isStarting = _so.StartingUnlocked != null && _so.StartingUnlocked.Contains(key);
                bool levelReached = (unlockLevel != int.MaxValue) && (curLevel >= unlockLevel);
                unlocked = isStarting || levelReached;
            }

            if (!unlocked)
            {
                row.SetLocked(GetIconFor(key), GetNiceName(key), unlockLevel);
                continue;
            }

            // Current value
            float current = (_rt != null && GunnerUpgradeManager.Instance != null)
                ? GunnerUpgradeManager.Instance.GetEffectiveStatValue(_so, _rt, key)
                : GetBaseValueFor(_so, key);

            // Can upgrade now?
            bool hasPoints = _rt != null && _rt.UnspentSkillPoints > 0;
            bool canUpgrade = hasPoints && GunnerUpgradeManager.Instance != null;

            // If upgradable, show "+ → next"; else, simple unlocked row
            if (canUpgrade)
            {
                int nextLvl = _rt.GetUpgradeLevel(key) + 1;
                float nextVal = GunnerUpgradeManager.Instance.GetEffectiveStatValueAtLevel(_so, key, nextLvl);

                row.SetUnlockedWithUpgrade(
                    GetIconFor(key),
                    GetNiceName(key),
                    FormatValueFor(key, current),
                    FormatValueFor(key, nextVal),
                    onUpgrade: () =>
                    {
                        if (GunnerUpgradeManager.Instance.TrySpendPoint(_so, _rt, key))
                        {
                            // Rebind to refresh points, values, and any turret bonuses
                            Bind(_so);
                        }
                    });
            }
            else
            {
                row.SetUnlocked(GetIconFor(key), GetNiceName(key), FormatValueFor(key, current));
            }

        }
    }

    private List<GunnerStatKey> CollectAllStatKeys(GunnerSO so)
    {
        var result = new List<GunnerStatKey>();
        var seen = new HashSet<GunnerStatKey>();

        // 1) Starting unlocked (keep authoring order)
        if (so.StartingUnlocked != null)
        {
            foreach (var k in so.StartingUnlocked)
            {
                if (seen.Add(k)) result.Add(k);
            }
        }

        // 2) Level unlocks in ascending level; keep each group's order
        if (so.LevelUnlocks != null && so.LevelUnlocks.Count > 0)
        {
            // Work on a copy to avoid mutating the SO list reference
            var sorted = new List<GunnerSO.LevelUnlock>(so.LevelUnlocks);
            sorted.Sort((a, b) => a.Level.CompareTo(b.Level));

            foreach (var group in sorted)
            {
                if (group.Unlocks == null) continue;
                foreach (var k in group.Unlocks)
                {
                    if (seen.Add(k)) result.Add(k);
                }
            }
        }

        return result;
    }

    private int GetUnlockLevel(GunnerSO so, GunnerStatKey key)
    {
        // 0 = starting unlocked; otherwise return the configured level.
        if (so.StartingUnlocked != null && so.StartingUnlocked.Contains(key))
            return 0;

        if (so.LevelUnlocks != null)
        {
            for (int i = 0; i < so.LevelUnlocks.Count; i++)
            {
                var group = so.LevelUnlocks[i];
                if (group.Unlocks != null && group.Unlocks.Contains(key))
                    return group.Level;
            }
        }

        // Not present in this GunnerSO - never unlocks (should not be displayed).
        return int.MaxValue;
    }

    private float GetBaseValueFor(GunnerSO so, GunnerStatKey key)
    {
        switch (key)
        {
            case GunnerStatKey.Health: return so.BaseHealth;
            case GunnerStatKey.Damage: return so.BaseDamage;
            case GunnerStatKey.FireRate: return so.BaseFireRate;
            case GunnerStatKey.Range: return so.BaseRange;
            case GunnerStatKey.PercentBonusDamagePerSec: return so.BaseDamagePerSecPctBonus;
            case GunnerStatKey.SlowEffect: return so.BaseSlowEffect;
            case GunnerStatKey.CriticalChance: return so.BaseCriticalChance;
            case GunnerStatKey.CriticalDamageMultiplier: return so.BaseCriticalDamage;
            case GunnerStatKey.KnockbackStrength: return so.BaseKnockback;
            case GunnerStatKey.SplashDamage: return so.BaseSplash;
            case GunnerStatKey.PierceChance: return so.BasePierceChance;
            case GunnerStatKey.PierceDamageFalloff: return so.BasePierceFalloff;
            case GunnerStatKey.ArmorPenetration: return so.BaseArmorPenetration;
            default: return 0f;
        }
    }

    private void UpdateEquipButtons()
    {
        // Equipped if runtime exists and is in a slot (>= 0)
        bool isEquipped = _rt != null && _rt.EquippedSlot >= 0;
        // Allow equipping even with no free slots (player may swap by clicking a taken slot)
        bool canEquip = _rt != null && !_rt.IsOnQuest && !isEquipped;

        if (equipButton)
        {
            equipButton.gameObject.SetActive(!isEquipped);
            equipButton.interactable = canEquip;
        }

        if (unequipButton)
        {
            unequipButton.gameObject.SetActive(isEquipped);
            unequipButton.interactable = isEquipped; // extra guard
        }
    }

    private void UpdatePurchaseAndEquipUI()
    {
        var gm = GunnerManager.Instance;
        bool owned = gm != null && _so != null && gm.IsOwned(_so.GunnerId);

        // Default: hide price UI
        if (buyPriceText) buyPriceText.text = "";
        if (currencyIconGO) currencyIconGO.SetActive(false);
        if (currencyIconText) currencyIconText.gameObject.SetActive(false);

        // Prestige lock check (only matters if not owned yet)
        if (!owned)
        {
            bool requiresPrestige = gm != null && gm.RequiresPrestigeUnlock(_so.GunnerId);
            bool prestigeUnlocked = PrestigeManager.Instance != null && PrestigeManager.Instance.IsGunnerUnlocked(_so.GunnerId);

            if (requiresPrestige && !prestigeUnlocked)
            {
                // Locked by prestige — disable buy/equip, show lock label
                if (equipText) equipText.text = "Locked (Prestige)";
                if (equipButton) { equipButton.gameObject.SetActive(true); equipButton.interactable = false; }
                if (unequipButton) unequipButton.gameObject.SetActive(false);
                return;
            }
            else if (equipText)
                equipText.text = "Equip";

            // Purchasable: show price + icon, enable the "equip" button to act as Buy
            ulong cost = gm != null ? gm.GetFirstCopyCost(_so.GunnerId) : 0UL;
            if (buyPriceText) buyPriceText.text = cost > 0 ? cost.ToString("N0") : "Free";
            if (currencyIconGO) currencyIconGO.SetActive(cost > 0);
            if (currencyIconText)
            {
                currencyIconText.gameObject.SetActive(cost > 0);
                currencyIconText.SetText(UIManager.GetCurrencyIcon(gm.GetPurchaseCurrency(_so.GunnerId)));
            }

            if (equipButton) { equipButton.gameObject.SetActive(true); equipButton.interactable = true; }
            if (unequipButton) unequipButton.gameObject.SetActive(false);
            return;
        }
        else if (equipText)
            equipText.text = "Equip";

        // Owned - behave like normal equip/unequip buttons
        UpdateEquipButtons();
    }


    // Helpers to adapt to your assets/meta -----------------------------------

    private string GetNiceName(GunnerStatKey key)
    {
        switch (key)
        {
            case GunnerStatKey.Damage: return "Damage";
            case GunnerStatKey.FireRate: return "Attack Speed";
            case GunnerStatKey.Range: return "Range";
            case GunnerStatKey.CriticalChance: return "Critical Chance";
            case GunnerStatKey.CriticalDamageMultiplier: return "Critical Damage";
            case GunnerStatKey.SlowEffect: return "Slow";
            case GunnerStatKey.KnockbackStrength: return "Knockback";
            case GunnerStatKey.SplashDamage: return "Splash";
            case GunnerStatKey.PierceChance: return "Pierce Chance";
            case GunnerStatKey.PierceDamageFalloff: return "Pierce Falloff";
            case GunnerStatKey.PercentBonusDamagePerSec: return "Damage Ramp";
            case GunnerStatKey.ArmorPenetration: return "Armor Penetration";
            case GunnerStatKey.Health: return "Health";
            default: return key.ToString();
        }
    }

    private string FormatValueFor(GunnerStatKey key, float v)
    {
        switch (key)
        {
            case GunnerStatKey.CriticalChance:
            case GunnerStatKey.SlowEffect:
            case GunnerStatKey.PierceChance:
            case GunnerStatKey.ArmorPenetration:
                return v.ToString("0.#") + "%";
            case GunnerStatKey.CriticalDamageMultiplier:
                return v.ToString("0.#") + "%";
            case GunnerStatKey.FireRate:
                return v.ToString("0.##") + "/s";
            default:
                return v.ToString("0.##");
        }
    }

    private Sprite GetIconFor(GunnerStatKey key)
    {
        // Pull from the centralized icon manager; it maps GunnerStatKey -> "stat.*" keys
        // and resolves the sprite from GameIconLibrarySO (with fallback if missing).
        return GameIconManager.Instance != null
            ? GameIconManager.Instance.IconForGunnerStat(key)
            : null;
    }


    // Buttons ----------------------------------------------------------------

    private void OnPrev()
    {
        if (roster == null || roster.Count == 0) return;
        int next = (currentIndex - 1 + roster.Count) % roster.Count;
        BindByIndex(next);
    }

    private void OnNext()
    {
        if (roster == null || roster.Count == 0) return;
        int next = (currentIndex + 1) % roster.Count;
        BindByIndex(next);
    }

    private void OnClickOpenUpgrades()
    {
        // Open your upgrade allocation panel for _so/_rt
        // or call into a UI manager; this class only gates by points.
        // Example:
        // GunnerUpgradePanel.Instance.Open(_so, _rt);
    }

    // If not owned yet, clicking acts as "Buy"
    private void OnEquip()
    {
        Debug.Log("GunnerDetailsUI: Equip clicked for " + (_so != null ? _so.GunnerId : "null SO"));
        if (_so == null || GunnerManager.Instance == null) return;

        var gm = GunnerManager.Instance;

        if (!gm.IsOwned(_so.GunnerId))
        {
            Debug.Log("GunnerDetailsUI: Attempting purchase of " + _so.GunnerId);
            if (gm.IsPurchasableNow(_so.GunnerId) && gm.TryPurchaseGunner(_so.GunnerId))
            {
                // Refresh runtime/state and switch UI to equip mode
                _rt = gm.GetRuntime(_so.GunnerId);
                Bind(_so);
                equipText.text = "Equip"; // switch from Buy to Equip
                equipButton.gameObject.SetActive(true);
                unequipButton.gameObject.SetActive(false);
                Debug.Log("GunnerDetailsUI: Purchase successful for " + _so.GunnerId);
            }
            return;
        }

        _rt = gm.GetRuntime(_so.GunnerId);
        if (_rt != null && _rt.EquippedSlot >= 0)
        {
            UpdateEquipButtons();
            if (closePanelOnEquip) Close();
            return;
        }

        // Begin slot selection as usual
        GunnerEquipFlow.Instance?.BeginSelectSlot(_so.GunnerId);

        if (closePanelOnEquip) Close();
    }

    private void OnUnequip()
    {
        if (_rt == null) return;
        if (_rt.EquippedSlot < 0) return;

        if (GunnerManager.Instance != null)
            GunnerManager.Instance.UnequipFromSlot(_rt.EquippedSlot);

        // Re-fetch runtime (slot changed to -1 inside manager)
        _rt = (GunnerManager.Instance != null) ? GunnerManager.Instance.GetRuntime(_so.GunnerId) : _rt;

        // Refresh interactables and list
        Bind(_so);

        UpdateEquipButtons();
    }

    private bool TryFindFirstFreeSlot(out int slotIndex, out Transform barrelAnchor)
    {
        slotIndex = -1;
        barrelAnchor = null;

        if (GunnerManager.Instance == null) return false;

        // Find all slots in scene and select the first one without a gunner.
        var slots = FindObjectsByType<SlotWorldButton>(FindObjectsSortMode.None);
        Array.Sort(slots, (a, b) => a.slotIndex.CompareTo(b.slotIndex));

        foreach (var s in slots)
        {
            string id = GunnerManager.Instance.GetEquippedGunnerId(s.slotIndex);
            if (string.IsNullOrEmpty(id))
            {
                slotIndex = s.slotIndex;
                barrelAnchor = s.barrelAnchor;
                return true;
            }
        }
        return false;
    }


}
