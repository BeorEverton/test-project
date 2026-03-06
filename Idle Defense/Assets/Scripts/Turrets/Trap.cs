using Assets.Scripts.Enemies;
using Assets.Scripts.Systems;
using Assets.Scripts.Turrets;
using System.Collections;
using UnityEngine;

public class Trap
{
    public GameObject visual;
    public Vector2Int cell;
    public float worldY;
    public float damage; // keep as fallback / LB traps
    public float delay;
    public float radius;
    public bool isActive;

    // For debug/UI only; do not rely on this existing at detonation time
    public BaseTurret ownerTurret;

    // NEW: trap ties to a slot, not to a turret GameObject
    public int ownerSlotIndex = -1;

    // Cache these so trap still works even if turret is destroyed
    public DamageEffectHandler ownerDamageEffects;
    public SplashDamageEffect splashDamageEffectRef;

    // Reused scratch to avoid allocations
    private TurretStatsInstance _effectiveScratch;

    public void Trigger(Enemy triggeringEnemy)
    {
        if (!isActive) return;

        // Always run on persistent host; turret coroutines die when turret is destroyed
        TrapPoolManager.Instance.StartCoroutine(TriggerRoutine(triggeringEnemy));
    }

    protected virtual IEnumerator TriggerRoutine(Enemy triggeringEnemy)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        // Build effective stats at detonation time (gunner might have changed)
        TurretStatsInstance eff = TryBuildEffectiveStatsNow();

        // If we couldn't build a context (LB trap / slot invalid), fall back to flat damage
        if (eff == null || ownerDamageEffects == null)
        {
            FallbackFlatDamage(triggeringEnemy);
            TrapPoolManager.Instance.DisableTrap(this);
            yield break;
        }

        if (radius <= 0f)
        {
            ownerDamageEffects.ApplyAll(triggeringEnemy, eff, ownerSlotIndex);
        }
        else
        {
            Vector3 trapWorldPosition = GridManager.Instance.GetWorldPosition(cell, worldY);

            var enemies = GridManager.Instance.GetEnemiesInRange(
                trapWorldPosition,
                Mathf.CeilToInt(radius),
                eff.CanHitFlying);

            for (int i = 0; i < enemies.Count; i++)
            {
                var e = enemies[i];
                if (e == null) continue;
                if (Vector3.Distance(e.transform.position, trapWorldPosition) > radius) continue;

                bool isPrimary = (e == triggeringEnemy);

                if (isPrimary)
                {
                    ownerDamageEffects.ApplyAll(e, eff, ownerSlotIndex);
                }
                else if (splashDamageEffectRef != null && eff.SplashDamage > 0f)
                {
                    ownerDamageEffects.ApplyAll(e, eff, ownerSlotIndex, splashDamageEffectRef);
                }
                else
                {
                    ownerDamageEffects.ApplyAll(e, eff, ownerSlotIndex);
                }
            }
        }

        TrapPoolManager.Instance.DisableTrap(this);
    }

    private TurretStatsInstance TryBuildEffectiveStatsNow()
    {
        if (ownerSlotIndex < 0) return null;

        // This should exist even if the turret GameObject was destroyed:
        // SlotWorldButton uses TurretSlotManager.Instance.Get(slotIndex) as the source of truth.
        var baseStats = TurretSlotManager.Instance != null ? TurretSlotManager.Instance.Get(ownerSlotIndex) : null;
        if (baseStats == null) return null;

        if (_effectiveScratch == null)
            _effectiveScratch = new TurretStatsInstance();

        CopyStatsForCombat(baseStats, _effectiveScratch);

        // Layer gunner bonuses (dynamic)
        if (GunnerManager.Instance != null)
            GunnerManager.Instance.ApplyTo(baseStats, ownerSlotIndex, _effectiveScratch);

        // Layer prestige if your project uses it (matches BaseTurret.BuildEffectiveStats)
        if (PrestigeManager.Instance != null)
            PrestigeManager.Instance.ApplyToTurretStats(_effectiveScratch);

        // Layer Heat Management outgoing damage penalty (matches BaseTurret.BuildEffectiveStats)
        if (LimitBreakManager.Instance != null && GunnerManager.Instance != null)
        {
            string gunnerId = GunnerManager.Instance.GetEquippedGunnerId(ownerSlotIndex);
            if (!string.IsNullOrEmpty(gunnerId))
            {
                float outMult = LimitBreakManager.Instance.GetHeatManagementOutgoingDamageMultiplier(gunnerId);
                if (outMult < 0.999f)
                {
                    _effectiveScratch.Damage *= outMult;
                    _effectiveScratch.SplashDamage *= outMult;
                }
            }
        }

        return _effectiveScratch;
    }

    private void FallbackFlatDamage(Enemy triggeringEnemy)
    {
        if (radius <= 0f)
        {
            triggeringEnemy.TakeDamage(damage, armorPenetrationPct: 0f, isAoe: false, isCritical: false);
            return;
        }

        Vector3 trapWorldPosition = GridManager.Instance.GetWorldPosition(cell, worldY);
        var enemies = GridManager.Instance.GetEnemiesInRange(trapWorldPosition, Mathf.CeilToInt(radius), false);

        for (int i = 0; i < enemies.Count; i++)
        {
            var e = enemies[i];
            if (e == null) continue;
            if (Vector3.Distance(e.transform.position, trapWorldPosition) > radius) continue;
            e.TakeDamage(damage, armorPenetrationPct: 0f, isAoe: true, isCritical: false);
        }
    }

    
    private static void CopyStatsForCombat(TurretStatsInstance src, TurretStatsInstance dst)
    {
        dst.TurretType = src.TurretType;

        dst.Damage = src.Damage;
        dst.FireRate = src.FireRate;
        dst.Range = src.Range;

        dst.CriticalChance = src.CriticalChance;
        dst.CriticalDamageMultiplier = src.CriticalDamageMultiplier;

        dst.KnockbackStrength = src.KnockbackStrength;
        dst.SplashDamage = src.SplashDamage;

        dst.PierceChance = src.PierceChance;
        dst.PierceDamageFalloff = src.PierceDamageFalloff;

        dst.PercentBonusDamagePerSec = src.PercentBonusDamagePerSec;
        dst.SlowEffect = src.SlowEffect;

        dst.CanHitFlying = src.CanHitFlying;
        dst.ArmorPenetration = src.ArmorPenetration;

        dst.BounceCount = src.BounceCount;
        dst.BounceRange = src.BounceRange;
        dst.BounceDelay = src.BounceDelay;
        dst.BounceDamagePct = src.BounceDamagePct;

        dst.ConeAngle = src.ConeAngle;
        dst.ExplosionDelay = src.ExplosionDelay;
    }
}