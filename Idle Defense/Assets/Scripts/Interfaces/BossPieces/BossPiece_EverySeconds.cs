using Assets.Scripts.WaveSystem;
using UnityEngine;
using static Assets.Scripts.WaveSystem.EnemyManager;

public sealed class BossPiece_EverySeconds : MonoBehaviour, IBossPiece
{
    [Header("Trigger")]
    [SerializeField] private float interval = 6f;

    [Header("Skill")]
    [SerializeField] private BossSkillId skill = BossSkillId.SummonWave;

    [Tooltip("SpecialGunnerHit: multiplier of normal attack damage. 1.5 = 150%. HealPulse: heal pct of max HP. 0.10 = 10%. BuffSelf: armor pct of base armor. 0.25 = +25%.")]
    [SerializeField] private float paramA = 1f;

    [Tooltip("SpecialGunnerHit: radius. HealPulse: radius. BuffSelf: speed multiplier.")]
    [SerializeField] private float paramB = 4f;

    [Tooltip("SpecialGunnerHit: max targets. HealPulse: max targets. ShieldGain: charges. BuffSelf: duration seconds.")]
    [SerializeField] private int paramI = 2;

    [Header("Boss Brain")]
    [SerializeField] private OutOfRangeBehavior outOfRangeBehavior = OutOfRangeBehavior.ExecuteAnyway;
    public OutOfRangeBehavior OutOfRangeBehavior => outOfRangeBehavior;

    [SerializeField] private int priority = 0;
    public int Priority => priority;

    [SerializeField] private string animationTrigger = "Skill";
    public string AnimationTrigger => animationTrigger;

    private float _nextTime;

    private void OnEnable()
    {
        _nextTime = 0f;
    }

    public bool WantsToExecute(BossContext ctx)
    {
        return ctx.TimeAlive >= _nextTime;
    }

    public void Execute(BossContext ctx)
    {
        _nextTime = ctx.TimeAlive + Mathf.Max(0.1f, interval);

        ctx.Manager.SetPendingBossSkill(
            ctx.Boss,
            skill,
            a: paramA,
            b: paramB,
            i: paramI
        );
    }
}