using Assets.Scripts.Enemies;
using Assets.Scripts.WaveSystem;
using UnityEngine;

public sealed class EnemySkillEvents : MonoBehaviour
{
    private Enemy _enemy;
    private BossBrain _bossBrain;

    private void Awake()
    {
        _enemy = GetComponentInParent<Enemy>();
        _bossBrain = GetComponentInParent<BossBrain>();
    }

    public void AnimEvent_SkillStart()
    {
        if (_bossBrain != null) _bossBrain.OnSkillAnimStart();
    }

    public void AnimEvent_ExecuteSkill()
    {
        if (_bossBrain != null) _bossBrain.OnSkillAnimExecute();
        EnemyManager.Instance.ExecutePendingSkill(_enemy);
    }

    public void AnimEvent_SkillEnd()
    {
        if (_bossBrain != null) _bossBrain.OnSkillAnimEnd();
    }


    // Animation Event inside the Attack clip
    public void AnimEvent_ExecuteAttack()
    {
        if (_enemy == null) return;
        EnemyManager.Instance.ExecutePendingAttack(_enemy);
    }
}
