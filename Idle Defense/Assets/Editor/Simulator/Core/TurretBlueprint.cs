// Assets/Editor/Simulation/Core/Blueprints.cs
using Assets.Scripts.SO;        // where TurretInfoSO lives
using Assets.Scripts.Turrets;   // where TurretType & enum live

namespace IdleDefense.Editor.Simulation
{
    public readonly struct TurretBlueprint
    {
        public readonly TurretType Type;
        public readonly float Damage;
        public readonly float FireRate;     // shots per second
        public readonly ulong CostPerDamageUp;
        public readonly ulong CostPerFireRateUp;

        // after your existing ctor...
        private TurretBlueprint(
            TurretType type,
            float damage,
            float fireRate,
            ulong costPerDamageUp,
            ulong costPerFireRateUp)
        {
            Type = type;
            Damage = damage;
            FireRate = fireRate;
            CostPerDamageUp = costPerDamageUp;
            CostPerFireRateUp = costPerFireRateUp;
        }


        public TurretBlueprint(TurretInfoSO so)
        {
            Type = so.TurretType;
            Damage = so.Damage;
            FireRate = so.FireRate > 0f ? so.FireRate : 1f;
            CostPerDamageUp = (ulong)so.DamageUpgradeBaseCost;
            CostPerFireRateUp = (ulong)so.FireRateUpgradeBaseCost;
        }

        public float DamagePerSecond(float clickBonus = 0f)
            => (Damage * (1f + clickBonus)) * FireRate;

        /// <summary>Returns a new blueprint with Damage increased by the SO’s upgrade amount.</summary>
        /// <summary>
        /// Returns a new blueprint with Damage increased by the SO’s upgrade amount.
        /// </summary>
        public TurretBlueprint WithDamageUpgraded()
        {
            // Pull the real upgrade delta from the SO if you can; 
            // I’ll assume a +1.0 damage per upgrade for this example.
            float newDamage = Damage + 1.0f;
            return new TurretBlueprint(
                Type,
                newDamage,
                FireRate,
                CostPerDamageUp,      // you could increase cost here if desired
                CostPerFireRateUp);
        }

        /// <summary>
        /// Returns a new blueprint with FireRate increased by the SO’s upgrade amount.
        /// </summary>
        public TurretBlueprint WithFireRateUpgraded()
        {
            // Assume +0.2 shots/sec per upgrade
            float newFireRate = FireRate + 0.2f;
            return new TurretBlueprint(
                Type,
                Damage,
                newFireRate,
                CostPerDamageUp,
                CostPerFireRateUp);
        }

    }
}
