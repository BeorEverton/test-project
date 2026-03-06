using Assets.Scripts.WaveSystem;
using UnityEngine;
using static Assets.Scripts.WaveSystem.EnemyManager;

public sealed class BossPiece_EveryHpLoss : MonoBehaviour, IBossPiece
{
    [Header("Trigger")]
    [SerializeField, Range(0.01f, 0.5f)] private float step = 0.10f;

    [Header("Skill")]
    [SerializeField] private BossSkillId skill = BossSkillId.HealPulse;

    [Tooltip("SpecialGunnerHit: multiplier of normal attack damage. 1.5 = 150%. HealPulse: heal pct of max HP. 0.10 = 10%. JumpDepth: delta Z. BuffSelf: armor pct of base armor. 0.25 = +25%.")]
    [SerializeField] private float paramA = 0.10f;

    [Tooltip("SpecialGunnerHit: radius. HealPulse: radius. BuffSelf: speed multiplier.")]
    [SerializeField] private float paramB = 4f;

    [Tooltip("SpecialGunnerHit: max targets. HealPulse: max targets. ShieldGain: charges. BuffSelf: duration seconds.")]
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
        float s = Mathf.Max(0.001f, step);
        while (ctx.Hp01 <= _next)
            _next -= s;

        ctx.Manager.SetPendingBossSkill(ctx.Boss, skill, a: paramA, b: paramB, i: paramI);
    }
}