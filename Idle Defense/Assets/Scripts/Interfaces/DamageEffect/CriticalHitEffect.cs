using Assets.Scripts.Enemies;
using Assets.Scripts.Systems.Audio;
using Assets.Scripts.Turrets;

public class CriticalHitEffect : IDamageEffect
{
    public float ModifyDamage(float currentDamage, TurretStatsInstance stats, Enemy enemy)
    {
        if (UnityEngine.Random.Range(0, 100) < stats.CriticalChance)
        {
            AudioManager.Instance.Play("Critical");
            enemy.tookCriticalHit = true;
            currentDamage *= 1f + (stats.CriticalDamageMultiplier / 100f);
        }
        return currentDamage;
    }
}
