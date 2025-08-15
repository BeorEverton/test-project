using Assets.Scripts.Enemies;
using Assets.Scripts.Turrets;
using UnityEngine;

public class PierceDamageEffect : IDamageEffect
{
    private int _hitIndex;
    private readonly float _falloffPercent;

    public PierceDamageEffect(float falloffPercent)
    {
        _falloffPercent = falloffPercent;
    }

    public void SetHitIndex(int hitIndex)
    {
        _hitIndex = hitIndex;
    }

    public float ModifyDamage(float currentDamage, TurretStatsInstance stats, Enemy enemy)
    {
        if (_hitIndex == 0) return currentDamage; // First hit full damage

        float multiplier = 1f - (_falloffPercent / 100f);
        if (multiplier < 0f) multiplier = 0f;

        float scaledDamage = currentDamage * Mathf.Pow(multiplier, _hitIndex);
        return Mathf.Max(scaledDamage, 1f); // Never below 1
    }
}
