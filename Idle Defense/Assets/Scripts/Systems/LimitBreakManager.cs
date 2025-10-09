using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class LimitBreakManager : MonoBehaviour
{
    public static LimitBreakManager Instance { get; private set; }

    [Header("Registry")]
    [Tooltip("All Limit Break ScriptableObjects available in the game.")]
    [SerializeField] private List<LimitBreakSkillSO> allSkills = new List<LimitBreakSkillSO>();

    [Header("Input (optional)")]
    [SerializeField] private GraphicRaycaster uiRaycaster; // to ignore clicks over UI

    // Multipliers (timed by LB)
    public float FireRateMultiplier { get; private set; } = 1f;
    public float DamageMultiplier { get; private set; } = 1f;

    // Click-to-boost (only while LB active)
    public float ClickSpeedBonusPct { get; private set; } = 0f; // affects fire rate
    public float ClickDamageBonusPct { get; private set; } = 0f; // affects damage

    // Tuning for click behavior
    [SerializeField] private float maxClickBonus = 100f;
    [SerializeField] private float holdIncreaseRate = 5f;
    [SerializeField] private float initialBoost = 5f;
    [SerializeField] private float decreaseDelay = 10f;

    private float decreaseTimer = 0f;
    private bool isHolding = false;

    private readonly Dictionary<LimitBreakType, LimitBreakSkillSO> byType = new();
    private readonly HashSet<string> _onCooldown = new();

    // LB active flags
    private bool fireRateLBActive = false;
    private bool damageLBActive = false;

    // --- Event-driven UI bridge ---
    // (focus, maxClickCap, baselinePct, totalDuration)
    public static event System.Action<LBFocus, float, float, float> OnLBWindowStarted;
    public static event System.Action OnLBWindowEnded;
    // Visual value for the slider (already baseline-subtracted)
    public static event System.Action<float> OnClickBonusChanged;
    // (remaining, total) every frame of LB
    public static event System.Action<float, float> OnLBTimerUpdated;
    // When a character starts an LB session
    public static event Action<LBSessionInfo> OnLBSessionStarted;
    // Each frame for that session
    public static event Action<string, float, float, float> OnLBSessionTick; // (sessionId, rawPct, remaining, total)
                                                                             // When it ends
    public static event Action<string> OnLBSessionEnded;

    // ==== Multi-session support ====
    private class ActiveLB
    {
        public string SessionId;
        public LBFocus Focus;
        public float MaxCap;
        public float Baseline;
        public float Current;     // raw pct (includes baseline)
        public float Remaining;   // seconds
        public float Total;       // seconds
        public Sprite Icon;
        public string DisplayName;
    }

    private readonly Dictionary<string, ActiveLB> _activeLBs = new();
    private float _aggDamagePct = 0f;
    private float _aggFirePct = 0f;

    public float TotalDamageBonusPct => _aggDamagePct;
    public float TotalFireRateBonusPct => _aggFirePct;


    // Default cap (used when no LB active)
    [SerializeField] private float defaultMaxClickCap = 100f;

    // Runtime cap/baseline for the active LB session
    private float _currentMaxClickCap = 100f;
    private float _baselinePct = 0f;

    private LBFocus _currentFocus = LBFocus.None;
    private float _lastReportedClickPct = -1f; // force initial event

    // Input
    private PlayerInput input;
    private PointerEventData ped;
    private readonly List<RaycastResult> raycastResults = new();

    private Coroutine fireRateRoutine;
    private Coroutine damageRoutine;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;

        // Build registry
        byType.Clear();
        foreach (var s in allSkills)
        {
            if (s == null) continue;
            byType[s.Type] = s;
        }

        ped = new PointerEventData(EventSystem.current);
    }

    private void OnEnable()
    {
        input = new PlayerInput();
        input.Player.Click.performed += OnClickStarted;
        input.Player.Click.canceled += OnClickReleased;
        input.Player.Enable();
    }

    private void OnDisable()
    {
        if (input != null)
        {
            input.Player.Click.performed -= OnClickStarted;
            input.Player.Click.canceled -= OnClickReleased;
            input.Player.Disable();
            input = null;
        }
    }

    private void Update()
    {
        // Any active sessions?
        if (_activeLBs.Count == 0)
        {
            isHolding = false;
            decreaseTimer = 0f;
            _aggDamagePct = 0f;
            _aggFirePct = 0f;
            return;
        }

        // Adjust each session's Current by input/decay
        if (isHolding)
        {
            foreach (var kv in _activeLBs)
            {
                var s = kv.Value;
                s.Current += holdIncreaseRate * Time.deltaTime;
                s.Current = Mathf.Clamp(s.Current, 0f, s.MaxCap);
            }
            decreaseTimer = 0f;
        }
        else
        {
            if (decreaseTimer >= decreaseDelay)
            {
                foreach (var kv in _activeLBs)
                {
                    var s = kv.Value;
                    s.Current -= holdIncreaseRate * 0.8f * Time.deltaTime;
                    s.Current = Mathf.Clamp(s.Current, 0f, s.MaxCap);
                }
            }
            else
            {
                decreaseTimer += Time.deltaTime;
            }
        }

        // Aggregate totals for gameplay usage
        float d = 0f, f = 0f;
        foreach (var kv in _activeLBs)
        {
            var s = kv.Value;
            if (s.Focus == LBFocus.Damage) d += s.Current;
            if (s.Focus == LBFocus.FireRate) f += s.Current;
        }
        _aggDamagePct = d;
        _aggFirePct = f;

        // (Legacy single-bar events can be omitted now; UI uses per-session events)

    }

    private bool PointerOverUI()
    {
        if (uiRaycaster == null) return false;
        ped.position = Pointer.current != null ? Pointer.current.position.ReadValue() : Vector2.zero;
        raycastResults.Clear();
        uiRaycaster.Raycast(ped, raycastResults);
        return raycastResults.Count > 0;
    }

    private void OnClickStarted(InputAction.CallbackContext _)
    {
        if (PointerOverUI()) return;

        // Only start boosting if a relevant LB is active
        if (_activeLBs.Count > 0)
        {
            foreach (var kv in _activeLBs)
            {
                kv.Value.Current = Mathf.Clamp(kv.Value.Current + initialBoost, 0f, kv.Value.MaxCap);
            }
            isHolding = true;
            decreaseTimer = 0f;
        }
    }

    private void OnClickReleased(InputAction.CallbackContext _)
    {
        isHolding = false;
        decreaseTimer = 0f;
    }

    /// <summary>Return the resolved skill for a gunner.</summary>
    public LimitBreakSkillSO ResolveFor(GunnerSO so)
    {
        if (so == null) return null;
        if (so.LimitBreakSkill != null) return so.LimitBreakSkill;
        return null; // or lookup by type if you use it
    }

    public Sprite GetIconFor(GunnerSO so)
    {
        var skill = ResolveFor(so);
        return skill != null ? skill.Icon : null;
    }

    public bool TryActivate(string gunnerId)
    {
        if (string.IsNullOrEmpty(gunnerId) || GunnerManager.Instance == null) return false;
        if (_onCooldown.Contains(gunnerId)) return false;

        var so = GunnerManager.Instance.GetSO(gunnerId);
        var rt = GunnerManager.Instance.GetRuntime(gunnerId);
        var skill = ResolveFor(so);
        if (so == null || rt == null || skill == null) return false;
        if (rt.LimitBreakCurrent < rt.LimitBreakMax) return false;

        rt.LimitBreakCurrent = 0f;
        GunnerManager.Instance.NotifyLimitBreakChanged(gunnerId);

        var ctx = new LimitBreakContext { GunnerId = gunnerId, GunnerSO = so, Runtime = rt };
        skill.Activate(ctx);
        Assets.Scripts.Systems.Save.SaveGameManager.Instance?.SaveGame();
        return true;
    }

    // ===== Public API that skills call =====
    public void ActivateFireRateBoost(float multiplier, float duration, LimitBreakSkillSO skill)
    {
        StartCoroutine(RunLBSession(LBFocus.FireRate, multiplier, duration, skill));
    }

    public void ActivateDamageBoost(float multiplier, float duration, LimitBreakSkillSO skill)
    {
        StartCoroutine(RunLBSession(LBFocus.Damage, multiplier, duration, skill));
    }

    private IEnumerator RunLBSession(LBFocus focus, float multiplier, float duration, LimitBreakSkillSO skill)
    {
        // Cap from skill (or fallback to Magnitude as cap) — raw space
        float cap = (skill != null && skill.ClickMaxBonusPct > 0f) ? skill.ClickMaxBonusPct : Mathf.Max(0f, (multiplier - 1f) * 100f);
        float baseline = (skill != null) ? Mathf.Clamp(skill.BaselinePct, 0f, cap) : 0f;

        string sessionId = Guid.NewGuid().ToString("N");

        var s = new ActiveLB
        {
            SessionId = sessionId,
            Focus = focus,
            MaxCap = cap,
            Baseline = baseline,
            Current = baseline, // start at baseline (raw)
            Remaining = duration,
            Total = duration,
            Icon = (skill != null) ? skill.Icon : null,
            DisplayName = (skill != null && !string.IsNullOrWhiteSpace(skill.DisplayName))
                            ? skill.DisplayName
                            : (focus == LBFocus.Damage ? "Damage Boost" : "Fire Rate Boost")
        };

        _activeLBs[sessionId] = s;

        // Notify UI: a new bar has begun
        OnLBSessionStarted?.Invoke(new LBSessionInfo
        {
            SessionId = s.SessionId,
            DisplayName = s.DisplayName,
            Icon = s.Icon,
            MaxClickCap = s.MaxCap,
            BaselinePct = s.Baseline,
            Focus = s.Focus
        });

        // Timer loop
        while (s.Remaining > 0f)
        {
            // Emit tick (UI reads raw Current, also needs remaining/total for the timer)
            OnLBSessionTick?.Invoke(s.SessionId, s.Current, s.Remaining, s.Total);
            s.Remaining -= Time.deltaTime;
            yield return null;
        }

        // End
        OnLBSessionEnded?.Invoke(s.SessionId);
        _activeLBs.Remove(s.SessionId);
    }

    // UI Helper
    private void RecomputeUIFocusAndNotify()
    {
        // Decide priority if both are active. You can invert if you prefer.
        LBFocus newFocus =
            damageLBActive ? LBFocus.Damage :
            fireRateLBActive ? LBFocus.FireRate :
            LBFocus.None;

        if (newFocus != _currentFocus)
        {
            _currentFocus = newFocus;

            if (_currentFocus == LBFocus.None)
            {
                OnLBWindowEnded?.Invoke();
                _lastReportedClickPct = -1f;
                OnClickBonusChanged?.Invoke(0f);
            }
            else
            {
                // duration: when a window opens, we don't know remaining yet; UI will get ticks immediately after
                float assumedDuration = 0f; // UI doesn't need this value; ticks will arrive next frame
                OnLBWindowStarted?.Invoke(_currentFocus, _currentMaxClickCap, _baselinePct, assumedDuration);
                _lastReportedClickPct = -1f;
            }
        }
    }

}

public enum LBFocus
{
    None = 0,
    FireRate = 1,
    Damage = 2
}

public struct LBSessionInfo
{
    public string SessionId;     // unique per activation (e.g., $"{charId}-{Time.frameCount}")
    public string DisplayName;   // e.g., "Damage Boost" or character name + short effect
    public Sprite Icon;          // from LimitBreakSkillSO.Icon or character portrait
    public float MaxClickCap;    // 0..cap
    public float BaselinePct;    // starting fill (raw space)
    public LBFocus Focus;        // which slider to show
}

