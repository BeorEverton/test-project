using UnityEngine;

[CreateAssetMenu(menuName = "LimitBreak/Cryo Burst")]
public class CryoBurstSkillSO : LimitBreakSkillSO
{
    [Header("Cryo Burst")]
    public float Radius = 7f;
    [Range(0, 100)] public float SlowPct = 45f;
    public float SlowSeconds = 3.5f;
    public float HealPct = 5f;

    public override void Activate(LimitBreakContext ctx)
    {
        LimitBreakManager.Instance?.ActivateCryoBurst(Radius, SlowPct, SlowSeconds, HealPct);
    }
}
