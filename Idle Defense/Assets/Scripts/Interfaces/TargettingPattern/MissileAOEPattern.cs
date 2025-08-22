using Assets.Scripts.Enemies;
using Assets.Scripts.Systems;
using Assets.Scripts.Systems.Audio;
using Assets.Scripts.Turrets;
using UnityEngine;
using static Assets.Scripts.Turrets.MissileController;

public class MissileAOEPattern : MonoBehaviour, ITargetingPattern
{
    [SerializeField] private MissileController missileController;
    [SerializeField] private string explosionSound;

    // MissileAOEPattern.cs
    public void ExecuteAttack(BaseTurret turret, TurretStatsInstance stats, GameObject primaryTarget)
    {
        if (primaryTarget == null) return;
        if (!primaryTarget.TryGetComponent(out Enemy enemy)) return;

        float travelTime = 0.8f;
        Vector3 predictedPos = (enemy.transform.position.Depth() <= enemy.Info.AttackRange)
            ? enemy.transform.position
            : enemy.transform.position - Vector3.forward * enemy.Info.MovementSpeed * travelTime;

        System.EventHandler<MissileHitEventArgs> handler = null;
        handler = (s, e) =>
        {
            missileController.OnMissileHit -= handler;   // prevent accumulation
            CreateExplosion(e.HitPosition, stats, turret, enemy);
        };
        missileController.OnMissileHit += handler;

        missileController.Launch(predictedPos, travelTime);
    }


    private void CreateExplosion(Vector3 hitPos, TurretStatsInstance stats, BaseTurret turret, Enemy primaryEnemy)
    {
        var inRange = GridManager.Instance.GetEnemiesInRange(hitPos, Mathf.CeilToInt(stats.ExplosionRadius));
        float innerRadius = stats.ExplosionRadius / 3f;

        foreach (var e in inRange)
        {
            if (e == null) continue;

            float dist = Vector3.Distance(e.transform.position, hitPos);
            bool isPrimary = (primaryEnemy != null && e == primaryEnemy) || dist <= innerRadius;

            if (isPrimary)
            {
                // Full (flat) damage to primary/inner ring
                turret.DamageEffects.ApplyAll(e, stats);
            }
            else if (turret.SplashDamageEffectRef != null && stats.SplashDamage > 0f)
            {
                // Splash = percentage of the total after all other effects
                turret.DamageEffects.ApplyAll(e, stats, turret.SplashDamageEffectRef);
            }
            else
            {
                // Fallback: full if splash not configured
                turret.DamageEffects.ApplyAll(e, stats);
            }
        }

        AudioManager.Instance.PlayWithVariation(explosionSound, 0.5f, 1f);
    }

}
