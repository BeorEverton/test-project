using Assets.Scripts.WaveSystem;
using UnityEngine;
using static Assets.Scripts.WaveSystem.EnemyManager;

public sealed class BossPiece_EveryDistance : MonoBehaviour, IBossPiece
{
    [SerializeField] private float distanceStep = 8f;
    [SerializeField] private BossSkillId skill = BossSkillId.ShieldGain;
    [SerializeField] private int shieldCharges = 1;

    private float _next;

    [SerializeField] private OutOfRangeBehavior outOfRangeBehavior = OutOfRangeBehavior.ExecuteAnyway;
    public OutOfRangeBehavior OutOfRangeBehavior => outOfRangeBehavior;

    [SerializeField] private int priority = 0;
    public int Priority => priority;


    [SerializeField] private string animationTrigger = "Skill_HpThreshold";
    public string AnimationTrigger => animationTrigger;


    private void OnEnable() { _next = distanceStep; }

    public bool WantsToExecute(BossContext ctx) => ctx.DistanceWalked >= _next;

    public void Execute(BossContext ctx)
    {
        _next += distanceStep;
        ctx.Manager.SetPendingBossSkill(ctx.Boss, skill, i: shieldCharges);
        
    }
}
