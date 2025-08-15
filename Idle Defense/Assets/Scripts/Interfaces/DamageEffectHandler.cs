using Assets.Scripts.Enemies;
using Assets.Scripts.Systems;
using Assets.Scripts.Turrets;
using System.Collections.Generic;

public class DamageEffectHandler
{
    private readonly List<IDamageEffect> _effects = new();

    public void AddEffect(IDamageEffect effect) => _effects.Add(effect);

    public void ApplyAll(Enemy enemy, TurretStatsInstance stats)
    {
        if (enemy == null) return;

        float totalDamage = stats.Damage;

        // Let each effect modify the damage
        foreach (var effect in _effects)
        {
            totalDamage = effect.ModifyDamage(totalDamage, stats, enemy);
        }

        // Apply the combined damage once
        if (totalDamage > 0)
        {
            enemy.TakeDamage(totalDamage);
            StatsManager.Instance.AddTurretDamage(stats.TurretType, totalDamage);
        }
    }

    public void DebugGetEffects()
    {
        foreach (var effect in _effects)
        {
            UnityEngine.Debug.Log($"Effect: {effect.GetType().Name}");
        }
    }
}
