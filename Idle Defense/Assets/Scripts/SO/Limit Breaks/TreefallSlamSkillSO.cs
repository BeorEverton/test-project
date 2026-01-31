using UnityEngine;

[CreateAssetMenu(menuName = "LimitBreak/Treefall Slam")]
public class TreefallSlamSkillSO : LimitBreakSkillSO
{
    [Header("Treefall Slam")]
    public float Radius = 3.5f;

    [Header("Power")]
    [Tooltip("If > 0, Damage becomes (EffectiveGunnerDamage * pct / 100). If 0, uses FlatDamage.")]
    public float DamagePctOfGunnerDamage = 0f;

    public float FlatDamage = 250f;
    public float MinDamage = 0f;
    public float MaxDamage = 999999f;

    public override void Activate(LimitBreakContext ctx)
    {
        var lbm = LimitBreakManager.Instance;
        if (lbm == null) return;

        float dmg = lbm.ResolvePower(ctx, FlatDamage, DamagePctOfGunnerDamage, MinDamage, MaxDamage);
        lbm.ActivateTreefallSlam(Radius, dmg);
    }

}
