using Assets.Scripts.Enemies;
using Assets.Scripts.Systems;
using Assets.Scripts.Turrets;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    public float TurretRangeAuraAdd { get; private set; }

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

    // Tracks coroutine-based LBs per gunner so we can cancel them on death.
    private readonly Dictionary<string, List<Action>> _cancelActionsByGunner = new();


    // ==== Multi-session support ====
    private class ActiveLB
    {
        public string SessionId;
        public string OwnerGunnerId; // who started this session (used to cancel on death)

        public LBFocus Focus;
        public float MaxCap;
        public float Baseline;
        public float Current;     // raw pct (includes baseline)
        public float Remaining;   // seconds
        public float Total;       // seconds
        public Sprite Icon;
        public string DisplayName;
    }

    // Set briefly during TryActivate so skills that call ActivateDamageBoost/ActivateFireRateBoost
    // (without passing gunnerId) still get ownership.
    private string _activatingGunnerId = null;

    private readonly Dictionary<string, ActiveLB> _activeLBs = new();
    private float _aggDamagePct = 0f;
    private float _aggFirePct = 0f;

    public float TotalDamageBonusPct => _aggDamagePct;
    public float TotalFireRateBonusPct => _aggFirePct;
    public float TurretRangeMultiplier { get; private set; } = 1f;



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

    // Indicators & tuning
    [SerializeField] private GameObject circleIndicatorPrefab;
    [SerializeField] private GameObject lineIndicatorPrefab;
    [SerializeField] private float lineThickness = 0.75f;
    [SerializeField] private float maxAimDistance = 100f;

    // Dome (Iron Citadel): % reduction to gunners' incoming damage
    public float GunnerDamageReductionPct { get; private set; } = 0f;

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
        HandleLBHotkeys();

        // Any active sessions?
        if (_activeLBs.Count == 0)
        {
            // reset hold/decay
            isHolding = false;
            decreaseTimer = 0f;

            // reset aggregates
            _aggDamagePct = 0f;
            _aggFirePct = 0f;

            // >>> propagate to public, gameplay-facing properties <<<
            DamageMultiplier = 1f; // affects damage via LimitBreakDamageEffect
            FireRateMultiplier = 1f; // used by BaseTurret to speed up fire interval
            ClickDamageBonusPct = 0f; // keep 0 unless you separate click-only bonuses
            ClickSpeedBonusPct = 0f;

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

        // Aggregate totals for gameplay usage (raw % space)
        float d = 0f, f = 0f;
        foreach (var kv in _activeLBs)
        {
            var s = kv.Value;
            if (s.Focus == LBFocus.Damage) d += s.Current;
            if (s.Focus == LBFocus.FireRate) f += s.Current;
        }
        _aggDamagePct = d;
        _aggFirePct = f;

        // >>> propagate to public, gameplay-facing properties each frame <<<
        // Choose ONE place to reflect the effect. We use the multipliers:
        DamageMultiplier = 1f + (_aggDamagePct / 100f);  // e.g., 25% -> 1.25x
        FireRateMultiplier = 1f + (_aggFirePct / 100f);  // used in BaseTurret.Update()

        // If you want click-only bars separate, map them here; else keep at 0.
        ClickDamageBonusPct = 0f;
        ClickSpeedBonusPct = 0f;

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

        // Cooldown check (can be bypassed by debug)
        if (_onCooldown.Contains(gunnerId) && !lbDebugIgnoreCooldown) return false;

        var so = GunnerManager.Instance.GetSO(gunnerId);
        var rt = GunnerManager.Instance.GetRuntime(gunnerId);
        var skill = ResolveFor(so);
        if (so == null || rt == null || skill == null) return false;

        // Safety: dead gunners can't activate LB (prevents chatter + session ownership edge cases)
        if (rt.CurrentHealth <= 0f)
            return false;


        // Requirement: LB must be full (unless debug override)
        bool ready = (rt.LimitBreakCurrent >= rt.LimitBreakMax);
        if (!ready && !lbDebugIgnoreRequirements) return false;

        // Spend charge (unless debug no-cost)
        if (!lbDebugNoCost)
        {
            rt.LimitBreakCurrent = 0f;
            GunnerManager.Instance.NotifyLimitBreakChanged(gunnerId);
        }
        else
        {
            Debug.Log($"[LB DEBUG] Casting without cost (gunnerId={gunnerId})");
        }

        var ctx = new LimitBreakContext { GunnerId = gunnerId, GunnerSO = so, Runtime = rt };

        _activatingGunnerId = gunnerId;
        try
        {
            skill.Activate(ctx);
        }
        finally
        {
            _activatingGunnerId = null;
        }

        Assets.Scripts.Systems.Save.SaveGameManager.Instance?.SaveGame();

        // Notify chatter (unchanged)
        var chatter = FindFirstObjectByType<GunnerChatterSystem>();
        if (chatter != null)
        {
            chatter.TriggerEvent(GunnerEvent.LimitBreak, so, null, 1f);
        }
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
            OwnerGunnerId = _activatingGunnerId, // captured from TryActivate

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
            // If someone force-stopped this session (death), stop the coroutine immediately.
            if (!_activeLBs.ContainsKey(s.SessionId))
                yield break;

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

    private void HandleLBHotkeys()
    {
        if (UnityEngine.InputSystem.Keyboard.current == null)
            return;

        // Map number keys (top row and numpad) to slots 0..4
        int pressedSlot = -1;

        var kb = Keyboard.current;
        if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame) pressedSlot = 0;
        else if (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame) pressedSlot = 1;
        else if (kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame) pressedSlot = 2;
        else if (kb.digit4Key.wasPressedThisFrame || kb.numpad4Key.wasPressedThisFrame) pressedSlot = 3;
        else if (kb.digit5Key.wasPressedThisFrame || kb.numpad5Key.wasPressedThisFrame) pressedSlot = 4;

        if (pressedSlot < 0) return;

        // Optional: ignore if pointer is over UI so players can type in fields without triggering LBs
        if (PointerOverUI()) return;

        TryActivateBySlot(pressedSlot);
    }

    public bool TryActivateBySlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex > 4) return false;
        if (GunnerManager.Instance == null) return false;

        GunnerRuntime rt;
        bool gotRt = GunnerManager.Instance.TryGetEquippedRuntime(slotIndex, out rt);
        if (!gotRt || rt == null) return false;

        return TryActivate(rt.GunnerId);
    }

    private Vector3 GetMouseOnXZ(float y = 0f)
    {
        var cam = Camera.main;
        if (cam == null) return Vector3.zero;
        var ray = cam.ScreenPointToRay(Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero);
        if (new Plane(Vector3.up, new Vector3(0f, y, 0f)).Raycast(ray, out float t))
            return ray.GetPoint(t);
        return Vector3.zero;
    }

    private GameObject SpawnCircleIndicator(Vector3 pos, float radius, float duration)
    {
        if (circleIndicatorPrefab == null) return null;
        var go = Instantiate(circleIndicatorPrefab, pos, Quaternion.identity);
        go.transform.localScale = new Vector3(radius * 2f, 1f, radius * 2f);
        if (duration > 0f) Destroy(go, duration);
        return go;
    }

    private GameObject SpawnLineIndicator(Vector3 start, Vector3 end, float duration)
    {
        if (lineIndicatorPrefab == null)
        {
            // Simple runtime line if no prefab provided
            var go = new GameObject("LB_LineIndicator");
            var lr = go.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.useWorldSpace = true;
            lr.startWidth = lr.endWidth = lineThickness;
            lr.SetPosition(0, start);
            lr.SetPosition(1, end);
            lr.material = new Material(Shader.Find("Sprites/Default"));
            if (duration > 0f) Destroy(go, duration);
            return go;
        }
        var obj = Instantiate(lineIndicatorPrefab);
        var rend = obj.GetComponent<LineRenderer>() ?? obj.AddComponent<LineRenderer>();
        rend.positionCount = 2;
        rend.useWorldSpace = true;
        rend.startWidth = rend.endWidth = lineThickness;
        rend.SetPosition(0, start);
        rend.SetPosition(1, end);
        if (duration > 0f) Destroy(obj, duration);
        return obj;
    }

    // ===== Queries =====
    private static readonly List<Enemy> _enemyBuf = new List<Enemy>(256);

    // ===== Permafrost Domain (global slow) =====
    private readonly HashSet<int> _permafrostAffected = new HashSet<int>(512);
    private bool _permafrostActive = false;

    private int EnemiesInRadius(Vector3 pos, float radius, List<Enemy> into)
    {
        into.Clear();
        var near = GridManager.Instance.GetEnemiesInRange(pos, Mathf.CeilToInt(radius));
        if (near == null) return 0;
        float r2 = radius * radius;
        for (int i = 0; i < near.Count; i++)
        {
            var e = near[i];
            if (e == null || !e.IsAlive) continue;
            var p = e.transform.position;
            float dx = p.x - pos.x, dz = p.z - pos.z;
            if (dx * dx + dz * dz <= r2) into.Add(e);
        }
        return into.Count;
    }

    private int EnemiesInLine(Vector3 from, Vector3 to, float width, List<Enemy> into)
    {
        into.Clear();
        float w2 = width * width;
        var mid = (from + to) * 0.5f;
        float halfLen = Vector3.Distance(from, to) * 0.5f;

        // Broad phase: use radius = halfLen + width around segment midpoint
        float broad = halfLen + width;
        var near = GridManager.Instance.GetEnemiesInRange(mid, Mathf.CeilToInt(broad));
        if (near == null) return 0;

        Vector3 dir = (to - from).normalized;
        for (int i = 0; i < near.Count; i++)
        {
            var e = near[i];
            if (e == null || !e.IsAlive) continue;
            Vector3 p = e.transform.position;

            // point-segment dist^2 on XZ
            Vector3 a = new Vector3(from.x, 0f, from.z);
            Vector3 b = new Vector3(to.x, 0f, to.z);
            Vector3 v = b - a;
            Vector3 ap = new Vector3(p.x, 0f, p.z) - a;
            float t = Mathf.Clamp01(Vector3.Dot(ap, v) / Mathf.Max(0.0001f, v.sqrMagnitude));
            Vector3 proj = a + v * t;
            float d2 = (new Vector3(p.x, 0f, p.z) - proj).sqrMagnitude;

            if (d2 <= w2) into.Add(e);
        }
        return into.Count;
    }

    #region ACTIVATE METHODS FOR EACH LB TYPE

    // 1) Survivor’s Roar → Damage LB (click boosts already supported)
    public void ActivateSurvivorsRoar(float mult, float duration, LimitBreakSkillSO s)
    {
        StartCoroutine(RunLBSession(LBFocus.Damage, mult, duration, s));
    }

    // 2) Boil Jet → hold to steer a piercing steam line
    public void ActivateBoilJet(string gunnerId, float dps, float width, float duration, LimitBreakSkillSO skill)
    {
        StartCoroutine(Co_BoilJet(gunnerId, dps, width, duration, skill));
    }
    private IEnumerator Co_BoilJet(string gunnerId, float dps, float width, float duration, LimitBreakSkillSO skill)
    {
        // Resolve the slot for this gunner
        var gm = GunnerManager.Instance;
        var rt = gm != null ? gm.GetRuntime(gunnerId) : null;
        int slot = (rt != null) ? rt.EquippedSlot : -1;
        if (slot < 0) yield break;

        // Find the scene turret bound to that slot (existing helper)
        BaseTurret turret = FindTurretBySlot(slot);
        if (turret == null) yield break;

        // Prefer the muzzle flash point as origin; fall back to the turret transform
        Transform originT = (turret._rotationPoint != null) ? turret._rotationPoint : turret.transform;

        // Start a timer-only LB bar (no slider)
        string sessionId = Guid.NewGuid().ToString("N");
        bool cancelled = false;
        bool ended = false;

        OnLBSessionStarted?.Invoke(new LBSessionInfo
        {
            SessionId = sessionId,
            DisplayName = (skill != null && !string.IsNullOrWhiteSpace(skill.DisplayName)) ? skill.DisplayName : "Boil Jet",
            Icon = (skill != null) ? skill.Icon : null,
            MaxClickCap = 0f,
            BaselinePct = 0f,
            Focus = LBFocus.None
        });

        Vector3 startNow = originT.position;
        startNow = new Vector3(startNow.x, startNow.y + 1f, startNow.z);

        GameObject lineObj = SpawnLineIndicator(startNow, startNow, 0f);
        var lr = (lineObj != null) ? lineObj.GetComponent<LineRenderer>() : null;
        if (lr != null) lr.startWidth = lr.endWidth = Mathf.Max(0.01f, width);

        // Register cancellation owned by this gunner
        Action cancelAction = () =>
        {
            if (ended) return;
            cancelled = true;

            if (lineObj != null) Destroy(lineObj);

            ended = true;
            OnLBSessionEnded?.Invoke(sessionId);
        };
        RegisterCancelAction(gunnerId, cancelAction);

        float t = duration;
        while (t > 0f && !cancelled)
        {
            // Optional: also stop if the gunner is already dead (extra safety)
            var rtCheck = GunnerManager.Instance != null ? GunnerManager.Instance.GetRuntime(gunnerId) : null;
            if (rtCheck == null || rtCheck.CurrentHealth <= 0f) break;

            Vector3 start = originT.position;
            start = new Vector3(start.x, start.y + 1f, start.z);

            Vector3 aim = GetMouseOnXZ(start.y);
            Vector3 dir = (aim - start);
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.001f) dir = Vector3.forward;
            dir.Normalize();

            Vector3 end = start + dir * maxAimDistance;

            if (lr != null)
            {
                lr.positionCount = 2;
                lr.useWorldSpace = true;
                lr.SetPosition(0, start);
                lr.SetPosition(1, end);
            }

            int n = EnemiesInLine(start, end, width, _enemyBuf);
            float tick = Mathf.Min(Time.deltaTime, t);
            float damage = dps * tick;
            for (int i = 0; i < n; i++)
            {
                var e = _enemyBuf[i];
                if (e != null && e.IsAlive)
                    e.TakeDamage(damage, armorPenetrationPct: 0f, isAoe: true);
            }

            OnLBSessionTick?.Invoke(sessionId, 0f, t, duration);

            t -= Time.deltaTime;
            yield return null;
        }

        // Normal end (if not cancelled)
        if (!ended)
        {
            if (lineObj != null) Destroy(lineObj);
            ended = true;
            OnLBSessionEnded?.Invoke(sessionId);
        }

        // Always unregister
        UnregisterCancelAction(gunnerId, cancelAction);

    }

    // 3) Heat Vent → radial push + small heal to gunners
    public void ActivateHeatVent(float radius, float knockSeconds, float knockSpeed, float healPct)
    {
        StartCoroutine(Co_HeatVent(radius, knockSeconds, knockSpeed, healPct));
    }

    private IEnumerator Co_HeatVent(float radius, float knockTime, float knockSpeed, float healPct)
    {
        Vector3 center = new Vector3(0f, 0f, GridManager.Instance.NearZ + 2f);
        SpawnCircleIndicator(center, radius, 0.25f);

        int n = EnemiesInRadius(center, radius, _enemyBuf);
        for (int i = 0; i < n; i++)
        {
            var e = _enemyBuf[i];
            Vector3 dir = (e.transform.position - center); dir.y = 0f; dir.x = 0f;
            dir = dir.normalized * knockSpeed;
            e.KnockbackVelocity = new Vector2(dir.x, dir.z);
            e.KnockbackTime = knockTime;
            e.TakeDamage(Mathf.Max(1f, e.MaxHealth * 0.02f), 0f, isAoe: true); // light scorch
        }

        // Heal equipped gunners by % of max
        GunnerManager.Instance?.HealEquippedPercent(healPct);

        yield break;
    }

    // 4) Cinderslash → click-to-confirm: double strike + place a pooled trap (turret-owned)
    public void ActivateCinderslash(string gunnerId, float damage, float radius, float apPct, float lineWidth,
                                float trapRadius, float trapDamage, float trapDelay,
                                GameObject trapPrefab, int trapPoolSize)
    {
        StartCoroutine(Co_Cinderslash_Targeted(gunnerId, damage, radius, apPct, lineWidth,
                                               trapRadius, trapDamage, trapDelay,
                                               trapPrefab, trapPoolSize));
    }


    public float GetEffectiveGunnerDamage(LimitBreakContext ctx)
    {
        if (ctx.GunnerSO == null || ctx.Runtime == null) return 0f;

        // Prefer upgrade-aware values (this matches how gunners scale elsewhere)
        var up = GunnerUpgradeManager.Instance;
        float dmg;

        if (up != null)
        {
            dmg = up.GetEffectiveStatValue(ctx.GunnerSO, ctx.Runtime, GunnerStatKey.Damage);
        }
        else
        {
            dmg = ctx.GunnerSO.BaseDamage;
        }

        // If you ever allow Damage to be locked for some gunners, you can gate it here:
        // if (ctx.Runtime.Unlocked != null && !ctx.Runtime.Unlocked.Contains(GunnerStatKey.Damage)) dmg = 0f;

        return Mathf.Max(0f, dmg);
    }

    /// <summary>
    /// Utility for "flat OR % of gunner damage" power scaling with optional clamps.
    /// pctOfGunnerDamage: 0 means "disabled" and uses flatPower.
    /// </summary>
    public float ResolvePower(LimitBreakContext ctx, float flatPower, float pctOfGunnerDamage, float minPower = 0f, float maxPower = float.PositiveInfinity)
    {
        float p = flatPower;

        if (pctOfGunnerDamage > 0f)
        {
            float gunnerDmg = GetEffectiveGunnerDamage(ctx);
            p = gunnerDmg * (pctOfGunnerDamage / 100f);
        }

        if (!float.IsPositiveInfinity(maxPower))
            p = Mathf.Min(p, maxPower);

        p = Mathf.Max(p, minPower);
        return p;
    }


    private IEnumerator Co_Cinderslash_Targeted(string gunnerId, float soSlashMul, float radius, float apPct, float unusedWidth,
                                            float trapRadius, float soTrapMul, float trapDelay,
                                            GameObject lbTrapPrefab, int lbPoolSize)
    {
        // Resolve slot & turret
        var gm = GunnerManager.Instance;
        var rt = gm != null ? gm.GetRuntime(gunnerId) : null;
        var so = gm != null ? gm.GetSO(gunnerId) : null;
        int slot = (rt != null) ? rt.EquippedSlot : -1;
        if (slot < 0) yield break;

        BaseTurret turret = FindTurretBySlot(slot);
        if (turret == null) yield break;
                
        // Effective gunner damage 
        float gunnerDmg = Mathf.Max(0f, GetEffectiveGunnerDamage(new LimitBreakContext
        {
            GunnerId = gunnerId,
            GunnerSO = so,
            Runtime = rt
        }));

        // Default to 200% if SO sends 0 (you can tune per-SO later)
        float slashDamage = gunnerDmg * (soSlashMul > 0f ? soSlashMul : 2f);
        float trapDamage = gunnerDmg * (soTrapMul > 0f ? soTrapMul : 2f);

        // Debounce the press that opened LB
        if (Mouse.current != null && Mouse.current.leftButton.isPressed)
            yield return new WaitUntil(() => Mouse.current.leftButton.wasReleasedThisFrame);

        // Live circular indicator at mouse, snapped cell size
        float cellSize = GridManager.Instance._cellSize;
        float indicatorRadius = Mathf.Max(trapRadius, cellSize * 0.5f);
        GameObject indicator = null;

        while (true)
        {
            Vector3 hover = GetMouseOnXZ(0f);
            Vector2Int hoverCell = GridManager.Instance.GetGridPosition(hover);
            Vector3 cellCenter = GridManager.Instance.GetWorldPosition(hoverCell, 0f);

            if (indicator == null) indicator = SpawnCircleIndicator(cellCenter, indicatorRadius, 0f);
            else { indicator.transform.position = cellCenter; indicator.transform.localScale = new Vector3(indicatorRadius * 2f, 1f, indicatorRadius * 2f); }

            if (!PointerOverUI() && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                if (indicator != null) Destroy(indicator);

                // 1) Immediate hit: damage enemies in a radius around the click, ignore armor
                float clickRadius = radius;
                TrapPoolManager.Instance.InitializePool(lbTrapPrefab, lbPoolSize);
                int n = EnemiesInRadius(cellCenter, clickRadius, _enemyBuf);
                for (int i = 0; i < n; i++)
                {
                    var e = _enemyBuf[i];
                    if (e != null && e.IsAlive)
                        e.TakeDamage(slashDamage, armorPenetrationPct: 100f, isAoe: true);
                }

                // 2) Place a pooled trap at that cell (no rotation)
                if (TrapPoolManager.Instance == null || lbTrapPrefab == null || lbPoolSize <= 0)
                {
                    Debug.LogWarning("[Cinderslash] Trap pool/prefab not set.");
                    yield break;
                }
                                

                // Update the method call to match the correct signature of PlaceTrap
                var placed = TrapPoolManager.Instance.PlaceTrap(
                    cellCenter, // Vector3 worldPos
                    hoverCell,  // Vector2Int cell
                    trapDamage, // float damage
                    trapDelay,  // float delay
                    trapRadius, // float radius
                    null,     // BaseTurret owner
                    0f          // float cellWorldY
                );

                if (placed == null)
                {
                    Debug.LogWarning("[Cinderslash] No free LB trap instance available.");
                }
                else
                {
                    // If this prefab is a LimitBreakTrap, ensure it uses flat AoE and ignores armor.
                    var lbTrap = placed as LimitBreakTrap;
                    if (lbTrap != null)
                    {
                        lbTrap.ConfigureLB(
                            absoluteDamage: trapDamage,
                            apPct: 100f,
                            radiusOverride: Mathf.Max(0f, trapRadius),
                            delayOverride: Mathf.Max(0f, trapDelay)
                        );
                    }
                }
                yield break;
            }

            yield return null;
        }
    }


    // Helper: find the scene turret bound to a slot (uses BaseTurret.SlotIndex)
    private BaseTurret FindTurretBySlot(int slotIndex)
    {
        var slotMgr = TurretSlotManager.Instance;
        var invMgr = TurretInventoryManager.Instance;
        if (slotMgr == null || invMgr == null) return null;

        // 1) Get the equipped runtime stats for this slot (fast)
        var stats = slotMgr.Get(slotIndex); // TurretStatsInstance or null
        if (stats == null) return null;

        // 2) Resolve the live scene GameObject from the inventory map (O(1))
        var go = invMgr.GetGameObjectForInstance(stats);
        if (go != null)
        {
            var t = go.GetComponent<BaseTurret>();
            if (t != null) return t;
        }

        // 3) Fallback (rare): we failed to find the GO in the map.
        //    Do a one-time scene scan, then re-register so next time is O(1).
#if UNITY_2022_1_OR_NEWER
        var found = FindObjectsByType<BaseTurret>(FindObjectsSortMode.None)
                    .FirstOrDefault(bt => bt.SlotIndex == slotIndex);
#else
    var found = GameObject.FindObjectsOfType<Assets.Scripts.Turrets.BaseTurret>(true)
                .FirstOrDefault(bt => bt.SlotIndex == slotIndex);
#endif
        if (found != null)
        {
            invMgr.RegisterTurretInstance(stats, found.gameObject);
            return found;
        }

        return null;
    }

    // 5) Cryo Burst → radial slow + light heal to allies
    public void ActivateCryoBurst(float radius, float slowPct, float slowSeconds, float healPct)
    {
        StartCoroutine(Co_CryoBurst(radius, slowPct, slowSeconds, healPct));
    }

    private IEnumerator Co_CryoBurst(float radius, float slowPct, float slowSeconds, float healPct)
    {
        Vector3 center = new Vector3(0f, 0f, GridManager.Instance.NearZ + 3f);
        SpawnCircleIndicator(center, radius, 0.25f);

        int n = EnemiesInRadius(center, radius, _enemyBuf);
        for (int i = 0; i < n; i++)
            _enemyBuf[i].ReduceMovementSpeed(slowPct);

        // decay the slow after a few seconds by restoring speed via smaller negative
        yield return new WaitForSeconds(slowSeconds);

        // Minor team heal
        GunnerManager.Instance?.HealEquippedPercent(healPct);
    }

    // 6) Treefall Slam → click a spot; big AoE punch once
    public void ActivateTreefallSlam(float radius, float damage)
    {
        StartCoroutine(Co_TreefallSlam(radius, damage));
    }

    private IEnumerator Co_TreefallSlam(float radius, float damage)
    {
        Vector3 aim = GetMouseOnXZ(0f);
        SpawnCircleIndicator(aim, radius, 0.25f);
        yield return new WaitForSeconds(0.12f);

        int n = EnemiesInRadius(aim, radius, _enemyBuf);
        for (int i = 0; i < n; i++) _enemyBuf[i].TakeDamage(damage, armorPenetrationPct: 10f, isAoe: true);
    }

    // 7) Tidepush → radial knockback + modest damage
    public void ActivateTidepush(float radius, float knockTime, float knockSpeed, float damage)
    {
        StartCoroutine(Co_Tidepush(radius, knockTime, knockSpeed, damage));
    }
    private IEnumerator Co_Tidepush(float radius, float knockTime, float knockSpeed, float damage)
    {
        Vector3 center = new Vector3(0f, 0f, GridManager.Instance.NearZ + 2.5f);
        SpawnCircleIndicator(center, radius, 0.2f);

        int n = EnemiesInRadius(center, radius, _enemyBuf);
        for (int i = 0; i < n; i++)
        {
            var e = _enemyBuf[i];
            Vector3 dir = (e.transform.position - center); dir.y = 0f;
            dir = dir.normalized * knockSpeed;
            e.KnockbackVelocity = new Vector2(dir.x, dir.z);
            e.KnockbackTime = knockTime;
            e.TakeDamage(damage, 0f, isAoe: true);
        }
        yield break;
    }

    // 8) Iron Citadel → temporary dome: reduce all incoming damage for gunners
    public void ActivateIronCitadel(float reducePct, float duration)
    {
        StartCoroutine(Co_IronCitadel(reducePct, duration));
    }
    private IEnumerator Co_IronCitadel(float reducePct, float duration)
    {
        GunnerDamageReductionPct = Mathf.Clamp(reducePct, 0f, 95f);
        var dome = SpawnCircleIndicator(new Vector3(0f, 0f, GridManager.Instance.NearZ + 2f), 6f, duration);
        yield return new WaitForSeconds(duration);
        GunnerDamageReductionPct = 0f;
    }

    // 9) Heat Management → personal overhealth (shield) + temporary damage penalty
    public void ActivateHeatManagement(string gunnerId, float extraHP, float selfDamagePenaltyPct, float duration)
    {
        StartCoroutine(Co_HeatManagement(gunnerId, extraHP, selfDamagePenaltyPct, duration));
    }
    private IEnumerator Co_HeatManagement(string gunnerId, float extraHP, float selfPenalty, float duration)
    {
        var rt = GunnerManager.Instance?.GetRuntime(gunnerId);
        if (rt == null) yield break;

        float beforeMax = rt.MaxHealth;
        rt.MaxHealth += extraHP;
        rt.CurrentHealth = Mathf.Min(rt.MaxHealth, rt.CurrentHealth + extraHP);

        // negative damage session (shows in the bar if you want)
        StartCoroutine(RunLBSession(LBFocus.Damage, 1f - (selfPenalty / 100f), duration, null));

        // Update bars
        GunnerManager.Instance?.NotifyLimitBreakChanged(gunnerId);

        yield return new WaitForSeconds(duration);

        // Revert max; keep current clamped
        rt.MaxHealth = beforeMax;
        rt.CurrentHealth = Mathf.Min(rt.CurrentHealth, rt.MaxHealth);
        GunnerManager.Instance?.NotifyLimitBreakChanged(gunnerId);
    }

    // 10) Anima Infusion → global attack speed boost + small team heal
    public void ActivateAnimaInfusion(float fireRateMult, float duration, float healPct)
    {
        StartCoroutine(RunLBSession(LBFocus.FireRate, fireRateMult, duration, null));
        GunnerManager.Instance?.HealEquippedPercent(healPct);
    }

    // Frost Infusion -> instant piercing "needle wave" that damages + slows.
    // damage is already resolved by the SkillSO (flat or % of gunner dmg).
    public void ActivateFrostInfusion(
        string gunnerId,
        float damage,
        float length,
        float width,
        float slowPct,
        float slowSeconds,
        float armorPenetrationPct = 0f,
        LimitBreakSkillSO skill = null)
    {
        StartCoroutine(Co_FrostInfusion(
            gunnerId, damage, length, width, slowPct, slowSeconds, armorPenetrationPct, skill));
    }

    private IEnumerator Co_FrostInfusion(
    string gunnerId,
    float damage,
    float length,
    float width,
    float slowPct,
    float slowSeconds,
    float armorPenetrationPct,
    LimitBreakSkillSO skill)
    {
        // Resolve the slot for this gunner
        var gm = GunnerManager.Instance;
        var rt = gm != null ? gm.GetRuntime(gunnerId) : null;
        int slot = (rt != null) ? rt.EquippedSlot : -1;
        if (slot < 0) yield break;

        // Find the live turret in the scene
        BaseTurret turret = FindTurretBySlot(slot);
        if (turret == null) yield break;

        // Origin: muzzle/rotation point (same as Boil Jet)
        Transform originT = (turret._rotationPoint != null) ? turret._rotationPoint : turret.transform;

        Vector3 start = originT.position;
        start = new Vector3(start.x, start.y + 1f, start.z);

        // Aim once at activation time
        Vector3 aim = GetMouseOnXZ(start.y);
        Vector3 dir = (aim - start);
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.001f)
            dir = Vector3.forward;

        dir.Normalize();

        float clampedLen = Mathf.Max(0.25f, length);
        Vector3 end = start + dir * clampedLen;

        // Quick indicator
        GameObject lineObj = SpawnLineIndicator(start, end, 0.18f);
        var lr = (lineObj != null) ? lineObj.GetComponent<LineRenderer>() : null;
        if (lr != null) lr.startWidth = lr.endWidth = Mathf.Max(0.01f, width);

        // Hit enemies in the line (piercing)
        int n = EnemiesInLine(start, end, width, _enemyBuf);
        if (n <= 0) yield break;

        // Store who we slowed so we can restore later
        // (Local list avoids allocations across frames)
        List<Enemy> slowed = new List<Enemy>(n);

        for (int i = 0; i < n; i++)
        {
            var e = _enemyBuf[i];
            if (e == null || !e.IsAlive) continue;

            e.TakeDamage(damage, armorPenetrationPct: armorPenetrationPct, isAoe: true);

            if (slowPct > 0f)
            {
                e.ReduceMovementSpeed(slowPct);
                slowed.Add(e);
            }
        }

        // Restore slow after duration (IMPORTANT: see note below)
        if (slowSeconds > 0f && slowed.Count > 0 && slowPct > 0f)
        {
            yield return new WaitForSeconds(slowSeconds);

            // Assumption: ReduceMovementSpeed is additive and accepts negative to undo.
            // If your Enemy implementation clamps or doesn't support negative values,
            // I’ll show you the alternative fix right after this code section.
            for (int i = 0; i < slowed.Count; i++)
            {
                var e = slowed[i];
                if (e == null || !e.IsAlive) continue;
                e.ReduceMovementSpeed(-slowPct);
            }
        }
    }

    // Permafrost Domain → massively slow all enemies for several seconds (no recovery by design)
    public void ActivatePermafrostDomain(float radius, float slowPct, float durationSeconds)
    {
        StartCoroutine(Co_PermafrostDomain(radius, slowPct, durationSeconds));
    }

    private IEnumerator Co_PermafrostDomain(float radius, float slowPct, float durationSeconds)
    {
        // Mark active (if later you want to read this from elsewhere)
        _permafrostActive = true;

        // IMPORTANT: clear affected set so each cast is independent
        _permafrostAffected.Clear();

        // Center: use the same “battlefield-ish” convention as other LBs.
        // CryoBurst uses NearZ + 3f【turn7file9†LimitBreakManager.cs†L16-L18】, so we’ll keep it consistent.
        Vector3 center = new Vector3(0f, 0f, GridManager.Instance.NearZ + 3f);

        // Optional visual indicator that lasts the whole domain
        SpawnCircleIndicator(center, radius, durationSeconds);

        float t = durationSeconds;

        // Tick so that enemies spawning during the domain also get slowed,
        // but each enemy gets slowed only once (prevents stacking).
        const float tickInterval = 0.15f;

        while (t > 0f)
        {
            int n = EnemiesInRadius(center, radius, _enemyBuf);

            for (int i = 0; i < n; i++)
            {
                var e = _enemyBuf[i];
                if (e == null || !e.IsAlive) continue;

                int id = e.GetInstanceID();
                if (_permafrostAffected.Contains(id)) continue;

                _permafrostAffected.Add(id);

                // Your existing convention: ReduceMovementSpeed(slowPct)【turn7file9†LimitBreakManager.cs†L19-L22】
                // Since you *want* no recovery, we do not restore later.
                e.ReduceMovementSpeed(slowPct);
            }

            float dt = Mathf.Min(tickInterval, t);
            t -= dt;
            yield return new WaitForSeconds(dt);
        }

        _permafrostActive = false;
    }

    public void ActivateSandstormCollapse(
    string gunnerId,
    float dps,
    float startRadius,
    float endRadius,
    float duration,
    float armorPenetrationPct = 0f,
    LimitBreakSkillSO skill = null)
    {
        StartCoroutine(Co_SandstormCollapse(
            gunnerId, dps, startRadius, endRadius, duration, armorPenetrationPct, skill));
    }

    private IEnumerator Co_SandstormCollapse(
    string gunnerId,
    float dps,
    float startRadius,
    float endRadius,
    float duration,
    float armorPenetrationPct,
    LimitBreakSkillSO skill)
    {
        // Safety
        if (duration <= 0.01f) yield break;
        dps = Mathf.Max(0f, dps);

        // Center of battlefield (consistent with your other “global center” LBs)
        Vector3 center = new Vector3(0f, 0f, GridManager.Instance.NearZ + 3f);

        float r0 = Mathf.Max(0f, startRadius);
        float r1 = Mathf.Max(r0, endRadius);

        // Visual that persists; we’ll scale it manually (so don’t auto-destroy here)
        GameObject vfx = SpawnCircleIndicator(center, r0, 0f);
        if (vfx != null)
            vfx.transform.localScale = new Vector3(r0 * 2f, 1f, r0 * 2f);

        float t = duration;

        while (t > 0f)
        {
            // Optional: stop if gunner died (like your Boil Jet safety)
            var rtCheck = GunnerManager.Instance != null ? GunnerManager.Instance.GetRuntime(gunnerId) : null;
            if (rtCheck == null || rtCheck.CurrentHealth <= 0f) break;

            float dt = Mathf.Min(Time.deltaTime, t);
            float u = 1f - (t / duration); // 0 -> 1

            float r = Mathf.Lerp(r0, r1, u);

            if (vfx != null)
                vfx.transform.localScale = new Vector3(r * 2f, 1f, r * 2f);

            int n = EnemiesInRadius(center, r, _enemyBuf); // your grid query helper :contentReference[oaicite:4]{index=4}

            float damage = dps * dt; // same DPS tick model you already use :contentReference[oaicite:5]{index=5}
            for (int i = 0; i < n; i++)
            {
                var e = _enemyBuf[i];
                if (e != null && e.IsAlive)
                    e.TakeDamage(damage, armorPenetrationPct: armorPenetrationPct, isAoe: true);
            }

            t -= dt;
            yield return null;
        }

        if (vfx != null) Destroy(vfx);
    }

    // Steamblossom Burst → click a spot; spawn multiple LB traps around it
    public void ActivateSteamblossomBurst(
        string gunnerId,
        int trapCount,
        float spawnRadiusWorld,
        float trapRadius,
        float trapDelay,
        float armorPenetrationPct,
        float flatTrapDamage,
        float trapDamagePctOfGunnerDamage,
        GameObject lbTrapPrefab,
        int lbPoolSize
    )
    {
        StartCoroutine(Co_SteamblossomBurst_Targeted(
            gunnerId,
            trapCount,
            spawnRadiusWorld,
            trapRadius,
            trapDelay,
            armorPenetrationPct,
            flatTrapDamage,
            trapDamagePctOfGunnerDamage,
            lbTrapPrefab,
            lbPoolSize
        ));
    }

    private IEnumerator Co_SteamblossomBurst_Targeted(
        string gunnerId,
        int trapCount,
        float spawnRadiusWorld,
        float trapRadius,
        float trapDelay,
        float armorPenetrationPct,
        float flatTrapDamage,
        float trapDamagePctOfGunnerDamage,
        GameObject lbTrapPrefab,
        int lbPoolSize
    )
    {
        // Resolve slot & turret (same style as Cinderslash)
        var gm = GunnerManager.Instance;
        var rt = gm != null ? gm.GetRuntime(gunnerId) : null;
        var so = gm != null ? gm.GetSO(gunnerId) : null;
        int slot = (rt != null) ? rt.EquippedSlot : -1;
        if (slot < 0) yield break;

        BaseTurret turret = FindTurretBySlot(slot);
        if (turret == null) yield break;

        // Guard
        trapCount = Mathf.Max(1, trapCount);
        spawnRadiusWorld = Mathf.Max(0f, spawnRadiusWorld);
        trapRadius = Mathf.Max(0f, trapRadius);
        trapDelay = Mathf.Max(0f, trapDelay);

        // Pool
        if (TrapPoolManager.Instance == null || lbTrapPrefab == null)
        {
            Debug.LogWarning("[SteamblossomBurst] TrapPoolManager or prefab missing.");
            yield break;
        }
        TrapPoolManager.Instance.InitializePool(lbTrapPrefab, lbPoolSize);
                
        // Damage scaling (upgrade-aware)
        var ctx = new LimitBreakContext
        {
            GunnerId = gunnerId,
            GunnerSO = so,
            Runtime = rt
        };

        float gunnerDmg = Mathf.Max(0f, GetEffectiveGunnerDamage(ctx));
        float perTrapDamage = flatTrapDamage;


        if (trapDamagePctOfGunnerDamage > 0f)
            perTrapDamage = gunnerDmg * (trapDamagePctOfGunnerDamage / 100f);

        perTrapDamage = Mathf.Max(0f, perTrapDamage);

        // Debounce the press that opened LB (same as Cinderslash)
        if (Mouse.current != null && Mouse.current.leftButton.isPressed)
            yield return new WaitUntil(() => Mouse.current.leftButton.wasReleasedThisFrame);

        // Live indicator at mouse (snap to cell center)
        float indicatorRadius = Mathf.Max(spawnRadiusWorld, GridManager.Instance._cellSize * 0.5f);
        GameObject indicator = null;

        while (true)
        {
            Vector3 hover = GetMouseOnXZ(0f);
            Vector2Int hoverCell = GridManager.Instance.GetGridPosition(hover);
            Vector3 cellCenter = GridManager.Instance.GetWorldPosition(hoverCell, 0f);

            if (indicator == null) indicator = SpawnCircleIndicator(cellCenter, indicatorRadius, 0f);
            else
            {
                indicator.transform.position = cellCenter;
                indicator.transform.localScale = new Vector3(indicatorRadius * 2f, 1f, indicatorRadius * 2f);
            }

            if (!PointerOverUI() && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                if (indicator != null) Destroy(indicator);

                // Decide which cells get traps
                int maxCellRadius = WorldRadiusToCellRadius(spawnRadiusWorld);
                var cells = BuildRingCells(hoverCell, trapCount, maxCellRadius);

                // Place traps
                for (int i = 0; i < cells.Count; i++)
                {
                    Vector2Int c = cells[i];
                    Vector3 wp = GridManager.Instance.GetWorldPosition(c, 0f);

                    var placed = TrapPoolManager.Instance.PlaceTrap(
                        wp,       // worldPos
                        c,        // cell
                        perTrapDamage,
                        trapDelay,
                        trapRadius,
                        null,     // owner turret is null for LB traps (matches Cinderslash usage)
                        0f
                    );

                    if (placed == null)
                        continue;

                    var lbTrap = placed as LimitBreakTrap;
                    if (lbTrap != null)
                    {
                        lbTrap.ConfigureLB(
                            absoluteDamage: perTrapDamage,
                            apPct: Mathf.Clamp(armorPenetrationPct, 0f, 100f),
                            radiusOverride: trapRadius,
                            delayOverride: trapDelay
                        );
                    }
                }

                yield break;
            }

            yield return null;
        }
    }

    // 11) Abyssal Hymn → heal all equipped gunners + charge other equipped gunners' limit breaks
    public void ActivateAbyssalHymn(string casterGunnerId, float healPct, float chargePctOfTargetMax, bool includeCaster = false)
    {
        var gm = GunnerManager.Instance;
        if (gm == null) return;

        // 1) Heal (uses your existing, proven path)
        gm.HealEquippedPercent(healPct);

        // 2) Charge others
        foreach (var gid in gm.EnumerateEquippedGunnerIds())
        {
            if (string.IsNullOrEmpty(gid)) continue;
            if (!includeCaster && gid == casterGunnerId) continue;

            var rt = gm.GetRuntime(gid);
            if (rt == null) continue;

            float add = rt.LimitBreakMax * (Mathf.Max(0f, chargePctOfTargetMax) / 100f);
            rt.LimitBreakCurrent = Mathf.Min(rt.LimitBreakMax, rt.LimitBreakCurrent + add);

            gm.NotifyLimitBreakChanged(gid);
        }
    }

    // ===================== POLEN PULSE =====================
    // Damages enemies in an AoE once per tick while active (damage is % of gunner damage),
    // and grants +Range to ALL turrets while active. Clicking ramps both effects.

    public void ActivatePolenPulse(
    string gunnerId,
    float duration,
    float tickInterval,
    float radiusWorld,
    float minRangeBoostPct,
    float maxRangeBoostPct,
    float minDamagePctOfGunnerDamage,
    float maxDamagePctOfGunnerDamage,
    float clickGain,
    float maxClickCharge,
    LimitBreakSkillSO skill
)
    {
        StartCoroutine(Co_PolenPulse(
            gunnerId,
            duration,
            tickInterval,
            radiusWorld,
            minRangeBoostPct,
            maxRangeBoostPct,
            minDamagePctOfGunnerDamage,
            maxDamagePctOfGunnerDamage,
            clickGain,
            maxClickCharge,
            skill
        ));
    }

    private IEnumerator Co_PolenPulse(
        string gunnerId,
        float duration,
        float tickInterval,
        float radiusWorld,
        float minRangeBoostPct,
        float maxRangeBoostPct,
        float minDmgPct,
        float maxDmgPct,
        float clickGain,
        float maxClickCharge,
        LimitBreakSkillSO skill
    )
    {
        var gm = GunnerManager.Instance;
        var rt = gm != null ? gm.GetRuntime(gunnerId) : null;
        var so = gm != null ? gm.GetSO(gunnerId) : null;
        int slot = (rt != null) ? rt.EquippedSlot : -1;
        if (slot < 0) yield break;

        BaseTurret turret = FindTurretBySlot(slot);
        if (turret == null) yield break;

        // Session bookkeeping (matches your existing event signature)
        string sessionId = $"{gunnerId}_POLEN_{Time.frameCount}";
        OnLBSessionStarted?.Invoke(new LBSessionInfo
        {
            SessionId = sessionId,
            DisplayName = (skill != null && !string.IsNullOrWhiteSpace(skill.DisplayName)) ? skill.DisplayName : "Polen Pulse",
            Icon = (skill != null) ? skill.Icon : null,
            MaxClickCap = maxClickCharge,
            BaselinePct = 0f,
            Focus = LBFocus.None
        });


        // Optional: show indicator ring
        var ring = SpawnCircleIndicator(turret.transform.position, radiusWorld, duration);

        // Click scaling state
        float clickCharge = 0f;

        // Save previous range mult so multiple LBs don’t stomp each other
        float prevRangeMult = TurretRangeMultiplier;

        float tRemaining = duration;
        float tickLeft = 0f;

        // Debounce activation click
        if (Mouse.current != null && Mouse.current.leftButton.isPressed)
            yield return new WaitUntil(() => Mouse.current.leftButton.wasReleasedThisFrame);

        while (tRemaining > 0f)
        {
            // Stop if gunner dies mid-LB
            if (rt != null && rt.IsDead) break;

            // Click to increase more
            if (!PointerOverUI() && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                clickCharge = Mathf.Min(maxClickCharge, clickCharge + Mathf.Max(0f, clickGain));

            float alpha = (maxClickCharge <= 0f) ? 0f : (clickCharge / maxClickCharge);

            float rangeBoostPct = Mathf.Lerp(minRangeBoostPct, maxRangeBoostPct, alpha);
            TurretRangeMultiplier = prevRangeMult * (1f + (rangeBoostPct / 100f));

            // Tick damage once per interval
            tickLeft -= Time.deltaTime;
            if (tickLeft <= 0f)
            {
                tickLeft = Mathf.Max(0.05f, tickInterval);

                // Effective gunner damage (upgrade-aware)
                float gunnerDmg = Mathf.Max(0f, GetEffectiveGunnerDamage(new LimitBreakContext
                {
                    GunnerId = gunnerId,
                    GunnerSO = so,
                    Runtime = rt
                }));

                float dmgPct = Mathf.Lerp(minDmgPct, maxDmgPct, alpha);
                float hitDamage = gunnerDmg * (dmgPct / 100f);

                Vector3 center = turret.transform.position;
                int n = EnemiesInRadius(center, radiusWorld, _enemyBuf);
                for (int i = 0; i < n; i++)
                {
                    var e = _enemyBuf[i];
                    if (e != null && e.IsAlive)
                        e.TakeDamage(hitDamage, armorPenetrationPct: 0f, isAoe: true);
                }
            }

            // Session tick event
            OnLBSessionTick?.Invoke(sessionId, clickCharge, tRemaining, duration);

            tRemaining -= Time.deltaTime;
            yield return null;
        }

        // Cleanup
        TurretRangeMultiplier = prevRangeMult;
        if (ring != null) Destroy(ring);
        OnLBSessionEnded?.Invoke(sessionId);
    }

    // X) Anchor Drop -> click a spot; after a short delay, AoE damage + radial knockback
    public void ActivateAnchorDrop(
        float radius,
        float impactDelay,
        float knockTime,
        float knockSpeed,
        float damage,
        float armorPenetrationPct,
        GameObject impactPrefab,
        float impactPrefabYOffset
    )
    {
        StartCoroutine(Co_AnchorDrop(radius, impactDelay, knockTime, knockSpeed, damage, armorPenetrationPct, impactPrefab, impactPrefabYOffset));
    }

    private IEnumerator Co_AnchorDrop(
        float radius,
        float impactDelay,
        float knockTime,
        float knockSpeed,
        float damage,
        float armorPenetrationPct,
        GameObject impactPrefab,
        float impactPrefabYOffset
    )
    {
        Vector3 aim = GetMouseOnXZ(0f);

        // Visual telegraph
        SpawnCircleIndicator(aim, radius, 0.25f);

        // Optional VFX
        if (impactPrefab != null)
        {
            Vector3 vfxPos = aim + Vector3.up * impactPrefabYOffset;
            Instantiate(impactPrefab, vfxPos, Quaternion.identity);
        }

        if (impactDelay > 0f)
            yield return new WaitForSeconds(impactDelay);

        int n = EnemiesInRadius(aim, radius, _enemyBuf);
        for (int i = 0; i < n; i++)
        {
            var e = _enemyBuf[i];
            if (e == null || !e.IsAlive) continue;

            // Radial knockback away from impact
            Vector3 dir = (e.transform.position - aim);
            dir.y = 0f;

            if (dir.sqrMagnitude < 0.0001f)
                dir = Vector3.forward;

            dir = dir.normalized * knockSpeed;

            e.KnockbackVelocity = new Vector2(dir.x, dir.z);
            e.KnockbackTime = knockTime;

            // AoE damage
            e.TakeDamage(damage, armorPenetrationPct: armorPenetrationPct, isAoe: true);
        }
    }

    public void ActivateLavaArc(
    LimitBreakContext ctx,
    GameObject prefab,
    Vector3 spawnOffset,
    float totalDuration,
    float forwardDistance,
    float sideDistance,
    float damage,
    float armorPenetrationPct,
    float damageRadius,
    float tickRate,
    float perEnemyHitInterval,
    float spinDps
)
    {
        StartCoroutine(Co_LavaArc(
            ctx, prefab, spawnOffset, totalDuration, forwardDistance, sideDistance,
            damage, armorPenetrationPct, damageRadius, tickRate, perEnemyHitInterval, spinDps
        ));
    }

    private IEnumerator Co_LavaArc(
        LimitBreakContext ctx,
        GameObject prefab,
        Vector3 spawnOffset,
        float totalDuration,
        float forwardDistance,
        float sideDistance,
        float damage,
        float armorPenetrationPct,
        float damageRadius,
        float tickRate,
        float perEnemyHitInterval,
        float spinDps
    )
    {
        // Resolve origin + forward without asking player to aim.
        // If your ctx exposes a gunner transform, use it. Otherwise fallback to a global origin.
        Transform originTr = TryGetLimitBreakOrigin(ctx);
        Vector3 origin = (originTr != null) ? originTr.position : Vector3.zero;
        Vector3 forward = (originTr != null) ? originTr.forward : Vector3.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f) forward = Vector3.forward;
        forward.Normalize();

        // Side direction in XZ
        Vector3 side = Vector3.Cross(Vector3.up, forward).normalized;

        Vector3 start = origin + spawnOffset;

        // Outbound end point (forward + side)
        Vector3 outEnd = start + forward * forwardDistance + side * sideDistance;

        // Return uses a different curve so it feels like a boomerang arc
        Vector3 returnCtrl = start + forward * (forwardDistance * 0.35f) - side * (sideDistance * 1.25f);
        Vector3 outCtrl = start + forward * (forwardDistance * 0.55f) + side * (sideDistance * 1.1f);

        Transform boomerang = null;
        if (prefab != null)
        {
            var go = Instantiate(prefab, start, Quaternion.LookRotation(forward, Vector3.up));
            boomerang = go.transform;
        }

        // Per-enemy hit limiter for THIS activation only
        var lastHitTime = new System.Collections.Generic.Dictionary<int, float>(128);

        float dt = (tickRate <= 0f) ? 0.02f : (1f / tickRate);
        float half = Mathf.Max(0.01f, totalDuration * 0.5f);

        // Outbound
        yield return RunPathProjectile(
            boomerang,
            duration: half,
            step: dt,
            spinDps: spinDps,
            pathPos: (t) => QuadraticBezier(start, outCtrl, outEnd, t),
            damageRadius: damageRadius,
            damage: damage,
            armorPenetrationPct: armorPenetrationPct,
            perEnemyHitInterval: perEnemyHitInterval,
            lastHitTime: lastHitTime
        );

        // Return
        yield return RunPathProjectile(
            boomerang,
            duration: half,
            step: dt,
            spinDps: spinDps,
            pathPos: (t) => QuadraticBezier(outEnd, returnCtrl, start, t),
            damageRadius: damageRadius,
            damage: damage,
            armorPenetrationPct: armorPenetrationPct,
            perEnemyHitInterval: perEnemyHitInterval,
            lastHitTime: lastHitTime
        );

        if (boomerang != null)
            Destroy(boomerang.gameObject);
    }

    private IEnumerator RunPathProjectile(
        Transform projectile,
        float duration,
        float step,
        float spinDps,
        System.Func<float, Vector3> pathPos,
        float damageRadius,
        float damage,
        float armorPenetrationPct,
        float perEnemyHitInterval,
        System.Collections.Generic.Dictionary<int, float> lastHitTime
    )
    {
        float t = 0f;
        while (t < duration)
        {
            float u = Mathf.Clamp01(t / duration);
            Vector3 pos = pathPos(u);

            if (projectile != null)
            {
                projectile.position = pos;
                if (spinDps != 0f)
                    projectile.Rotate(0f, spinDps * step, 0f, Space.Self);
            }

            ApplyPathDamageTick(
                pos,
                damageRadius,
                damage,
                armorPenetrationPct,
                perEnemyHitInterval,
                lastHitTime
            );

            t += step;
            yield return new WaitForSeconds(step);
        }

        // Final tick at u=1
        Vector3 finalPos = pathPos(1f);
        if (projectile != null) projectile.position = finalPos;
        ApplyPathDamageTick(finalPos, damageRadius, damage, armorPenetrationPct, perEnemyHitInterval, lastHitTime);
    }

    private void ApplyPathDamageTick(
        Vector3 center,
        float radius,
        float damage,
        float armorPenetrationPct,
        float perEnemyHitInterval,
        System.Collections.Generic.Dictionary<int, float> lastHitTime
    )
    {
        int n = EnemiesInRadius(center, radius, _enemyBuf);
        float now = Time.time;

        for (int i = 0; i < n; i++)
        {
            var e = _enemyBuf[i];
            if (e == null || !e.IsAlive) continue;

            int id = e.GetInstanceID();
            if (perEnemyHitInterval > 0f && lastHitTime.TryGetValue(id, out float last))
            {
                if ((now - last) < perEnemyHitInterval)
                    continue;
            }

            lastHitTime[id] = now;
            e.TakeDamage(damage, armorPenetrationPct: armorPenetrationPct, isAoe: true);
        }
    }

    private static Vector3 QuadraticBezier(Vector3 a, Vector3 b, Vector3 c, float t)
    {
        float u = 1f - t;
        return (u * u * a) + (2f * u * t * b) + (t * t * c);
    }

    // Try to keep origin resolution generic / expandable.
    // Replace this with whatever your project uses consistently.
    private Transform TryGetLimitBreakOrigin(LimitBreakContext ctx)
    {
        // Use zero position for now.
        return transform;

    }

    public void ActivateRustExecution(string gunnerId, float damageMultiplier, float armorPenetrationPct)
    {
        StartCoroutine(Co_RustExecution(gunnerId, damageMultiplier, armorPenetrationPct));
    }

    private IEnumerator Co_RustExecution(string gunnerId, float damageMultiplier, float armorPenetrationPct)
    {
        // Resolve gunner
        var gm = GunnerManager.Instance;
        var rt = gm != null ? gm.GetRuntime(gunnerId) : null;
        var so = gm != null ? gm.GetSO(gunnerId) : null;
        if (rt == null || so == null) yield break;

        // Dead gunners shouldn't execute
        if (rt.CurrentHealth <= 0f) yield break;

        // Effective gunner damage (same approach as Cinderslash)
        float gunnerDmg = Mathf.Max(0f, GetEffectiveGunnerDamage(new LimitBreakContext
        {
            GunnerId = gunnerId,
            GunnerSO = so,
            Runtime = rt
        }));

        float dmg = gunnerDmg * Mathf.Max(0f, damageMultiplier);

        // Live circular indicator at mouse, snapped to cell size (same as Cinderslash)
        float cellSize = GridManager.Instance._cellSize;

        // Keep this small so it really feels like "click the enemy"
        float pickRadius = Mathf.Max(cellSize * 0.5f, 0.75f);

        GameObject indicator = null;

        while (true)
        {
            Vector3 hover = GetMouseOnXZ(0f);
            Vector2Int hoverCell = GridManager.Instance.GetGridPosition(hover);
            Vector3 cellCenter = GridManager.Instance.GetWorldPosition(hoverCell, 0f);

            if (indicator == null) indicator = SpawnCircleIndicator(cellCenter, pickRadius, 0f);
            else
            {
                indicator.transform.position = cellCenter;
                indicator.transform.localScale = new Vector3(pickRadius * 2f, 1f, pickRadius * 2f);
            }

            if (!PointerOverUI() && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                // Find enemies near the clicked cell center
                int n = EnemiesInRadius(cellCenter, pickRadius, _enemyBuf);

                Enemy target = null;

                // Choose the "best" enemy inside the click radius.
                // This matches "click an enemy" in a no-collider world:
                // pick closest to the click point.
                float bestSqr = float.PositiveInfinity;

                for (int i = 0; i < n; i++)
                {
                    var e = _enemyBuf[i];
                    if (e == null || !e.IsAlive) continue;

                    float sqr = (e.transform.position - cellCenter).sqrMagnitude;
                    if (sqr < bestSqr)
                    {
                        bestSqr = sqr;
                        target = e;
                    }
                }

                // If no enemy was clicked, keep waiting (same UX as requested)
                if (target == null)
                    continue;

                if (indicator != null) Destroy(indicator);

                // Execute: single target, massive damage, ignore armor
                target.TakeDamage(dmg, armorPenetrationPct: armorPenetrationPct, isAoe: false);
                yield break;
            }

            yield return null;
        }


    }

    public void ActivatePressureSync(float fireRateMultiplier, float duration, float healPct, LimitBreakSkillSO skill)
    {
        // Heal gunners (exact same pattern as Heat Vent)
        GunnerManager.Instance?.HealEquippedPercent(healPct);

        // Fire rate buff uses your existing LB session system
        ActivateFireRateBoost(fireRateMultiplier, duration, skill);
    }

    public void ActivateColdExecution(
    string gunnerId,
    float range,
    float angleDeg,
    float damageMultiplier,
    float armorPenetrationPct,
    float slowPct,
    float slowSeconds
)
    {
        StartCoroutine(Co_ColdExecution(gunnerId, range, angleDeg, damageMultiplier, armorPenetrationPct, slowPct, slowSeconds));
    }

    private IEnumerator Co_ColdExecution(
        string gunnerId,
        float range,
        float angleDeg,
        float damageMultiplier,
        float armorPenetrationPct,
        float slowPct,
        float slowSeconds
    )
    {
        var gm = GunnerManager.Instance;
        var rt = gm != null ? gm.GetRuntime(gunnerId) : null;
        var so = gm != null ? gm.GetSO(gunnerId) : null;
        if (rt == null || so == null) yield break;

        // Use the turret slot forward as the cone direction if possible
        int slot = rt.EquippedSlot;
        BaseTurret turret = (slot >= 0) ? FindTurretBySlot(slot) : null;

        Vector3 origin = (turret != null) ? turret.transform.position : new Vector3(0f, 0f, GridManager.Instance.NearZ + 2f);
        Vector3 forward = (turret != null) ? turret.transform.forward : Vector3.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f) forward = Vector3.forward;
        forward.Normalize();

        // Compute damage from effective gunner damage (same as Cinderslash/Rust Execution)
        float gunnerDmg = Mathf.Max(0f, GetEffectiveGunnerDamage(new LimitBreakContext
        {
            GunnerId = gunnerId,
            GunnerSO = so,
            Runtime = rt
        }));

        float dmg = gunnerDmg * Mathf.Max(0f, damageMultiplier);

        // Broad phase
        int n = EnemiesInRadius(origin, range, _enemyBuf);
        float cosThreshold = Mathf.Cos(Mathf.Deg2Rad * (angleDeg * 0.5f));

        // Track slowed enemies so we can restore later
        List<Enemy> slowed = null;
        if (slowSeconds > 0f && slowPct > 0f) slowed = new List<Enemy>(n);

        for (int i = 0; i < n; i++)
        {
            var e = _enemyBuf[i];
            if (e == null || !e.IsAlive) continue;

            Vector3 to = e.transform.position - origin;
            to.y = 0f;
            if (to.sqrMagnitude < 0.0001f) continue;

            Vector3 dir = to.normalized;
            float dot = Vector3.Dot(forward, dir);
            if (dot < cosThreshold) continue;

            e.TakeDamage(dmg, armorPenetrationPct: armorPenetrationPct, isAoe: true);

            if (slowed != null)
            {
                e.ReduceMovementSpeed(slowPct); // SAME as Permafrost Domain
                slowed.Add(e);
            }
        }

        if (slowed != null && slowed.Count > 0)
        {
            yield return new WaitForSeconds(slowSeconds);

            // Your existing convention elsewhere: pass negative to undo
            for (int i = 0; i < slowed.Count; i++)
            {
                var e = slowed[i];
                if (e == null || !e.IsAlive) continue;
                e.ReduceMovementSpeed(-slowPct);
            }
        }
    }


    #endregion

    // Helper for trap placement
    private int WorldRadiusToCellRadius(float worldRadius)
    {
        float cell = Mathf.Max(0.0001f, GridManager.Instance._cellSize);
        return Mathf.Max(0, Mathf.RoundToInt(worldRadius / cell));
    }

    /// <summary>
    /// Deterministic spread: center, then rings around it.
    /// Avoids random so the feel is consistent and testable.
    /// </summary>
    private List<Vector2Int> BuildRingCells(Vector2Int center, int count, int maxCellRadius)
    {
        var result = new List<Vector2Int>(count);

        // Always include center first
        result.Add(center);
        if (count == 1) return result;

        int r = 1;
        while (result.Count < count && r <= Mathf.Max(1, maxCellRadius))
        {
            // square ring perimeter
            for (int x = -r; x <= r && result.Count < count; x++)
            {
                result.Add(new Vector2Int(center.x + x, center.y + r));
                if (result.Count >= count) break;
                result.Add(new Vector2Int(center.x + x, center.y - r));
            }

            for (int y = -r + 1; y <= r - 1 && result.Count < count; y++)
            {
                result.Add(new Vector2Int(center.x + r, center.y + y));
                if (result.Count >= count) break;
                result.Add(new Vector2Int(center.x - r, center.y + y));
            }

            r++;
        }

        // If maxCellRadius was small and we still need more, just keep expanding.
        while (result.Count < count)
        {
            result.Add(new Vector2Int(center.x + r, center.y));
            r++;
        }

        return result;
    }


    // --- DEBUG toggles (put near your other serialized fields) ---
    [SerializeField] private bool lbDebugIgnoreRequirements = false; // allow cast when bar not full
    [SerializeField] private bool lbDebugIgnoreCooldown = true;      // allow cast while on cooldown
    [SerializeField] private bool lbDebugNoCost = true;              // do not spend charge when casting

    [ContextMenu("LB Debug: Enable (IgnoreReq, IgnoreCD, NoCost)")]
    public void EnableLBDebug()
    {
        lbDebugIgnoreRequirements = true;
        lbDebugIgnoreCooldown = true;
        lbDebugNoCost = true;
        Debug.Log("[LB DEBUG] Enabled: IgnoreRequirements=ON, IgnoreCooldown=ON, NoCost=ON");
    }

    [ContextMenu("LB Debug: Disable")]
    public void DisableLBDebug()
    {
        lbDebugIgnoreRequirements = false;
        lbDebugIgnoreCooldown = false;
        lbDebugNoCost = false;
        Debug.Log("[LB DEBUG] Disabled");
    }

    public void SetLBDebug(bool ignoreRequirements, bool ignoreCooldown, bool noCost)
    {
        lbDebugIgnoreRequirements = ignoreRequirements;
        lbDebugIgnoreCooldown = ignoreCooldown;
        lbDebugNoCost = noCost;
        Debug.Log($"[LB DEBUG] Set: IgnoreReq={(ignoreRequirements ? "ON" : "OFF")}, IgnoreCD={(ignoreCooldown ? "ON" : "OFF")}, NoCost={(noCost ? "ON" : "OFF")}");
    }

    // CANCEL LBs WHEN A GUNNER DIES 
    public void StopAllForGunner(string gunnerId)
    {
        //Debug.Log("Stopping all LB sessions for gunnerId=" + gunnerId + " active count " + _activeLBs.Count);
        if (string.IsNullOrEmpty(gunnerId)) return;

        // 1) Cancel coroutine-based LBs owned by this gunner (BoilJet, targeted skills, etc.)
        if (_cancelActionsByGunner.TryGetValue(gunnerId, out var cancels) && cancels != null)
        {
            // Copy to avoid modification during iteration
            var copy = cancels.ToArray();
            for (int i = 0; i < copy.Length; i++)
            {
                try { copy[i]?.Invoke(); }
                catch (Exception ex) { Debug.LogException(ex); }
            }
            _cancelActionsByGunner.Remove(gunnerId);
        }

        // 2) Cancel tracked (Update-driven) sessions
        if (_activeLBs.Count == 0) return;

        var toStop = new List<string>();

        Debug.Log("passed the stop checks");

        foreach (var kv in _activeLBs)
        {
            var s = kv.Value;
            if (s != null && s.OwnerGunnerId == gunnerId)
                toStop.Add(kv.Key);
        }

        for (int i = 0; i < toStop.Count; i++)
        {
            string sessionId = toStop[i];

            // Notify UI this session ended immediately
            OnLBSessionEnded?.Invoke(sessionId);

            _activeLBs.Remove(sessionId);
        }

        // If that was the last session(s), input/decay will self-reset next Update() early-return.
        // But make the "stop" feel immediate:
        if (_activeLBs.Count == 0)
        {
            isHolding = false;
            decreaseTimer = 0f;

            _aggDamagePct = 0f;
            _aggFirePct = 0f;

            DamageMultiplier = 1f;
            FireRateMultiplier = 1f;
            ClickDamageBonusPct = 0f;
            ClickSpeedBonusPct = 0f;
        }
    }

    private void RegisterCancelAction(string gunnerId, Action cancel)
    {
        if (string.IsNullOrEmpty(gunnerId) || cancel == null) return;
        if (!_cancelActionsByGunner.TryGetValue(gunnerId, out var list))
        {
            list = new List<Action>(2);
            _cancelActionsByGunner[gunnerId] = list;
        }
        list.Add(cancel);
    }

    private void UnregisterCancelAction(string gunnerId, Action cancel)
    {
        if (string.IsNullOrEmpty(gunnerId) || cancel == null) return;
        if (_cancelActionsByGunner.TryGetValue(gunnerId, out var list))
        {
            list.Remove(cancel);
            if (list.Count == 0) _cancelActionsByGunner.Remove(gunnerId);
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

