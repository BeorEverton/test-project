using Assets.Scripts.Enemies;
using Assets.Scripts.Turrets;
using System.Collections;
using UnityEngine;

public class LaserBeamPattern : MonoBehaviour, ITargetingPattern
{
    [SerializeField] private LineRenderer laserLine;

    private float timeOnSameTarget;
    private GameObject lastTarget;
    private float disableLaserTime; // Time to disable laser after target is lost
    private float lastShotTime;
    Coroutine disableLaserCoroutine;

    public void ExecuteAttack(BaseTurret turret, TurretStatsInstance stats, GameObject primaryTarget)
    {
        if (primaryTarget == null) return;

        disableLaserTime = (1 / stats.FireRate) + 0.01f;
        lastShotTime = Time.time;

        if (disableLaserCoroutine == null)
            disableLaserCoroutine = StartCoroutine(DisableLaserLine());

        // Apply all damage effects (includes ramp bonus)
        var enemy = primaryTarget.GetComponent<Enemy>();
        turret.DamageEffects.ApplyAll(enemy, stats);

        // Visual only
        laserLine.enabled = true;
        laserLine.SetPosition(0, laserLine.transform.position);
        laserLine.SetPosition(1, primaryTarget.transform.position);
    }


    IEnumerator DisableLaserLine()
    {
        while (Time.time - lastShotTime < disableLaserTime)
        {
            yield return new WaitForEndOfFrame();
        }
        laserLine.enabled = false;
        disableLaserCoroutine = null;
    }
}
