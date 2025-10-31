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
        public bool IsFlying;
        [Tooltip("Amount of coins to drop on death. Will be randomized with +- 5%")]
        public ulong CoinDropAmount;
        public Currency CurrencyDropType;

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
        [Tooltip("The amount the movementspeed can differ from MovementSpeed")]
        public float AttackRangeDifference = .1f;
        [Tooltip("Attacks per second. - 0.5 makes the enemy attack once every two second")]
        public float AttackSpeed;
        [Range(1, 5)] public int SweepTargets = 1; // how many gunners to hit at once

        [Header("Defense stats")]
        [Range(0f, 0.9f)] public float Armor; // 0–90%
        [Range(0f, 0.9f)] public float DodgeChance;

        [Header("Special: Shielded")]
        [Tooltip("If > 0, this enemy blocks the first X damage *instances* entirely.")]
        public int ShieldCharges = 0;

        [Header("Special: Exploder (on death)")]
        [Tooltip("Enable: on death, damage gunners in radius; this occurs before rewards are granted.")]
        public bool ExploderEnabled = false;
        [Tooltip("Delay before the death explosion damage is applied (seconds).")]
        public float ExploderDelay = 0f;
        [Tooltip("Explosion radius (world units on X axis around enemy).")]
        public float ExploderRadius = 3f;
        [Tooltip("Max number of gunners hit by the death explosion (1..5).")]
        [Range(1, 5)] public int ExploderMaxGunners = 2;

        [Header("Special: Healer")]
        [Tooltip("Enable: periodically heal nearby allies (not self).")]
        public bool HealerEnabled = false;
        [Tooltip("Seconds between heals.")]
        public float HealerCooldown = 5f;
        [Tooltip("Heal amount as % of target's MaxHealth (0..1).")]
        public float HealerHealPctOfMaxHP = 0.10f;
        [Tooltip("Heal radius (grid/world range to search enemies).")]
        public float HealerRadius = 6f;
        [Tooltip("Maximum allies to heal per tick.")]
        public int HealerMaxTargets = 2;

        [Header("Special: Kamikaze (on reach)")]
        [Tooltip("Enable: when reaching attack range, self-explodes and grants NO rewards or XP.")]
        public bool KamikazeOnReach = false;
        [Tooltip("Suicide explosion radius on X around enemy.")]
        public float KamikazeRadius = 3f;
        [Tooltip("Max number of gunners hit by suicide explosion (1..5).")]
        [Range(1, 5)] public int KamikazeMaxGunners = 3;

        [Header("Special: Summoner")]
        [Tooltip("Enable: this enemy can spawn other enemies using the wave pool.")]
        public bool SummonerEnabled = false;

        [Tooltip("Prefab of the enemy to spawn (must match a pooled enemy prefab).")]
        public GameObject SummonPrefab;

        [Tooltip("How many instances to prewarm into the pool at OnEnable.")]
        public int SummonPrewarmCount = 0;

        public enum SummonMode { Burst, Stream }

        [Tooltip("Spawn mode: Burst (all at once) or Stream (with a delay between each).")]
        public SummonMode SummonType = SummonMode.Burst;

        [Tooltip("Total enemies spawned per activation.")]
        public int SummonCount = 10;

        [Tooltip("Delay between each spawned unit when in Stream mode.")]
        public float SummonStreamInterval = 5f;

        [Header("Summon Placement")]
        [Tooltip("Depth (Z) offset toward the player. Positive values place spawns in front of the summoner (closer to the base).")]
        public float SummonForwardDepth = 0.8f;

        [Tooltip("Random X jitter to avoid perfect overlap.")]
        public float SummonXJitter = 0.5f;

        [Header("Summon Timing")]
        [Tooltip("Delay before the first summon after the enemy spawns.")]
        public float SummonFirstDelay = 0.75f;

        [Tooltip("Cooldown between repeated summons. If <= 0, summon happens once.")]
        public float SummonCooldown = 0f;

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