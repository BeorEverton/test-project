using Assets.Scripts.WaveSystem;
using UnityEngine;
using static Assets.Scripts.WaveSystem.EnemyManager;

public sealed class BossPiece_EveryHpLoss : MonoBehaviour, IBossPiece
{
    [SerializeField, Range(0.01f, 0.5f)] private float step = 0.10f; // 10%
    [SerializeField] private BossSkillId skill = BossSkillId.HealPulse;

    private float _next;

    [SerializeField] private int priority = 0;
    public int Priority => priority;

    [SerializeField] private string animationTrigger = "Skill_HpThreshold";
    public string AnimationTrigger => animationTrigger;


    private void OnEnable() { _next = 1f - step; }

    public bool WantsToExecute(BossContext ctx)
    {
        return ctx.Hp01 <= _next;
    }

    public void Execute(BossContext ctx)
    {
        _next -= step;

        // Queue skill to be executed by animation event
        ctx.Manager.SetPendingBossSkill(ctx.Boss, skill);
        
    }
}
