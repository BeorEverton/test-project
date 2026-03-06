using Assets.Scripts.WaveSystem;
using UnityEngine;
using static Assets.Scripts.WaveSystem.EnemyManager;

public sealed class BossPiece_AlternateSkills : MonoBehaviour, IBossPiece
{
    [SerializeField] private int attacksPerPhase = 4;
    [SerializeField] private BossSkillId skillA = BossSkillId.HealPulse;
    [SerializeField] private BossSkillId skillB = BossSkillId.ShieldGain;

    [Tooltip("SpecialGunnerHit: multiplier of normal attack damage. 1.5 = 150%. HealPulse: heal pct of max HP. 0.10 = 10%. JumpDepth: delta Z. BuffSelf: armor pct of base armor. 0.25 = +25%.")]
    [SerializeField] private float paramA = 0.10f;

    [Tooltip("SpecialGunnerHit: radius. HealPulse: radius. BuffSelf: speed multiplier.")]
    [SerializeField] private float paramB = 4f;

    [Tooltip("SpecialGunnerHit: max targets. HealPulse: max targets. ShieldGain: charges. BuffSelf: duration seconds.")]
    [SerializeField] private int paramI = 2;

    [SerializeField] private OutOfRangeBehavior outOfRangeBehavior = OutOfRangeBehavior.ExecuteAnyway;
    public OutOfRangeBehavior OutOfRangeBehavior => outOfRangeBehavior;

    [SerializeField] private int priority = 0;
    public int Priority => priority;

    [SerializeField] private string animationTriggerA = "Skill_PhaseA";
    [SerializeField] private string animationTriggerB = "Skill_PhaseB";

    private int _phaseStartAttacks;
    private bool _nextUsesA = true;

    public string AnimationTrigger => _nextUsesA ? animationTriggerA : animationTriggerB;

    private void OnEnable()
    {
        _phaseStartAttacks = 0;
        _nextUsesA = true;
    }

    public bool WantsToExecute(BossContext ctx)
    {
        return (ctx.AttacksDone - _phaseStartAttacks) >= attacksPerPhase;
    }

    public void Execute(BossContext ctx)
    {
        _phaseStartAttacks = ctx.AttacksDone;

        BossSkillId selectedSkill = _nextUsesA ? skillA : skillB;

        ctx.Manager.SetPendingBossSkill(
            ctx.Boss,
            selectedSkill,
            a: paramA,
            b: paramB,
            i: paramI
        );

        _nextUsesA = !_nextUsesA;
    }
}