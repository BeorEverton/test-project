using UnityEngine;
using static Assets.Scripts.WaveSystem.EnemyManager;

public sealed class BossPiece_ChanceEveryNAttacks : MonoBehaviour, IBossPiece
{
    [SerializeField] private OutOfRangeBehavior outOfRangeBehavior = OutOfRangeBehavior.ExecuteAnyway;
    public OutOfRangeBehavior OutOfRangeBehavior => outOfRangeBehavior;

    [SerializeField] private int priority = 0;
    public int Priority => priority;

    [SerializeField] private string animationTrigger = "Skill";
    public string AnimationTrigger => animationTrigger;

    [SerializeField] private int everyN = 3;
    [SerializeField, Range(0f, 1f)] private float chance = 0.25f;

    [Header("Skill")]
    [SerializeField] private BossSkillId skill = BossSkillId.SpecialGunnerHit;

    [Tooltip("SpecialGunnerHit: multiplier of normal attack damage. 1.5 = 150%. HealPulse: heal pct of max HP. 0.10 = 10%. JumpDepth: delta Z. BuffSelf: armor pct of base armor. 0.25 = +25%.")]
    [SerializeField] private float paramA = 1.5f;

    [Tooltip("SpecialGunnerHit: radius. HealPulse: radius. BuffSelf: speed multiplier.")]
    [SerializeField] private float paramB = 4f;

    [Tooltip("SpecialGunnerHit: max targets. HealPulse: max targets. ShieldGain: charges. BuffSelf: duration seconds.")]
    [SerializeField] private int paramI = 2;

    private int _nextAttack;
    private int _lastAttackSeen = -1;
    private bool _shouldFire;

    private void OnEnable()
    {
        _nextAttack = Mathf.Max(1, everyN);
        _lastAttackSeen = -1;
        _shouldFire = false;
    }

    public bool WantsToExecute(BossContext ctx)
    {
        if (ctx.AttacksDone == _lastAttackSeen)
            return _shouldFire;

        _lastAttackSeen = ctx.AttacksDone;
        _shouldFire = false;

        if (ctx.AttacksDone < _nextAttack)
            return false;

        _nextAttack += Mathf.Max(1, everyN);

        if (Random.value <= chance)
            _shouldFire = true;

        return _shouldFire;
    }

    public void Execute(BossContext ctx)
    {
        _shouldFire = false;

        ctx.Manager.SetPendingBossSkill(
            ctx.Boss,
            skill,
            a: paramA,
            b: paramB,
            i: paramI
        );
    }
}