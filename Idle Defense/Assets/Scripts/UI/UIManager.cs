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
            WaveManager.Instance.OnWaveStarted += OnWaveStarted;
        }

        private void OnEnemyDeath(object sender, EventArgs e)
        {
            _enemyCount--;
            enemies.text = $"Enemies: {_enemyCount}";
        }

        private void OnWaveStarted(object sender, WaveManager.OnWaveStartedEventArgs args)
        {
            wave.text = $"Wave: {args.WaveNumber}";

        }

        private void OnWaveCreated(object sender, EnemySpawner.OnWaveCreatedEventArgs args)
        {
            _enemyCount = args.EnemyCount;
            enemies.text = $"Enemies: {_enemyCount}";
        }

        public void ReduceWave()
        {
            // Button that goes back to the previous wave
            // Must make sure all enemies are killed before reducing the wave
            // WaveManager.Instance.ReduceWave();

        }

        public void IncreaseWave()
        {
            // Button that goes to the next wave, if it was already unlocked.
        }

        public void UpdateSpdBonus(float value)
        {
            spdBonusSlider.value = value;
            spdBonus.text = "Spd Bonus\n" + value.ToString("F0") +"%";
        }
    }
}