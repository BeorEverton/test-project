using Assets.Scripts.UI;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Assets.Scripts.Systems
{
    public class GameManager : MonoBehaviour
    {
        public float attackBonus { get; private set; }
        public float spdBonus { get; private set; }

        private const float maxSpdBonus = 200f;
        private const float increaseRate = maxSpdBonus / 10f; // Increase to 200 in 10 seconds
        private const float decreaseRate = maxSpdBonus / 30f; // Decrease to 0 in 30 seconds

        private void Update()
        {
            if (!EventSystem.current.IsPointerOverGameObject())
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
}
