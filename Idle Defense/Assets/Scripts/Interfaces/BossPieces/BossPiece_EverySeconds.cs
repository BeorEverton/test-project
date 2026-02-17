using Assets.Scripts.WaveSystem;
using UnityEngine;
using static Assets.Scripts.WaveSystem.EnemyManager;

public sealed class BossPiece_EverySeconds : MonoBehaviour, IBossPiece
{
    [SerializeField] private float interval = 6f;
    [SerializeField] private BossSkillId skill = BossSkillId.SummonWave;
    [SerializeField] private OutOfRangeBehavior outOfRangeBehavior = OutOfRangeBehavior.ExecuteAnyway;
    public OutOfRangeBehavior OutOfRangeBehavior => outOfRangeBehavior;

    [SerializeField] private int priority = 0;
    public int Priority => priority;


    [SerializeField] private string animationTrigger = "Skill_Jump";
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
        ctx.Manager.SetPendingBossSkill(ctx.Boss, skill);
    }

}
