using Assets.Scripts.Enemies;
using Assets.Scripts.Turrets;
using UnityEngine;

public class RampDamageOverTimeEffect : IDamageEffect
{
    private Enemy _lastTarget;
    private float _timeOnTarget;

    public float ModifyDamage(float currentDamage, TurretStatsInstance stats, Enemy enemy)
    {
        if (enemy != _lastTarget)
        {
            _lastTarget = enemy;
            _timeOnTarget = 0f;
        }

        _timeOnTarget += Time.deltaTime;

        float rampBonus = currentDamage * (stats.PercentBonusDamagePerSec / 100f) * _timeOnTarget;
        return currentDamage + rampBonus;
    }
}
