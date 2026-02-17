using Assets.Scripts.Enemies;
using Assets.Scripts.WaveSystem;
using UnityEngine;
using static Assets.Scripts.WaveSystem.EnemyManager;

public sealed class BossPiece_DeathExplosionMode : MonoBehaviour, IBossPiece
{
    [SerializeField] private Enemy.BossDeathExplosionMode mode = Enemy.BossDeathExplosionMode.Both;

    [SerializeField] private OutOfRangeBehavior outOfRangeBehavior = OutOfRangeBehavior.ExecuteAnyway;
    public OutOfRangeBehavior OutOfRangeBehavior => outOfRangeBehavior;

    [SerializeField] private int priority = 999;
    public int Priority => priority;


    [SerializeField] private string animationTrigger = ""; // no animation needed
    public string AnimationTrigger => animationTrigger;

    public bool WantsToExecute(BossContext ctx) => ctx.TimeAlive < 0.1f; // start once

    public void Execute(BossContext ctx)
    {
        ctx.Manager.SetPendingBossSkill(ctx.Boss, BossSkillId.DeathExplosionMode, i: (int)mode);
        
        enabled = false;
    }
}
