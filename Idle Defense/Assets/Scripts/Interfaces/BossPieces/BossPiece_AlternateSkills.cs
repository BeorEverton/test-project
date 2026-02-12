using Assets.Scripts.WaveSystem;
using UnityEngine;
using static Assets.Scripts.WaveSystem.EnemyManager;

public sealed class BossPiece_AlternateSkills : MonoBehaviour, IBossPiece
{
    [SerializeField] private int attacksPerPhase = 4;
    [SerializeField] private BossSkillId skillA = BossSkillId.HealPulse;
    [SerializeField] private BossSkillId skillB = BossSkillId.ShieldGain;

    private int _phaseStartAttacks;
    private bool _useA = true;

    [SerializeField] private int priority = 0;
    public int Priority => priority;

    [SerializeField] private string animationTriggerA = "Skill_PhaseA";
    [SerializeField] private string animationTriggerB = "Skill_PhaseB";
        
    public string AnimationTrigger => _useA ? animationTriggerA : animationTriggerB;


    public bool WantsToExecute(BossContext ctx)
    {
        return (ctx.AttacksDone - _phaseStartAttacks) >= attacksPerPhase;
    }

    public void Execute(BossContext ctx)
    {
        _phaseStartAttacks = ctx.AttacksDone;
        _useA = !_useA;

        var s = _useA ? skillA : skillB;
        ctx.Manager.SetPendingBossSkill(ctx.Boss, s, i: 2);
        
    }
}
