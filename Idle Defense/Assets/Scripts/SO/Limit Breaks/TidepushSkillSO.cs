using UnityEngine;

[CreateAssetMenu(menuName = "LimitBreak/Tidepush")]
public class TidepushSkillSO : LimitBreakSkillSO
{
    [Header("Tidepush")]
    public float Radius = 6f;
    public float KnockTime = 0.4f;
    public float KnockSpeed = 12f;
    public float Damage = 90f;

    public override void Activate(LimitBreakContext ctx)
    {
        LimitBreakManager.Instance?.ActivateTidepush(Radius, KnockTime, KnockSpeed, Damage);
    }
}
