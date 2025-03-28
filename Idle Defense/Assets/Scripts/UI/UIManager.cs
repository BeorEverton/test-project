using UnityEngine;
using TMPro;

namespace Assets.Scripts.UI
{
    public class UIManager : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI dmgBonus, spdBonus, wave, enemies, money;

        public static UIManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        public void UpdateWave(int newWave, int totalEnemies)
        {
            wave.text = $"Wave: {newWave + 1}"; // +1 to now show 'wave 0'
            enemies.text = $"Enemies: {totalEnemies}";
        }

        public void EnemyDied(int totalEnemies)
        {
            // Update enemies text
            enemies.text = "Enemies: " + totalEnemies;
        }
    }
}