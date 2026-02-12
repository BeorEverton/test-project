using UnityEngine;
using Assets.Scripts.Enemies;
using Assets.Scripts.WaveSystem;

public sealed class BossContext
{
    public Enemy Boss;
    public EnemyManager Manager;

    public float Hp01;
    public float TimeAlive;
    public float DistanceWalked;
    public int AttacksDone;

    public float LastSkillTime;
}
