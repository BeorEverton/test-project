using Assets.Scripts.Enemies;
using Assets.Scripts.Systems;
using Assets.Scripts.Turrets;
using UnityEngine;

public class SingleTargetPattern : MonoBehaviour, ITargetingPattern
{
    public void ExecuteAttack(BaseTurret turret, TurretStatsInstance stats, GameObject primaryTarget)
    {
        if (primaryTarget == null) return;
        var enemy = primaryTarget.GetComponent<Enemy>();
        if (enemy == null) return;

        turret.DamageEffects.ApplyAll(enemy, stats);
    }
}
