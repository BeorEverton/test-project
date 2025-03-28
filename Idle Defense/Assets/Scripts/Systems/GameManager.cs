using Assets.Scripts.UI;
using Unity.VisualScripting;
using UnityEngine;

namespace Assets.Scripts.Systems
{
    public class GameManager : MonoBehaviour
    {
        public float attackBonus { get; private set; }
        public float spdBonus { get; private set; }

        private const float maxSpdBonus = 200f;
        private const float increaseRate = maxSpdBonus / 5f; // Increase to max in 20 seconds
        private const float decreaseRate = maxSpdBonus / 60f; // Decrease to 0 in 2 minutes

        private void Update()
        {
            if (Input.GetMouseButton(0))
            {
                spdBonus += increaseRate * Time.deltaTime;
            }
            else
            {
                spdBonus -= decreaseRate * Time.deltaTime;
            }

            spdBonus = Mathf.Clamp(spdBonus, 0, maxSpdBonus);

            UIManager.Instance.UpdateSpdBonus(spdBonus);
        }
    }
}
