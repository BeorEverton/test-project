using UnityEngine;

[CreateAssetMenu(menuName = "LimitBreak/Treefall Slam")]
public class TreefallSlamSkillSO : LimitBreakSkillSO
{
    [Header("Treefall Slam")]
    public float Radius = 3.5f;
    public float Damage = 250f;

    public override void Activate(LimitBreakContext ctx)
    {
        LimitBreakManager.Instance?.ActivateTreefallSlam(Radius, Damage);
    }
}
