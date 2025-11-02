using UnityEngine;

[CreateAssetMenu(menuName = "LimitBreak/Boil Jet")]
public class BoilJetSkillSO : LimitBreakSkillSO
{
    [Header("Boil Jet")]
    public float BeamWidth = 1.25f; // meters
    public float DPS = 50f;

    public override void Activate(LimitBreakContext ctx)
    {
        // pass 'this' so the manager can start a utility timer bar (icon/name/duration)
        LimitBreakManager.Instance?.ActivateBoilJet(ctx.GunnerId, DPS, BeamWidth, Duration, this);
    }

}
