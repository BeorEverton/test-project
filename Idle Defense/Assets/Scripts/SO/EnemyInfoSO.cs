using UnityEngine;

namespace Assets.Scripts.SO
{
    [CreateAssetMenu(fileName = "EnemyStats", menuName = "ScriptableObjects/EnemyStats", order = 2)]
    public class EnemyInfoSO : ScriptableObject
    {
        [Header("Base info")]
        public string Name;
        public Sprite Icon;
        [Tooltip("The class of the enemy. Will be used to scale through levels")]
        public EnemyClass EnemyClass;

        [Header("Base stats")]
        [Tooltip("Max health")]
        public float MaxHealth;
        [Tooltip("Movement speed")]
        public float MovementSpeed;
        [Tooltip("The amount the movementspeed can differ from MovementSpeed")]
        public float MovementSpeedDifference;
        [Tooltip("Amount of coins to drop on death. Will be randomized with +- 5%")]
        public ulong CoinDropAmount;

        [Header("Wave Multipliers")]
        [Tooltip("CoinDrop = Ceil(CoinDrop * coinDropmultiplier) (Default = 1.05, 5% increase per wave)")]
        public float CoinDropMultiplierByWaveCount = 1.05f;
        [Tooltip("Maxhealth += waveCount * healthMultiplier (Default = 2)")]
        public float HealthMultiplierByWaveCount = 2f;

        [Header("Attack stats")]
        [Tooltip("Damage dealth to the player per attack")]
        public float Damage;
        [Tooltip("Attack range for the enemy, before attacking the player")]
        public float AttackRange;
        [Tooltip("Attacks per second. - 0.5 makes the enemy attack once every two second")]
        public float AttackSpeed;
    }

    public enum EnemyClass
    {
        Melee,
        Ranged,
        Tank,
        Scout,
        Charger,
        Sniper,
        Assassin,
        Champion,
        Grunt, // melee
        Fusai,
        Breaker,
        Marksman, // ranged
        Arashi,
        Howler,
        Bulwark, // tank
        Kaba,
        Bunker,
        Spotter, // scout
        Hayai,
        Trace,
        Blitz, // charger
        Totsugeki,
        Crash,
        Scope, // sniper
        Kanshi,
        Deadeye,
        Shade, // assassin
        Shinobi,
        Vanish,
        Dreadnought, // champion
        Shogun,
        Overlord
    }
}