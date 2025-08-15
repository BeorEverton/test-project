using Assets.Scripts.Systems;
using Assets.Scripts.Turrets;
using UnityEngine;

[Tooltip("A targeting pattern that targets multiple enemies simultaneously, if there are less enemies than pellets, remaining pellets are lost.")]
public class MultiTargetSimultaneousPattern : MonoBehaviour, ITargetingPattern
{
    public void ExecuteAttack(BaseTurret turret, TurretStatsInstance stats, GameObject primaryTarget)
    {
        // Use explosion radius from stats instead of turret attack range
        var searchRadius = Mathf.CeilToInt(stats.ExplosionRadius);

        var enemies = GridManager.Instance.GetEnemiesInRange(
            primaryTarget.transform.position, searchRadius);

        if (enemies.Count == 0) return;

        int count = Mathf.Min(stats.PelletCount, enemies.Count);
        for (int i = 0; i < count; i++)
        {
            var enemy = enemies[i];
            if (enemy != null)
            {
                turret.DamageEffects.ApplyAll(enemy, stats);
            }
        }
    }

}
