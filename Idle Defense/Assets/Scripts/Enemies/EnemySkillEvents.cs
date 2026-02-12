using Assets.Scripts.Enemies;
using Assets.Scripts.WaveSystem;
using UnityEngine;

public sealed class EnemySkillEvents : MonoBehaviour
{
    private Enemy _enemy;

    private void Awake()
    {
        _enemy = GetComponentInParent<Enemy>();
    }

    // Call this from an Animation Event inside the Skill clip.
    public void AnimEvent_ExecuteSkill()
    {
        if (_enemy == null) return;
        EnemyManager.Instance.ExecutePendingSkill(_enemy);
    }

    // Animation Event inside the Attack clip
    public void AnimEvent_ExecuteAttack()
    {
        if (_enemy == null) return;
        EnemyManager.Instance.ExecutePendingAttack(_enemy);
    }
}
