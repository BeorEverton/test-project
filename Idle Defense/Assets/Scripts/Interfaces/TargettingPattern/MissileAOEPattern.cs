using Assets.Scripts.Enemies;
using Assets.Scripts.Systems;
using Assets.Scripts.Systems.Audio;
using Assets.Scripts.Turrets;
using UnityEngine;

public class MissileAOEPattern : MonoBehaviour, ITargetingPattern
{
    [SerializeField] private MissileController missileController;
    [SerializeField] private string explosionSound;

    public void ExecuteAttack(BaseTurret turret, TurretStatsInstance stats, GameObject primaryTarget)
    {
        if (primaryTarget == null) return;
        if (!primaryTarget.TryGetComponent(out Enemy enemy)) return;

        float travelTime = 0.8f;
        Vector3 predictedPos = new Vector3();
        if (enemy.transform.position.Depth() <= enemy.Info.AttackRange)
            predictedPos = enemy.transform.position;
        else
            predictedPos = enemy.transform.position - Vector3.forward * enemy.Info.MovementSpeed * travelTime;

        missileController.OnMissileHit += (s, e) => CreateExplosion(e.HitPosition, stats, turret);
        missileController.Launch(predictedPos, travelTime);
    }

    private void CreateExplosion(Vector3 hitPos, TurretStatsInstance stats, BaseTurret turret)
    {
        var inRange = GridManager.Instance.GetEnemiesInRange(hitPos, Mathf.CeilToInt(stats.ExplosionRadius));
        foreach (var e in inRange)
        {
            if (Vector3.Distance(e.transform.position, hitPos) <= stats.ExplosionRadius / 3f)
                turret.DamageEffects.ApplyAll(e, stats);
            else
                turret.DamageEffects.ApplyAll(e, stats);
        }
        AudioManager.Instance.PlayWithVariation(explosionSound, 0.5f, 1f);
    }
}
