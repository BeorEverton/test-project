using UnityEngine;

namespace Assets.Scripts.SO
{
    [CreateAssetMenu(fileName = "EnemyStats", menuName = "ScriptableObjects/EnemyStats", order = 2)]
    public class EnemyInfoSO : ScriptableObject
    {
        [Header("Base info")]
        public string Name;

        [Header("Base stats")]
        [Tooltip("Max health")]
        public float MaxHealth;
        [Tooltip("Movement speed")]
        public float MovementSpeed;
        [Tooltip("The amount the movementspeed can differ from MovementSpeed")]
        public float MovementSpeedDifference;
        [Tooltip("Amount of coins to drop on death. Will be randomized with +- 5%")]
        public ulong CoinDropAmount;

        [Header("Attack stats")]
        [Tooltip("Damage dealth to the player per attack")]
        public int Damage;
        [Tooltip("Attack range for the enemy, before attacking the player")]
        public float AttackRange;
        [Tooltip("Time between each attack. - 0.5 makes the enemy attack twice per second")]
        public float AttackSpeed;

        [Header("Wave settings")]
        [Tooltip("WaveManager sets this at runtime")]
        public float AddMaxHealth;
    }
}