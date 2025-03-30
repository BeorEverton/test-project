using Assets.Scripts.WaveSystem;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI
{
    public class UIManager : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI dmgBonus, spdBonus, wave, enemies, money;
        [SerializeField] private Slider spdBonusSlider;
        private int waveOnHold;

        public static UIManager Instance { get; private set; }

        private int _enemyCount;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        private void Start()
        {
            EnemySpawner.Instance.OnWaveCreated += OnWaveCreated;
            EnemySpawner.Instance.OnEnemyDeath += OnEnemyDeath;
            EnemySpawner.Instance.OnWaveCompleted += OnWaveCompleted;
            WaveManager.Instance.OnWaveStarted += OnWaveStarted;
        }

        private void OnEnemyDeath(object sender, EventArgs e)
        {
            _enemyCount--;
            enemies.text = $"Enemies\n{_enemyCount}";
        }

        private void OnWaveStarted(object sender, WaveManager.OnWaveStartedEventArgs args)
        {
            wave.text = $"Next Wave\n{args.WaveNumber + 1}";

        }

        private void OnWaveCreated(object sender, EnemySpawner.OnWaveCreatedEventArgs args)
        {
            _enemyCount = args.EnemyCount;
            enemies.text = $"Enemies\n{_enemyCount}";
        }

        private void OnWaveCompleted(object sender, EventArgs e)
        {
            if (waveOnHold != -1)
            {
                WaveManager.Instance.SetWave(waveOnHold);
                waveOnHold = -1;
            }            
        }

        /// <summary>
        /// Button on the UI to reduce the wave number
        /// </summary>
        public void ReduceWave()
        {
            if (waveOnHold == -1)
            {
                waveOnHold = WaveManager.Instance.GetCurrentWaveIndex();
            }
            else
                waveOnHold = Mathf.Max(1, waveOnHold - 1);

            wave.text = $"Next Wave\n{waveOnHold}";
        }


        public void IncreaseWave()
        {
            // Button that goes to the next wave, if it was already unlocked.
        }

        public void UpdateSpdBonus(float value)
        {
            spdBonusSlider.value = value;
            spdBonus.text = "Spd Bonus\n" + value.ToString("F0") +"%";
            UpdateBonusColor(spdBonus, value);
        }

        public void UpdateDmgBonus(float value)
        {
            dmgBonus.text = "Dmg Bonus\n" + value.ToString("F0") + "%";
            UpdateBonusColor(dmgBonus, value);
        }

        /// <summary>
        /// Colors the bonus text based on the value
        /// </summary>
        /// <param name="element"></param>
        /// <param name="value"></param>
        public void UpdateBonusColor(TextMeshProUGUI element, float value)
        {
            Color startColor = Color.white;
            Color midColor = new Color(1f, 1f, 0.5f);       // Pale yellow
            Color endColor = new Color(1f, 0.5f, 0.5f);      // Pale red
            Color finalColor = new Color(0.5f, 0.5f, 1f);    // Pale electric blue

            if (value <= 50f)
            {
                float t = value / 50f;
                element.color = Color.Lerp(startColor, midColor, t);
            }
            else if (value <= 100f)
            {
                float t = (value - 50f) / 50f;
                element.color = Color.Lerp(midColor, endColor, t);
            }
            else if (value <= 200f)
            {
                float t = (value - 100f) / 100f;
                element.color = Color.Lerp(endColor, finalColor, t);
            }
            else
            {
                // Clamp at final color if value > 200%
                element.color = finalColor;
            }
        }
    }
}