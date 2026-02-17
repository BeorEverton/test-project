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

    // Skill payload example
    [SerializeField] private BossSkillId skill = BossSkillId.SpecialGunnerHit;
    [SerializeField] private float damage = 20f;
    [SerializeField] private float radius = 4f;
    [SerializeField] private int maxTargets = 2;

    private int _nextAttack;
    private int _lastAttackSeen = -1;
    private bool _shouldFire;

    Animator _animator;

    private void OnEnable()
    {
        _nextAttack = Mathf.Max(1, everyN);
        _lastAttackSeen = -1;
        _shouldFire = false;
    }

    public bool WantsToExecute(BossContext ctx)
    {
        // Only evaluate when we see a NEW attack count
        if (ctx.AttacksDone == _lastAttackSeen)
            return _shouldFire; // if we already decided yes, keep it until executed

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

        Debug.Log($"BossPiece_ChanceEveryNAttacks triggered on attack {_lastAttackSeen} (next check at {_nextAttack}).");

        ctx.Manager.SetPendingBossSkill(
            ctx.Boss,
            skill,
            a: damage,
            b: radius,
            i: maxTargets
        );
    }
}
