using Assets.Scripts.WaveSystem;
using UnityEngine;
using static Assets.Scripts.WaveSystem.EnemyManager;

public sealed class BossPiece_EveryHpLoss : MonoBehaviour, IBossPiece
{
    [Header("Trigger")]
    [SerializeField, Range(0.01f, 0.5f)] private float step = 0.10f; // 10%

    [Header("Skill")]
    [SerializeField] private BossSkillId skill = BossSkillId.HealPulse;

    // Generic payload 
    // SpecialGunnerHit: A=damage, B=radius, I=maxTargets
    // ShieldGain: I=charges
    // JumpDepth: A=deltaZ
    // BuffSelf: A=armorDelta, B=speedMult, I=durationSeconds
    [Tooltip("SpecialGunnerHit = A=damage. JumpDepth: A=deltaZ. BuffSelf: A=armorDelta")]
    [SerializeField] private float paramA = 20f;
    [Tooltip("SpecialGunnerHit = B=radius. BuffSelf: B=speedMult")]
    [SerializeField] private float paramB = 4f;
    [Tooltip("SpecialGunnerHit = I=maxTargets. ShieldGain: I=charges. BuffSelf: I=durationSeconds")]
    [SerializeField] private int paramI = 2;

    [Header("Boss Brain")]
    [SerializeField] private OutOfRangeBehavior outOfRangeBehavior = OutOfRangeBehavior.ExecuteAnyway;
    public OutOfRangeBehavior OutOfRangeBehavior => outOfRangeBehavior;

    [SerializeField] private int priority = 0;
    public int Priority => priority;


    [SerializeField] private string animationTrigger = "Skill_HpThreshold";
    public string AnimationTrigger => animationTrigger;

    private float _next;

    private void OnEnable()
    {
        _next = 1f - step;
    }

    public bool WantsToExecute(BossContext ctx)
    {
        return ctx.Hp01 <= _next;
    }

    public void Execute(BossContext ctx)
    {
        // Prevent multi-fire burst: advance thresholds past current HP in one go.
        float s = Mathf.Max(0.001f, step);
        while (ctx.Hp01 <= _next)
            _next -= s;

        ctx.Manager.SetPendingBossSkill(ctx.Boss, skill, a: paramA, b: paramB, i: paramI);
    }
}
