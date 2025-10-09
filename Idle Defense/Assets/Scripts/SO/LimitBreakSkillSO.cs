using UnityEngine;

public abstract class LimitBreakSkillSO : ScriptableObject
{
    [Header("Identity")]
    public LimitBreakType Type = LimitBreakType.None;

    [Header("UI")]
    public string DisplayName;
    public string Description;
    public Sprite Icon;

    [Header("Tuning")]
    [Tooltip("How long the Limit Break lasts (seconds).")]
    public float Duration = 8f;

    [Tooltip("Main effect magnitude (e.g., damage or fire rate multiplier).")]
    public float Magnitude = 1.5f;

    [Header("Click Bonus")]
    [Tooltip("Cap for click-driven bonus percent during LB. If <= 0, falls back to Magnitude.")]
    public float ClickMaxBonusPct = 100f;

    [Tooltip("Baseline percent instantly active at LB start. Slider still shows 0 at this baseline.")]
    public float BaselinePct = 0f;

    // Called when LB is pressed and bar is full
    public abstract void Activate(LimitBreakContext ctx);
}

// Context passed to skills so they can talk to systems
public struct LimitBreakContext
{
    public string GunnerId;
    public GunnerSO GunnerSO;
    public GunnerRuntime Runtime;
}
