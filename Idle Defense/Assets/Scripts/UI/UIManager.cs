using Assets.Scripts.Systems;
using Assets.Scripts.WaveSystem;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [SerializeField] private TextMeshProUGUI _dmgBonus, _spdBonus, _wave, _enemies, _money;
        [SerializeField] private Slider _spdBonusSlider;

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
            GameManager.Instance.OnMoneyChanged += UpdateMoney;
        }

        private void OnEnemyDeath(object sender, EventArgs _)
        {
            _enemyCount--;
            _enemies.text = $"{_enemyCount}";
        }

        private void OnWaveStarted(object sender, WaveManager.OnWaveStartedEventArgs args)
        {
            _wave.text = $"Wave\n{args.WaveNumber}";
        }

        private void OnWaveCreated(object sender, EnemySpawner.OnWaveCreatedEventArgs args)
        {
            _enemyCount = args.EnemyCount;
            _enemies.text = $"{_enemyCount}";
        }

        public void UpdateSpdBonus(float value)
        {
            _spdBonusSlider.value = value;
            _spdBonus.text = "Spd +\n" + value.ToString("F0") + "%";

            //UpdateBonusColor(_spdBonus, value);
        }

        public void UpdateDmgBonus(float value)
        {
            _dmgBonus.text = "Dmg +\n" + value.ToString("F0") + "%";
            UpdateBonusColor(_dmgBonus, value);
        }

        /// <summary>
        /// Colors the bonus text based on the value
        /// </summary>
        public void UpdateBonusColor(TextMeshProUGUI element, float value)
        {
            float t = Mathf.Clamp01(value / 200f);
            element.color = Color.Lerp(Color.black, Color.red, t);
        }

        public void UpdateMoney(ulong value)
        {
            _money.SetText("$" + AbbreviateNumber(value));
        }

        public static string AbbreviateNumber(double number, bool showPercent = false)
        {
            const double Thousand = 1E3;
            const double Million = 1E6;
            const double Billion = 1E9;
            const double Trillion = 1E12;
            const double Quadrillion = 1E15;
            const double Quintillion = 1E18;
            const double Sextillion = 1E21;
            const double Septillion = 1E24;
            const double Octillion = 1E27;
            const double Nonillion = 1E30;
            const double Decillion = 1E33;
            const double Undecillion = 1E36;
            const double Duodecillion = 1E39;

            return number switch
            {
                >= Duodecillion => (number / Duodecillion).ToString("0.#") + "D",
                >= Undecillion => (number / Undecillion).ToString("0.#") + "U",
                >= Decillion => (number / Decillion).ToString("0.#") + "d",
                >= Nonillion => (number / Nonillion).ToString("0.#") + "N",
                >= Octillion => (number / Octillion).ToString("0.#") + "O",
                >= Septillion => (number / Septillion).ToString("0.#") + "S",
                >= Sextillion => (number / Sextillion).ToString("0.#") + "s",
                >= Quintillion => (number / Quintillion).ToString("0.#") + "Q",
                >= Quadrillion => (number / Quadrillion).ToString("0.#") + "q",
                >= Trillion => (number / Trillion).ToString("0.#") + "T",
                >= Billion => (number / Billion).ToString("0.#") + "B",
                >= Million => (number / Million).ToString("0.#") + "M",
                >= Thousand => (number / Thousand).ToString("0.#") + "K",
                _ => number.ToString(showPercent ? "F2" : "F0")
            };
        }
    }
}