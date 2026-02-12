using Assets.Scripts.WaveSystem;
using UnityEngine;
using static Assets.Scripts.WaveSystem.EnemyManager;

public sealed class BossPiece_EverySeconds : MonoBehaviour, IBossPiece
{
    [SerializeField] private float interval = 6f;
    [SerializeField] private BossSkillId skill = BossSkillId.SummonWave;

    [SerializeField] private int priority = 0;
    public int Priority => priority;

    [SerializeField] private string animationTrigger = "Skill_Jump";
    public string AnimationTrigger => animationTrigger;


    public bool WantsToExecute(BossContext ctx)
    {
        return (ctx.TimeAlive - ctx.LastSkillTime) >= Mathf.Max(0.1f, interval);
    }

    public void Execute(BossContext ctx)
    {
        ctx.Manager.SetPendingBossSkill(ctx.Boss, skill);        
    }
}
