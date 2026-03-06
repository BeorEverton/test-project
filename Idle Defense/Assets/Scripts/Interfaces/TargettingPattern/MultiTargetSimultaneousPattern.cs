using Assets.Scripts.Systems;
using Assets.Scripts.Turrets;
using UnityEngine;

[Tooltip("A targeting pattern that targets multiple enemies simultaneously, if there are less enemies than pellets, remaining pellets are lost.")]
public class MultiTargetSimultaneousPattern : MonoBehaviour, ITargetingPattern
{
    [Header("Needs ExplosionRadius on the stats to work")]
    public byte _; // Only to show the message on the editor
    public void ExecuteAttack(BaseTurret turret, TurretStatsInstance stats, GameObject primaryTarget)
    {
        // Use explosion radius from stats instead of turret attack range
        var searchRadius = Mathf.CeilToInt(stats.ExplosionRadius);

        var enemies = GridManager.Instance.GetEnemiesInRange(
            primaryTarget.transform.position, searchRadius, stats.CanHitFlying);

        if (enemies.Count == 0) return;

        int pelletCountThisShot;

        if (stats.PelletCount <= 1)
        {
            pelletCountThisShot = 1;
        }
        else
        {
            float chance = stats.PelletChance;
            bool proc =
                (chance >= 100f) ||
                (chance > 0f && Random.value <= (chance / 100f));

            pelletCountThisShot = proc ? Mathf.Max(1, stats.PelletCount) : 1;
        }

        int count = Mathf.Min(pelletCountThisShot, enemies.Count);
        for (int i = 0; i < count; i++)
        {
            var enemy = enemies[i];
            if (enemy != null)
            {
                float baseDamage = turret.ComputeDamageAfterFalloff(enemy.transform.position, stats);
                turret.DamageEffects.ApplyAll(enemy, stats, baseDamage, turret.SlotIndex);
            }
        }
    }

}
