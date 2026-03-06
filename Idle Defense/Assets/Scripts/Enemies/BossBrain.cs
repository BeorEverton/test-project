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
    private IBossPiece _waitingForRangePiece;

    private Vector3 _lastPos;
    private float _timeAlive;

    private bool _isExecutingSkill;
    private bool _skillPayloadExecuted;

    public bool BlocksBasicAttack => _isExecutingSkill || _waitingForRangePiece != null;

    private void Awake()
    {
        _boss = GetComponent<Enemy>();
        _ctx = new BossContext { Boss = _boss, Manager = EnemyManager.Instance };

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

        _activePiece = null;
        _waitingForRangePiece = null;
        _isExecutingSkill = false;
        _skillPayloadExecuted = false;
    }

    private void Update()
    {
        if (_boss == null || !_boss.IsAlive)
            return;

        float dt = Time.deltaTime;
        _timeAlive += dt;

        Vector3 p = transform.position;
        _ctx.DistanceWalked += Vector3.Distance(p, _lastPos);
        _lastPos = p;

        _ctx.TimeAlive = _timeAlive;
        _ctx.Hp01 = (_boss.MaxHealth <= 0f) ? 0f : (_boss.CurrentHealth / _boss.MaxHealth);

        if (_isExecutingSkill)
            return;

        bool inRange = IsBossInAttackRange();

        if (_waitingForRangePiece != null)
        {
            if (inRange)
            {
                IBossPiece pieceToRun = _waitingForRangePiece;
                _waitingForRangePiece = null;
                TryExecutePiece(pieceToRun);
            }

            return;
        }

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
                        break;

                    case OutOfRangeBehavior.WaitUntilInRange:
                        _waitingForRangePiece = piece;
                        return;

                    case OutOfRangeBehavior.SkipThisAttempt:
                        continue;
                }
            }

            TryExecutePiece(piece);
            return;
        }
    }

    private bool IsBossInAttackRange()
    {
        if (_boss == null)
            return false;

        float depthNow = transform.position.z;
        return _boss.CanAttack && depthNow <= _boss.attackRange;
    }

    private void TryExecutePiece(IBossPiece piece)
    {
        if (piece == null)
            return;

        _activePiece = piece;
        _isExecutingSkill = true;
        _skillPayloadExecuted = false;

        EnemyManager.Instance.ClearPendingAttack(_boss);
        _boss.TimeSinceLastAttack = 0f;
        _boss.ResetAttackTriggerOnly();

        piece.Execute(_ctx);
        _ctx.LastSkillTime = _timeAlive;

        string trig = piece.AnimationTrigger;

        if (string.IsNullOrEmpty(trig))
        {
            OnSkillAnimStart();
            OnSkillAnimExecute();
            OnSkillAnimEnd();
            return;
        }

        OnSkillAnimStart();

        if (animator != null)
        {
            animator.ResetTrigger(trig);
            animator.SetTrigger(trig);
        }
        else
        {
            OnSkillAnimExecute();
            OnSkillAnimEnd();
        }
    }

    public void NotifyAttackExecuted()
    {
        _ctx.AttacksDone++;
    }

    #region VFX Helpers

    private void SpawnPieceVfx(GameObject prefab)
    {
        if (prefab == null || _activePiece == null)
            return;

        Transform anchor = _activePiece.VfxAnchor != null
            ? _activePiece.VfxAnchor
            : transform;

        Instantiate(prefab, anchor.position + _activePiece.VfxOffset, Quaternion.identity);
    }

    public void OnSkillAnimStart()
    {
        SpawnPieceVfx(_activePiece != null ? _activePiece.VfxOnStart : null);
    }

    public void OnSkillAnimExecute()
    {
        if (!_isExecutingSkill || _skillPayloadExecuted)
            return;

        SpawnPieceVfx(_activePiece != null ? _activePiece.VfxOnExecute : null);
        EnemyManager.Instance.ExecutePendingSkill(_boss);
        _skillPayloadExecuted = true;
    }

    public void OnSkillAnimEnd()
    {
        Debug.Log("Is executing? " + _isExecutingSkill);
        if (!_isExecutingSkill)
            return;

        if (!_skillPayloadExecuted)
            OnSkillAnimExecute();

        SpawnPieceVfx(_activePiece != null ? _activePiece.VfxOnEnd : null);

        _activePiece = null;
        _isExecutingSkill = false;
        _skillPayloadExecuted = false;
    }

    #endregion
}