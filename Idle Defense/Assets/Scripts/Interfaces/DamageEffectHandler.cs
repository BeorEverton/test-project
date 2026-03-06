using Assets.Scripts.Enemies;
using Assets.Scripts.Systems;
using Assets.Scripts.Turrets;
using System.Collections.Generic;
using UnityEngine;

public class DamageEffectHandler
{
    private readonly List<IDamageEffect> _effects = new();

    public void AddEffect(IDamageEffect effect) => _effects.Add(effect);

    public void ApplyAll(Enemy enemy, TurretStatsInstance stats, int dealerSlotIndex)
    {
        ApplyAllInternal(enemy, stats, stats.Damage, null, false, dealerSlotIndex);
    }

    /// <summary>
    /// Apply all registered effects starting from an override base damage.
    /// </summary>
    public void ApplyAll(Enemy enemy, TurretStatsInstance stats, float baseDamageOverride, int dealerSlotIndex)
    {
        ApplyAllInternal(enemy, stats, baseDamageOverride, null, false, dealerSlotIndex);
    }

    /// <summary>
    /// Apply all registered effects, then apply any extra one-shot effects (e.g., splash).
    /// </summary>
    public void ApplyAll(Enemy enemy, TurretStatsInstance stats, int dealerSlotIndex, params IDamageEffect[] extraEffects)
    {
        ApplyAllInternal(enemy, stats, stats.Damage, extraEffects, false, dealerSlotIndex);
    }

    /// <summary>
    /// Apply with both base override and extra effects if needed.
    /// </summary>
    public void ApplyAll(Enemy enemy, TurretStatsInstance stats, float baseDamageOverride, int dealerSlotIndex,params IDamageEffect[] extraEffects)
    {
        ApplyAllInternal(enemy, stats, baseDamageOverride, extraEffects, false, dealerSlotIndex);
    }

    public void ApplyAll_NoBounce(Enemy enemy, TurretStatsInstance stats, float baseDamageOverride, int dealerSlotIndex)
    {
        ApplyAllInternal(enemy, stats, baseDamageOverride, null, true, dealerSlotIndex);
    }

    private void ApplyAllInternal(
        Enemy enemy,
        TurretStatsInstance stats,
        float startingDamage,
        IDamageEffect[] extraEffects,
        bool skipBounce = false,
        int dealerSlotIndex = -1
    )
    {
        if (enemy == null) return;

        float totalDamage = startingDamage;

        // Global, always-on effects
        foreach (var effect in _effects)
        {
            if (skipBounce && effect != null && effect.GetType().Name == "BounceDamageEffect")
                continue;

            totalDamage = effect.ModifyDamage(totalDamage, stats, enemy);
        }

        // Per-hit, one-shot effects (e.g., Splash for non-primary targets)
        if (extraEffects != null)
        {
            for (int i = 0; i < extraEffects.Length; i++)
            {
                if (extraEffects[i] == null) continue;
                totalDamage = extraEffects[i].ModifyDamage(totalDamage, stats, enemy);
            }
        }

        // Auto-identify AOE from turret stats.
        bool isAoe =
            (stats.ExplosionRadius > 0f) ||
            (stats.ConeAngle > 0f) ||
            (stats.ExplosionDelay > 0f) ||
            (stats.SplashDamage > 0f);

        if (totalDamage <= 0f)
            return;

        float armorPenApplied = 0f;

        if (stats.ArmorPenetration > 0f)
        {
            float apChance = stats.ArmorPenetrationChance;

            if (apChance >= 100f)
                armorPenApplied = stats.ArmorPenetration;
            else if (apChance > 0f && Random.value <= (apChance / 100f))
                armorPenApplied = stats.ArmorPenetration;
        }

        // Damage application
        enemy.TakeDamage(totalDamage, armorPenApplied, isAoe);
        StatsManager.Instance.AddTurretDamage(stats.TurretType, totalDamage);
    }


    public void DebugGetEffects()
    {
        foreach (var effect in _effects)
        {
            UnityEngine.Debug.Log($"Effect: {effect.GetType().Name}");
        }
    }
}
