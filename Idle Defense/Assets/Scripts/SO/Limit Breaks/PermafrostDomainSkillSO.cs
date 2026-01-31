using UnityEngine;

[CreateAssetMenu(menuName = "LimitBreak/Permafrost Domain")]
public class PermafrostDomainSkillSO : LimitBreakSkillSO
{
    [Header("Permafrost Domain")]
    public float Radius = 12f;

    [Range(0f, 100f)]
    public float SlowPct = 75f; // "massively slowing"

    [Tooltip("How long the domain persists, applying slow to enemies inside.")]
    public float DomainSeconds = 5f;

    public override void Activate(LimitBreakContext ctx)
    {
        // This one is global; gunnerId isn't needed unless you want VFX at the gunner.
        LimitBreakManager.Instance?.ActivatePermafrostDomain(Radius, SlowPct, DomainSeconds);
    }
}
