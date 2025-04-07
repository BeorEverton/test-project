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

            waveOnHold = -1;
        }

        private void Start()
        {
            EnemySpawner.Instance.OnWaveCreated += OnWaveCreated;
            EnemySpawner.Instance.OnEnemyDeath += OnEnemyDeath;
            EnemySpawner.Instance.OnWaveCompleted += OnWaveCompleted;
            WaveManager.Instance.OnWaveStarted += OnWaveStarted;
        }

        private void OnEnemyDeath(object sender, EventArgs _)
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
            spdBonus.text = "Spd +\n" + value.ToString("F0") + "%";

            //UpdateBonusColor(spdBonus, value);
        }

        public void UpdateDmgBonus(float value)
        {
            dmgBonus.text = "Dmg +\n" + value.ToString("F0") + "%";
            UpdateBonusColor(dmgBonus, value);
        }

        /// <summary>
        /// Colors the bonus text based on the value
        /// </summary>
        public void UpdateBonusColor(TextMeshProUGUI element, float value)
        {
            float t = Mathf.Clamp01(value / 200f);
            element.color = Color.Lerp(Color.black, Color.red, t);
        }

        /*public void UpdateBonusColor(TextMeshProUGUI element, float value)
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
                element.color = finalColor;
            }
        }*/


        public void UpdateMoney(ulong value)
        {
            money.SetText(AbbreviateNumber(value));
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

            if (number >= Duodecillion)
            {
                return (number / Duodecillion).ToString("0.#") + "D";
            }
            else if (number >= Undecillion)
            {
                return (number / Undecillion).ToString("0.#") + "U";
            }
            else if (number >= Decillion)
            {
                return (number / Decillion).ToString("0.#") + "d";
            }
            else if (number >= Nonillion)
            {
                return (number / Nonillion).ToString("0.#") + "N";
            }
            else if (number >= Octillion)
            {
                return (number / Octillion).ToString("0.#") + "O";
            }
            else if (number >= Septillion)
            {
                return (number / Septillion).ToString("0.#") + "S";
            }
            else if (number >= Sextillion)
            {
                return (number / Sextillion).ToString("0.#") + "s";
            }
            else if (number >= Quintillion)
            {
                return (number / Quintillion).ToString("0.#") + "Q";
            }
            else if (number >= Quadrillion)
            {
                return (number / Quadrillion).ToString("0.#") + "q";
            }
            else if (number >= Trillion)
            {
                return (number / Trillion).ToString("0.#") + "T";
            }
            else if (number >= Billion)
            {
                return (number / Billion).ToString("0.#") + "B";
            }
            else if (number >= Million)
            {
                return (number / Million).ToString("0.#") + "M";
            }
            else if (number >= Thousand)
            {
                return (number / Thousand).ToString("0.#") + "K";
            }
            else if (showPercent)
            {
                return number.ToString("F2");
            }
            else
                return number.ToString("F0");
        }
    }
}