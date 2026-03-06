using System.Collections.Generic;

[System.Serializable]
public struct PrestigeEffects
{
    // Global base
    public float dmgPct;
    public float fireRatePct;
    public float critChancePct;
    public float critDmgPct;
    public float pierceChancePct;

    // Full turret stat coverage
    public float rangePct;
    public float rotationSpeedPct;
    public float explosionRadiusPct;
    public float splashDamagePct;
    public float pierceDamageFalloffPct;
    public float pelletCountPct;
    public float pelletChancePct;
    public float damageFalloffOverDistancePct;
    public float percentBonusDpsPct;
    public float slowEffectPct;
    public float slowChancePct;
    public float knockbackStrengthPct;
    public float knockbackChancePct;

    public float bounceCountPct;
    public float bounceChancePct;
    public float bounceRangePct;
    public float bounceDelayPct;
    public float bounceDamagePctPct;

    public float coneAnglePct;
    public float explosionDelayPct;
    public float aheadDistancePct;
    public float maxTrapsActivePct;

    public float armorPenPct;
    public float armorPenChancePct;

    // Economy / Meta
    public float scrapsGainPct;
    public float blackSteelGainPct;

    // Enemy
    public float enemyHealthPct;
    public float enemyCountPct;

    // System caps
    public float speedCapBonus;

    // Upgrade costs
    public float allUpgradeCostPct; // total global discount 0..100

    // Per-type discounts (accumulated)
    public Dictionary<Assets.Scripts.Systems.TurretUpgradeType, float> perTypeDiscounts;

    public static PrestigeEffects operator +(PrestigeEffects a, PrestigeEffects b)
    {
        var c = new PrestigeEffects
        {
            dmgPct = a.dmgPct + b.dmgPct,
            fireRatePct = a.fireRatePct + b.fireRatePct,
            critChancePct = a.critChancePct + b.critChancePct,
            critDmgPct = a.critDmgPct + b.critDmgPct,
            pierceChancePct = a.pierceChancePct + b.pierceChancePct,

            rangePct = a.rangePct + b.rangePct,
            rotationSpeedPct = a.rotationSpeedPct + b.rotationSpeedPct,
            explosionRadiusPct = a.explosionRadiusPct + b.explosionRadiusPct,
            splashDamagePct = a.splashDamagePct + b.splashDamagePct,
            pierceDamageFalloffPct = a.pierceDamageFalloffPct + b.pierceDamageFalloffPct,
            pelletCountPct = a.pelletCountPct + b.pelletCountPct,
            pelletChancePct = a.pelletChancePct + b.pelletChancePct,
            damageFalloffOverDistancePct = a.damageFalloffOverDistancePct + b.damageFalloffOverDistancePct,
            percentBonusDpsPct = a.percentBonusDpsPct + b.percentBonusDpsPct,
            slowEffectPct = a.slowEffectPct + b.slowEffectPct,
            slowChancePct = a.slowChancePct + b.slowChancePct,
            knockbackStrengthPct = a.knockbackStrengthPct + b.knockbackStrengthPct,
            knockbackChancePct = a.knockbackChancePct + b.knockbackChancePct,

            bounceCountPct = a.bounceCountPct + b.bounceCountPct,
            bounceChancePct = a.bounceChancePct + b.bounceChancePct,
            bounceRangePct = a.bounceRangePct + b.bounceRangePct,
            bounceDelayPct = a.bounceDelayPct + b.bounceDelayPct,
            bounceDamagePctPct = a.bounceDamagePctPct + b.bounceDamagePctPct,

            coneAnglePct = a.coneAnglePct + b.coneAnglePct,
            explosionDelayPct = a.explosionDelayPct + b.explosionDelayPct,
            aheadDistancePct = a.aheadDistancePct + b.aheadDistancePct,
            maxTrapsActivePct = a.maxTrapsActivePct + b.maxTrapsActivePct,

            armorPenPct = a.armorPenPct + b.armorPenPct,
            armorPenChancePct = a.armorPenChancePct + b.armorPenChancePct,

            scrapsGainPct = a.scrapsGainPct + b.scrapsGainPct,
            blackSteelGainPct = a.blackSteelGainPct + b.blackSteelGainPct,

            enemyHealthPct = a.enemyHealthPct + b.enemyHealthPct,
            enemyCountPct = a.enemyCountPct + b.enemyCountPct,

            speedCapBonus = a.speedCapBonus + b.speedCapBonus,
            allUpgradeCostPct = a.allUpgradeCostPct + b.allUpgradeCostPct,
            perTypeDiscounts = new Dictionary<Assets.Scripts.Systems.TurretUpgradeType, float>()
        };

        // merge dictionaries (sum pcts)
        if (a.perTypeDiscounts != null)
            foreach (var kv in a.perTypeDiscounts)
                c.perTypeDiscounts[kv.Key] = kv.Value;

        if (b.perTypeDiscounts != null)
            foreach (var kv in b.perTypeDiscounts)
                c.perTypeDiscounts[kv.Key] = (c.perTypeDiscounts.TryGetValue(kv.Key, out var cur) ? cur : 0f) + kv.Value;

        return c;
    }
}
