using Assets.Scripts.Enemies;
using Assets.Scripts.Turrets;

public class FlatDamageEffect : IDamageEffect
{
    public float ModifyDamage(float currentDamage, TurretStatsInstance stats, Enemy enemy)
    {
        return currentDamage; // No change, baseline damage already in total
    }
}
