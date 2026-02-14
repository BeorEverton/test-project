using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.Enemies;
using Assets.Scripts.WaveSystem;

[DisallowMultipleComponent]
public sealed class BossBrain : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private string animTriggerSkill = "Skill";

    private Enemy _boss;
    private BossContext _ctx;

    private readonly List<IBossPiece> _pieces = new List<IBossPiece>(16);

    private Vector3 _lastPos;
    private float _timeAlive;

    private void Awake()
    {
        _boss = GetComponent<Enemy>();
        _ctx = new BossContext { Boss = _boss, Manager = EnemyManager.Instance };

        // Collect pieces attached to the boss
        GetComponents(_pieces);
        _pieces.Sort((a, b) => b.Priority.CompareTo(a.Priority));

        _lastPos = transform.position;
    }

    private void OnEnable()
    {
        _timeAlive = 0f;
        _ctx.TimeAlive = 0f;
        _ctx.DistanceWalked = 0f;
        _ctx.AttacksDone = 0;
        _ctx.LastSkillTime = -999f;
    }

    private void Update()
    {
        if (_boss == null || !_boss.IsAlive) return;

        float dt = Time.deltaTime;
        _timeAlive += dt;

        Vector3 p = transform.position;
        _ctx.DistanceWalked += Vector3.Distance(p, _lastPos);
        _lastPos = p;

        _ctx.TimeAlive = _timeAlive;
        _ctx.Hp01 = (_boss.MaxHealth <= 0f) ? 0f : (_boss.CurrentHealth / _boss.MaxHealth);

        // Pick first matching piece (order = priority). No allocations.
        for (int i = 0; i < _pieces.Count; i++)
        {
            if (_pieces[i].WantsToExecute(_ctx))
            {
                _pieces[i].Execute(_ctx);
                _ctx.LastSkillTime = _timeAlive;

                // Play skill anim (animation event will execute)
                if (animator != null && !string.IsNullOrEmpty(animTriggerSkill))
                {
                    string trig = _pieces[i].AnimationTrigger;
                    if (animator != null && !string.IsNullOrEmpty(trig))
                        animator.SetTrigger(trig);
                }

                return;
            }
        }
    }

    // Call this from your EnemyManager when boss attack executes (or via animation event)
    public void NotifyAttackExecuted()
    {
        _ctx.AttacksDone++;
        Debug.Log($"Boss executed attack #{_ctx.AttacksDone} at time {_ctx.TimeAlive:F1}s, hp%={_ctx.Hp01:P1}");
    }
}
