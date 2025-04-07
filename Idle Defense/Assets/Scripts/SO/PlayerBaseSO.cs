using UnityEngine;

namespace Assets.Scripts.SO
{
    [CreateAssetMenu(fileName = "PlayerBaseStats", menuName = "ScriptableObjects/PlayerBase", order = 3)]
    public class PlayerBaseSO : ScriptableObject
    {
        [Tooltip("Max health of player base")]
        public float MaxHealth;
    }
}