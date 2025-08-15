using Assets.Scripts.Turrets;
using UnityEngine;

public interface ITargetingPattern
{
    /// <summary>
    /// Executes the turret's attack pattern.
    /// </summary>
    /// <param name="turret">The turret calling the attack.</param>
    /// <param name="stats">The turret's runtime stats.</param>
    /// <param name="primaryTarget">The current locked target enemy.</param>
    void ExecuteAttack(BaseTurret turret, TurretStatsInstance stats, GameObject primaryTarget);
}
