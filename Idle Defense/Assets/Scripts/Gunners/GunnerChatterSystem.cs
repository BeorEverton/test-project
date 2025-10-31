using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class GunnerChatterSystem : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Active gunners in the scene (update this list when gunners are equipped/unequipped).")]
    public List<GunnerSO> ActiveGunners = new List<GunnerSO>();

    [Header("Chances")]
    [Range(0f, 1f)] public float PeriodicChatChance = 0.15f;  // chance every tick to spark a convo
    [Range(0f, 1f)] public float EventAutoChatChance = 0.25f; // chance on events (kill, crit, wave start, etc.)
    [Range(0f, 1f)] public float PositiveBias = 0.55f;        // bias to pick praise vs negative on generic triggers
    [Min(0f)] public float PeriodicTickSeconds = 8f;         // how often we try to chat idly

    [Header("Idle (inactive)")]
    [Tooltip("Every X seconds, try to have an unequipped (not in quest) gunner say an idle line.")]
    [SerializeField, Min(1f)] private float InactiveIdleTickSeconds = 12f;
    [SerializeField, Range(0f, 1f)] private float InactiveIdleChance = 0.25f;

    private float _inactiveIdleTimer;

    [Header("Weights")]
    public float SameAreaBonus = 1.2f;
    public float SameMoodBonus = 1.15f;
    public float CrossAreaPenalty = 0.9f;

    [Header("Affinity")]
    [Tooltip("Base affinity added to pairs from the same area.")]
    public float BaseAreaAffinity = 0.2f;
    [Tooltip("How much to nudge affinity when two gunners chat successfully.")]
    public float AffinityGainPerChat = 0.05f;
    [Tooltip("Clamp for affinity values (-1..+1).")]
    public Vector2 AffinityClamp = new Vector2(-1f, 1f);

    [Header("UI")]
    [SerializeField] private GunnerSpeechUIOverlay speechUIBehaviour; // must implement IGunnerSpeechUI

    [Header("Dialogue")]
    [Tooltip("Token inside phrase text that will be replaced with the listener's name.")]
    [SerializeField] private string nameToken = "*name*";
    [Tooltip("Color used to render the listener's name when token is replaced.")]
    [SerializeField] private Color nameColor = new Color(0.6f, 0.9f, 1f); // light-cyan
    [Tooltip("Time (seconds) between speaker and reply.")]
    [SerializeField, Min(0f)] private float replyDelaySeconds = 0.5f;
    [Tooltip("Chance the listener replies on two-person chats.")]
    [SerializeField, Range(0f, 1f)] private float replyChance = 1f; // default always reply

    private float _timer;
    private GunnerAffinityStore _affinity = new GunnerAffinityStore();
    public static GunnerChatterSystem Instance { get; private set; }
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // === Public API ===

    public void ForcePraise(GunnerSO speaker, GunnerSO maybeListener = null)
    {
        TrySpeakFromList(speaker, maybeListener, speaker.PraisePhrases);
        LearnAffinity(speaker, maybeListener, +AffinityGainPerChat);
    }

    public void ForceNegative(GunnerSO speaker, GunnerSO maybeListener = null)
    {
        TrySpeakFromList(speaker, maybeListener, speaker.NegativePhrases);
        LearnAffinity(speaker, maybeListener, -AffinityGainPerChat * 0.5f);
    }

    public void TriggerEvent(GunnerEvent evt, GunnerSO actor, GunnerSO optionalOther = null, float? chanceOverride = null)
    {
        //Debug.Log($"[GunnerChatterSystem] Event triggered: {evt} by {actor?.GunnerId} towards {optionalOther?.GunnerId}");
        float p = chanceOverride.HasValue ? Mathf.Clamp01(chanceOverride.Value) : EventAutoChatChance;
        if (Random.value > p) return;

        //Debug.Log($"[GunnerChatterSystem] Event chatter proceeding for {actor?.GunnerId}");
        // Small multiplex: combat chat is more likely during combat events
        if (evt == GunnerEvent.BossKilled
            || evt == GunnerEvent.CriticalHit
            || evt == GunnerEvent.BossAppeared
            || evt == GunnerEvent.LimitBreak)
        {
            //Debug.Log($"[GunnerChatterSystem] Event chatter using combat lines for {actor?.GunnerId}");
            bool spoke = TrySpeakFromList(actor, optionalOther, actor.CombatChatPhrases);
            if (spoke)
            {
                LearnAffinity(actor, optionalOther, +AffinityGainPerChat * 0.25f);
                // quick reply from the other gunner if present
                if (optionalOther != null && Random.value <= replyChance)
                    StartCoroutine(ReplyRoutine(optionalOther, actor, true)); // reply leans positive hype
            }
            return;
        }


        // Otherwise flip for praise vs negative
        bool positive = Random.value <= PositiveBias;
        if (positive) ForcePraise(actor, optionalOther);
        else ForceNegative(actor, optionalOther);
    }

    /// <summary>Speak a combat line now (no event gate), null-safe.</summary>
    public static void TryForceCombat(GunnerSO speaker, GunnerSO listener = null)
    {
        if (Instance == null || speaker == null) return;
        Instance.TrySpeakFromList(speaker, listener, speaker.CombatChatPhrases);
    }

    /// <summary>Trigger a GunnerEvent (null-safe); optional chanceOverride follows existing semantics.</summary>
    public static void TryTrigger(GunnerEvent evt, GunnerSO actor, GunnerSO optionalOther = null, float? chanceOverride = null)
    {
        if (Instance == null) return;
        Instance.TriggerEvent(evt, actor, optionalOther, chanceOverride);
    }


    // === Lifecycle ===

    private void Update()
    {
        // equipped chatter
        _timer += Time.unscaledDeltaTime;
        if (_timer >= PeriodicTickSeconds)
        {
            _timer = 0f;
            TryPeriodicSmallTalk();
        }

        // unequipped / idle chatter
        _inactiveIdleTimer += Time.unscaledDeltaTime;
        if (_inactiveIdleTimer >= InactiveIdleTickSeconds)
        {
            _inactiveIdleTimer = 0f;
            TryIdleForInactiveGunners();
        }
    }


    // === Internals ===

    private void TryPeriodicSmallTalk()
    {
        if (ActiveGunners == null || ActiveGunners.Count < 2) return;
        if (Random.value > PeriodicChatChance) return;

        // pick 2 weighted by area/mood/affinity
        var a = ActiveGunners[Random.Range(0, ActiveGunners.Count)];
        var b = PickPartner(a);
        if (b == null) return;

        bool positive = Random.value <= PositiveBias;
        var list = positive ? a.PraisePhrases : a.NegativePhrases;

        // Speaker A talks…
        bool spoke = TrySpeakFromList(a, b, list)
                 || TrySpeakFromList(a, b, a.CombatChatPhrases)
                 || TrySpeakFromList(a, b, a.IdlePhrases);

        if (spoke)
        {
            // Nudge affinity
            LearnAffinity(a, b, positive ? +AffinityGainPerChat : +AffinityGainPerChat * 0.1f);

            // …then B replies quickly (configurable chance)
            if (b != null && Random.value <= replyChance)
                StartCoroutine(ReplyRoutine(b, a, positive));
        }

        LearnAffinity(a, b, positive ? +AffinityGainPerChat : +AffinityGainPerChat * 0.1f);
    }

    private void TryIdleForInactiveGunners()
    {
        if (Random.value > InactiveIdleChance) return;
        if (GunnerManager.Instance == null) return;

        var pool = GunnerManager.Instance.GetAllIdleGunners(); // not equipped & not on quest
        if (pool == null || pool.Count == 0) return;

        var speaker = pool[Random.Range(0, pool.Count)];
        TrySpeakFromList(speaker, null, speaker.IdlePhrases);
    }


    private GunnerSO PickPartner(GunnerSO a)
    {
        GunnerSO best = null;
        float bestScore = float.NegativeInfinity;

        for (int i = 0; i < ActiveGunners.Count; i++)
        {
            var b = ActiveGunners[i];
            if (b == null || b == a) continue;

            float score = 1f;

            // area bias
            if (a.Area == b.Area) score *= SameAreaBonus;
            else score *= CrossAreaPenalty;

            // mood bias
            if (a.Mood == b.Mood) score *= SameMoodBonus;

            // learned affinity
            float aff = _affinity.Get(a.GunnerId, b.GunnerId);
            score += aff; // range [-1..+1] default 0

            if (score > bestScore)
            {
                bestScore = score;
                best = b;
            }
        }
        return best;
    }
    private bool TrySpeakFromList(GunnerSO speaker, GunnerSO listener, List<string> pool)
    {
        if (speaker == null || pool == null || pool.Count == 0) return false;

        string raw = pool[Random.Range(0, pool.Count)];
        string line = FormatLine(raw, speaker, listener);

        speechUIBehaviour.ShowLine(speaker, listener, line);
        return true;
    }

    private string FormatLine(string raw, GunnerSO speaker, GunnerSO listener)
    {
        if (string.IsNullOrEmpty(raw)) return string.Empty;
        if (listener == null || string.IsNullOrEmpty(nameToken)) return raw;

        // Replace token with colorized listener name (TMP supports <color=#RRGGBB>)
        if (raw.Contains(nameToken))
        {
            string hex = ColorUtility.ToHtmlStringRGB(nameColor);
            string listenerFirstName = listener.DisplayName.Split(' ')[0];
            string colored = $"<color=#{hex}>{listenerFirstName}</color>";
            return raw.Replace(nameToken, colored);
        }
        return raw;
    }

    private void LearnAffinity(GunnerSO a, GunnerSO b, float delta)
    {
        if (a == null || b == null) return;

        // gentle nudge: shared area starts a little higher
        float baseBias = (a.Area == b.Area) ? BaseAreaAffinity : 0f;
        float cur = _affinity.Get(a.GunnerId, b.GunnerId);
        float next = Mathf.Clamp(cur + delta + baseBias * 0.01f, AffinityClamp.x, AffinityClamp.y);

        _affinity.Set(a.GunnerId, b.GunnerId, next);
        _affinity.Set(b.GunnerId, a.GunnerId, next); // symmetric
    }

    private System.Collections.IEnumerator ReplyRoutine(GunnerSO replier, GunnerSO originalSpeaker, bool replyPositive)
    {
        yield return new WaitForSecondsRealtime(replyDelaySeconds);

        // Try matching tone; then combat; then idle.
        List<string> pool = replyPositive ? replier.PraisePhrases : replier.NegativePhrases;

        if (!TrySpeakFromList(replier, originalSpeaker, pool))
            if (!TrySpeakFromList(replier, originalSpeaker, replier.CombatChatPhrases))
                TrySpeakFromList(replier, originalSpeaker, replier.IdlePhrases);

        // small mutual affinity nudge for successful exchanges
        LearnAffinity(replier, originalSpeaker, +AffinityGainPerChat * 0.25f);
    }
}

public enum GunnerEvent
{
    BossKilled,
    CriticalHit,
    BossAppeared,
    WaveStart,
    WaveEnd,
    LimitBreak,
    NearlyDead
}

public class GunnerAffinityStore
{
    private readonly Dictionary<(string, string), float> _map = new Dictionary<(string, string), float>();

    public float Get(string a, string b)
    {
        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return 0f;
        return _map.TryGetValue((a, b), out var v) ? v : 0f;
    }

    public void Set(string a, string b, float value)
    {
        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return;
        _map[(a, b)] = value;
    }

    public void ClearAll() => _map.Clear();
}
