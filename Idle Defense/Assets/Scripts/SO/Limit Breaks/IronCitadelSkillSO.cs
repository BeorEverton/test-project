using UnityEngine;

[CreateAssetMenu(menuName = "LimitBreak/Iron Citadel")]
public class IronCitadelSkillSO : LimitBreakSkillSO
{
    [Header("Iron Citadel")]
    [Range(0, 95)] public float DamageReductionPct = 40f; // allies take less damage

    public override void Activate(LimitBreakContext ctx)
    {
        LimitBreakManager.Instance?.ActivateIronCitadel(DamageReductionPct, Duration);
    }
}
