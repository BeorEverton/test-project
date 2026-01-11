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


    private float GetEffectiveGunnerDamage(string gunnerId, GunnerSO so)
    {
        // Prefer a runtime/effective value if you have it in your GunnerManager; fallback to BaseDamage
        float baseDmg = so != null ? so.BaseDamage : 0f;
        try
        {
            // If you already expose something like this, use it:
            // return Mathf.Max(0f, GunnerManager.Instance.GetEffectiveGunnerDamage(gunnerId));
            return Mathf.Max(0f, baseDmg);
        }
        catch { return Mathf.Max(0f, baseDmg); }
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
        float gunnerDmg = Mathf.Max(0f, GetEffectiveGunnerDamage(gunnerId, so));
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

