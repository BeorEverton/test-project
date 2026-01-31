using UnityEngine;

[CreateAssetMenu(menuName = "LimitBreak/Boil Jet")]
public class BoilJetSkillSO : LimitBreakSkillSO
{
    [Header("Boil Jet")]
    public float BeamWidth = 1.25f; // meters

    [Header("Power")]
    [Tooltip("If > 0, DPS becomes (EffectiveGunnerDamage * pct / 100). If 0, uses FlatDPS.")]
    public float DpsPctOfGunnerDamage = 0f;

    [Tooltip("Used when DpsPctOfGunnerDamage is 0.")]
    public float FlatDPS = 50f;

    [Tooltip("Optional clamps after scaling.")]
    public float MinDPS = 0f;
    public float MaxDPS = 999999f;

    public override void Activate(LimitBreakContext ctx)
    {
        var lbm = LimitBreakManager.Instance;
        if (lbm == null) return;

        float dps = lbm.ResolvePower(ctx, FlatDPS, DpsPctOfGunnerDamage, MinDPS, MaxDPS);

        // pass 'this' so the manager can start a utility timer bar (icon/name/duration)
        lbm.ActivateBoilJet(ctx.GunnerId, dps, BeamWidth, Duration, this);
    }
}
