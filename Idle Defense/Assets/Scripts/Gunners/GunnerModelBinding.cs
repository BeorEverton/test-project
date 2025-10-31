using System;
using System.Collections;
using UnityEngine;

public class GunnerModelBinding : MonoBehaviour
{
    [Header("Anim")]
    public Animator Animator;        // Bool: "Run", Trigger: "Attack"
    public string RunBool = "Run";
    public string AttackTrigger = "Attack";

    [Header("FX")]
    public GameObject LimitBreakReadyFx; // optional; toggled when LB is full

    [Header("Motion")]
    public float RunSpeed = 6f;

    private bool _runningOut;
    private Action _onArrive;
    private Action _onExit;

    public void Initialize(float runSpeed, GameObject lbFxPrefab)
    {
        RunSpeed = runSpeed;
        if (lbFxPrefab != null && LimitBreakReadyFx == null)
            LimitBreakReadyFx = Instantiate(lbFxPrefab, transform);
        SetLimitBreakReady(false);
    }

    public void RunTo(Transform target, float snapDist, Action onArrive)
    {
        _onArrive = onArrive;
        _runningOut = false;
        if (Animator) Animator.SetBool(RunBool, true);
        StopAllCoroutines();
        StartCoroutine(Co_RunTo(target, snapDist));
    }

    public void RunOut(Vector3 worldExit, float snapDist, Action onExit)
    {
        _onExit = onExit;
        _runningOut = true;
        if (Animator) Animator.SetBool(RunBool, true);
        StopAllCoroutines();
        StartCoroutine(Co_RunOut(worldExit, snapDist));
    }

    public void PlayAttack()
    {
        if (Animator) Animator.SetTrigger(AttackTrigger);
    }

    public void SetLimitBreakReady(bool ready)
    {
        if (LimitBreakReadyFx) LimitBreakReadyFx.SetActive(ready);
    }

    private IEnumerator Co_RunTo(Transform target, float snap)
    {
        while (target && (transform.position - target.position).sqrMagnitude > snap * snap)
        {
            Vector3 dir = (target.position - transform.position);
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.0001f)
            {
                transform.position += dir.normalized * RunSpeed * Time.deltaTime;
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 12f * Time.deltaTime);
            }
            yield return null;
        }
        if (Animator) Animator.SetBool(RunBool, false);
        _onArrive?.Invoke();
    }

    private IEnumerator Co_RunOut(Vector3 exit, float snap)
    {
        while ((transform.position - exit).sqrMagnitude > snap * snap)
        {
            Vector3 dir = (exit - transform.position);
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.0001f)
            {
                transform.position += dir.normalized * RunSpeed * Time.deltaTime;
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 12f * Time.deltaTime);
            }
            yield return null;
        }
        if (Animator) Animator.SetBool(RunBool, false);
        _onExit?.Invoke();
        Destroy(gameObject);
    }
}
