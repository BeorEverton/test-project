using Assets.Scripts.Enemies;
using Assets.Scripts.Turrets;
using UnityEngine;

public class BounceDamageEffect : IDamageEffect
{
    private int _bounceIndex;
    private readonly float _bounceMultiplier;

    public BounceDamageEffect(float bounceMultiplier)
    {
        _bounceMultiplier = bounceMultiplier;
    }

    public void SetBounceIndex(int bounceIndex)
    {
        _bounceIndex = bounceIndex;
    }

    public float ModifyDamage(float currentDamage, TurretStatsInstance stats, Enemy enemy)
    {
        float scaledDamage = currentDamage * Mathf.Pow(_bounceMultiplier, _bounceIndex);
        if (scaledDamage <= 0f)
            scaledDamage = 1f;
        return scaledDamage;
    }
}
