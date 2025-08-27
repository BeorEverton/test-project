public struct GunnerBonus
{
    public float Damage;
    public float FireRate;
    public float Range;
    public float PercentBonusDamagePerSec;
    public float SlowEffect;
    public float CriticalChance;           // %
    public float CriticalDamageMultiplier; // %
    public float KnockbackStrength;
    public float SplashDamage;
    public float PierceChance;
    public float PierceDamageFalloff;
    public float ArmorPenetration;         // %

    public void Clear() { this = default; }
}
