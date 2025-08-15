using Assets.Scripts.Enemies;
using Assets.Scripts.Turrets;

public interface IDamageEffect
{
    /// <summary>
    /// Modify or add to the accumulated damage value for this hit.
    /// </summary>
    float ModifyDamage(float currentDamage, TurretStatsInstance stats, Enemy enemy);
}
