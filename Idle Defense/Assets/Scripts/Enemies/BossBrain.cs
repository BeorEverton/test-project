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
    private IBossPiece _activePiece;

    private IBossPiece _pendingPiece;
    private float _pendingSetTimeAlive = -999f; // optional: prevents spamming last skill time logic

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

        // Pick first matching piece (order = priority). 
        bool inRange = IsBossInAttackRange();

        // 1) If we were waiting on something and now we’re in range, fire it first.
        if (_pendingPiece != null && inRange)
        {
            TryExecutePiece(_pendingPiece);
            _pendingPiece = null;
            return;
        }

        // 2) Otherwise scan pieces in priority order (assuming your list is already sorted)
        for (int i = 0; i < _pieces.Count; i++)
        {
            IBossPiece piece = _pieces[i];
            if (!piece.WantsToExecute(_ctx))
                continue;

            if (!inRange)
            {
                switch (piece.OutOfRangeBehavior)
                {
                    case OutOfRangeBehavior.ExecuteAnyway:
                        // allowed to fire out of range
                        break;

                    case OutOfRangeBehavior.WaitUntilInRange:
                        // store one pending piece (highest priority wins because list is sorted)
                        _pendingPiece = piece;
                        // do NOT execute anything else this frame, because “wait” means this action is the choice
                        return;

                    case OutOfRangeBehavior.SkipThisAttempt:
                        // treat as "not ready" right now; keep scanning for other actions
                        continue;
                }
            }

            // In range OR ExecuteAnyway
            TryExecutePiece(piece);
            return;
        }

    }

    private bool IsBossInAttackRange()
    {
        if (_boss == null) return false;

        float depthNow = transform.position.z;
        return _boss.CanAttack && depthNow <= _boss.attackRange;
    }

    private void TryExecutePiece(IBossPiece piece)
    {
        _activePiece = piece;

        piece.Execute(_ctx);
        _ctx.LastSkillTime = _timeAlive;

        string trig = piece.AnimationTrigger;

        if (string.IsNullOrEmpty(trig))
        {
            OnSkillAnimExecute();
            EnemyManager.Instance.ExecutePendingSkill(_boss);
            OnSkillAnimEnd();
            return;
        }

        OnSkillAnimStart();

        if (animator != null)
        {
            animator.ResetTrigger(trig);
            animator.SetTrigger(trig);
        }
    }


    // Call this from your EnemyManager when boss attack executes (or via animation event)
    public void NotifyAttackExecuted()
    {
        _ctx.AttacksDone++;
        Debug.Log($"Boss executed attack #{_ctx.AttacksDone} at time {_ctx.TimeAlive:F1}s, hp%={_ctx.Hp01:P1}");
    }

    #region VFX Helpers   

    private void SpawnPieceVfx(GameObject prefab)
    {
        if (prefab == null || _activePiece == null)
            return;

        Transform anchor = _activePiece.VfxAnchor != null ?
                           _activePiece.VfxAnchor :
                           transform;

        Instantiate(prefab, anchor.position + _activePiece.VfxOffset, Quaternion.identity);
    }

    public void OnSkillAnimStart()
    {
        SpawnPieceVfx(_activePiece.VfxOnStart);
    }

    public void OnSkillAnimExecute()
    {
        SpawnPieceVfx(_activePiece.VfxOnExecute);
        EnemyManager.Instance.ExecutePendingSkill(_boss);
    }

    public void OnSkillAnimEnd()
    {
        SpawnPieceVfx(_activePiece.VfxOnEnd);
    }


    #endregion
}
