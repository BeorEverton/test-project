using Assets.Scripts.Enemies;
using Assets.Scripts.Turrets;

public interface IDamageDealer : IDamageEffect
{
    float ApplyAndReturnDamage(Enemy enemy, TurretStatsInstance stats);
}
