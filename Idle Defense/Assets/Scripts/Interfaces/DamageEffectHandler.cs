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
        ApplyAllInternal(enemy, stats, stats.Damage, null);
    }

    /// <summary>
    /// Apply all registered effects starting from an override base damage.
    /// </summary>
    public void ApplyAll(Enemy enemy, TurretStatsInstance stats, float baseDamageOverride)
    {
        ApplyAllInternal(enemy, stats, baseDamageOverride, null);
    }

    /// <summary>
    /// Apply all registered effects, then apply any extra one-shot effects (e.g., splash).
    /// </summary>
    public void ApplyAll(Enemy enemy, TurretStatsInstance stats, params IDamageEffect[] extraEffects)
    {
        ApplyAllInternal(enemy, stats, stats.Damage, extraEffects);
    }

    /// <summary>
    /// Apply with both base override and extra effects if needed.
    /// </summary>
    public void ApplyAll(Enemy enemy, TurretStatsInstance stats, float baseDamageOverride, params IDamageEffect[] extraEffects)
    {
        ApplyAllInternal(enemy, stats, baseDamageOverride, extraEffects);
    }

    private void ApplyAllInternal(Enemy enemy, TurretStatsInstance stats, float startingDamage, IDamageEffect[] extraEffects)
    {
        if (enemy == null) return;

        float totalDamage = startingDamage;

        // Global, always-on effects
        foreach (var effect in _effects)
            totalDamage = effect.ModifyDamage(totalDamage, stats, enemy);

        // Per-hit, one-shot effects (e.g., Splash for non-primary targets)
        if (extraEffects != null)
        {
            for (int i = 0; i < extraEffects.Length; i++)
            {
                if (extraEffects[i] == null) continue;
                totalDamage = extraEffects[i].ModifyDamage(totalDamage, stats, enemy);
            }
        }

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
