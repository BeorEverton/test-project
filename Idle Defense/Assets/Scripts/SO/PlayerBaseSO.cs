using UnityEngine;

namespace Assets.Scripts.SO
{
    [CreateAssetMenu(fileName = "PlayerBaseStats", menuName = "ScriptableObjects/PlayerBase", order = 3)]
    public class PlayerBaseSO : ScriptableObject
    {
        [Header("Health")]
        [Tooltip("Max health of player base")]
        public float MaxHealth;

        [Tooltip("Amount of health regenerated per tick")]
        public float RegenAmount;

        [Tooltip("Delay after taking damage before regen starts")]
        public float RegenDelay;

        [Tooltip("Time interval between regen ticks")]
        public float RegenInterval = 0.5f;

        [Tooltip("Upgrade amount per level for MaxHealth")]
        public float MaxHealthUpgradeAmount;
        public int MaxHealthUpgradeBaseCost;
        public int MaxHealthLevel;

        [Tooltip("Upgrade amount per level for RegenAmount")]
        public float RegenAmountUpgradeAmount;
        public int RegenAmountUpgradeBaseCost;
        public int RegenAmountLevel;

        [Tooltip("Upgrade amount per level for RegenInterval (lower is faster)")]
        public float RegenIntervalUpgradeAmount;
        public int RegenIntervalUpgradeBaseCost;
        public int RegenIntervalLevel;
    }
}