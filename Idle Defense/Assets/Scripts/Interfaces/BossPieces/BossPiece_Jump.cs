using Assets.Scripts.WaveSystem;
using UnityEngine;

public sealed class BossPiece_Jump : MonoBehaviour, IBossPiece
{
    [SerializeField] private int priority = 0;
    public int Priority => priority;

    [SerializeField] private string animationTrigger = "Skill_Jump";
    public string AnimationTrigger => animationTrigger;

    [SerializeField] private float everySeconds = 10f;
    [SerializeField] private float jumpDepth = -2.0f;

    public bool WantsToExecute(BossContext ctx)
    {
        return (ctx.TimeAlive - ctx.LastSkillTime) >= everySeconds;
    }

    public void Execute(BossContext ctx)
    {
        // This requires EnemyManager to support BossSkillId + SetPendingBossSkill.
        ctx.Manager.SetPendingBossSkill(ctx.Boss, EnemyManager.BossSkillId.JumpDepth, a: jumpDepth);
    }
}
