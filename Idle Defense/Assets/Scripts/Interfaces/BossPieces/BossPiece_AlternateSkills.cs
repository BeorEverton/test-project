using Assets.Scripts.WaveSystem;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using static Assets.Scripts.WaveSystem.EnemyManager;

public sealed class BossPiece_AlternateSkills : MonoBehaviour, IBossPiece
{
    [SerializeField] private int attacksPerPhase = 4;
    [SerializeField] private BossSkillId skillA = BossSkillId.HealPulse;
    [SerializeField] private BossSkillId skillB = BossSkillId.ShieldGain;

    private int _phaseStartAttacks;
    private bool _useA = true;

    // Generic payload 
    // SpecialGunnerHit: A=damage, B=radius, I=maxTargets
    // ShieldGain: I=charges
    // JumpDepth: A=deltaZ
    // BuffSelf: A=armorDelta, B=speedMult, I=durationSeconds
    [SerializeField] private float paramA = 20f;
    [SerializeField] private float paramB = 4f;
    [SerializeField] private int paramI = 2;

    [SerializeField] private OutOfRangeBehavior outOfRangeBehavior = OutOfRangeBehavior.ExecuteAnyway;
    public OutOfRangeBehavior OutOfRangeBehavior => outOfRangeBehavior;

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
        ctx.Manager.SetPendingBossSkill(ctx.Boss, s, a: paramA, b: paramB, i: paramI);

    }
}
