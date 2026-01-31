using UnityEngine;

[CreateAssetMenu(menuName = "LimitBreak/Tidepush")]
public class TidepushSkillSO : LimitBreakSkillSO
{
    [Header("Tidepush")]
    public float Radius = 6f;
    public float KnockTime = 0.4f;
    public float KnockSpeed = 12f;

    [Header("Power")]
    [Tooltip("If > 0, Damage becomes (EffectiveGunnerDamage * pct / 100). If 0, uses FlatDamage.")]
    public float DamagePctOfGunnerDamage = 0f;

    public float FlatDamage = 90f;
    public float MinDamage = 0f;
    public float MaxDamage = 999999f;

    public override void Activate(LimitBreakContext ctx)
    {
        var lbm = LimitBreakManager.Instance;
        if (lbm == null) return;

        float dmg = lbm.ResolvePower(ctx, FlatDamage, DamagePctOfGunnerDamage, MinDamage, MaxDamage);
        lbm.ActivateTidepush(Radius, KnockTime, KnockSpeed, dmg);
    }

}
