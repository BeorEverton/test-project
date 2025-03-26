using UnityEngine;

namespace Assets.Scripts.SO
{
    [CreateAssetMenu(fileName = "EnemyStats", menuName = "ScriptableObjects/EnemyStats", order = 2)]
    public class EnemyStatsSO : ScriptableObject
    {
        [Header("Base stats")]
        [Tooltip("Max health")]
        public float MaxHealth;
        [Tooltip("Movement speed")]
        public float Speed;

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