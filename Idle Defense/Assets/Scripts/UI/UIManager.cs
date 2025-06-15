using Assets.Scripts.Systems;
using Assets.Scripts.WaveSystem;
using System;
using System.Collections;
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

        // Equip management
        [SerializeField] private GameObject equipPanel;   // drag a panel root in Canvas
        [SerializeField] private GameObject unequipPanel; // another panel if you like
        [SerializeField] private TextMeshProUGUI toast;   // optional 1-line overlay
        [SerializeField] private GameObject[] rightPanels;
        public GameObject wallUpgradePanel;

        [Tooltip("Death Screen")]
        [SerializeField] private GameObject deathCountdownPanel;
        [SerializeField] private TextMeshProUGUI countdownText;
        [SerializeField] private Button immediateRestartButton;
        private Coroutine deathRoutine;
        private int rollbackWaveIndex;
        public float timeSpeedOnDeath;


        private int activeSlot; // for the equipment

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
            _enemies.text = $"Enemies\n{_enemyCount}";
        }

        private void OnWaveStarted(object sender, WaveManager.OnWaveStartedEventArgs args)
        {
            _wave.text = $"Zone\n{args.WaveNumber}";
        }

        private void OnWaveCreated(object sender, EnemySpawner.OnWaveCreatedEventArgs args)
        {
            _enemyCount = args.EnemyCount;
            _enemies.text = $"Enemies\n{_enemyCount}";
        }

        public void UpdateSpdBonus(float value)
        {
            _spdBonusSlider.value = value;
            _spdBonus.text = "Spd + " + value.ToString("F0") + "%";

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

        public static string FormatTime(TimeSpan time)
        {
            if (time.TotalSeconds < 60)
            {
                return $"{(int)time.TotalSeconds} sec";
            }
            else if (time.TotalMinutes < 60)
            {
                return $"{time.Minutes:D2}:{time.Seconds:D2}";
            }
            else if (time.TotalHours < 24)
            {
                return $"{(int)time.TotalHours:D2}:{time.Minutes:D2}:{time.Seconds:D2}";
            }
            else
            {
                return $"{(int)time.TotalDays}d:{time.Hours:D2}:{time.Minutes:D2}:{time.Seconds:D2}";
            }
        }


        public void OpenEquipPanel(int slot)
        {
            DeactivateRightPanels();
            activeSlot = slot;
            equipPanel.SetActive(true);
            equipPanel.GetComponent<EquipPanelUI>().Open(slot);
        }

        public void ShowToast(string msg, float time = 1.5f)
        {
            if (toast == null)
            { Debug.Log(msg); return; }  // no text object assigned
            StopAllCoroutines();                            // stop any previous toast
            StartCoroutine(ToastRoutine(msg, time));
        }

        private IEnumerator ToastRoutine(string m, float t)
        {
            toast.text = m;                // set message
            toast.gameObject.SetActive(true);
            yield return new WaitForSeconds(t);
            toast.gameObject.SetActive(false); // hide after delay
        }

        public void DeactivateRightPanels()
        {
            foreach (GameObject panel in rightPanels)
            {
                panel.SetActive(false);
            }
        }

        #region Death Methods
        public void ShowDeathCountdown(float seconds = 5f)
        {
            if (deathRoutine != null)
                StopCoroutine(deathRoutine);

            timeSpeedOnDeath = Time.timeScale; // Store current time scale
            Time.timeScale = 0f; // Pause the game

            deathCountdownPanel.SetActive(true);

            // Calculate and store rollback wave
            int currentWave = WaveManager.Instance.GetCurrentWaveIndex();
            rollbackWaveIndex = Mathf.Max(1, currentWave - 2); // clamp to wave 1

            countdownText.text = $"Restarting from Zone {rollbackWaveIndex} in {Mathf.CeilToInt(seconds)}...";
            immediateRestartButton.onClick.RemoveAllListeners();
            immediateRestartButton.onClick.AddListener(SkipDeathCountdown);

            deathRoutine = StartCoroutine(DeathCountdownRoutine(seconds));
        }

        private IEnumerator DeathCountdownRoutine(float seconds)
        {
            float timeLeft = seconds;

            while (timeLeft > 0f)
            {
                countdownText.text = $"Restarting from Zone {rollbackWaveIndex} in {Mathf.CeilToInt(timeLeft)}...";
                yield return new WaitForSecondsRealtime(1f);
                timeLeft -= 1f;
            }

            countdownText.text = $"Restarting now...";
            yield return new WaitForSecondsRealtime(0.5f); // optional buffer
            TriggerGameReset();
        }

        public void SkipDeathCountdown()
        {
            if (deathRoutine != null)
                StopCoroutine(deathRoutine);
            TriggerGameReset();
        }

        private void TriggerGameReset()
        {
            deathCountdownPanel.SetActive(false);
            WaveManager.Instance.LoadWave(rollbackWaveIndex);
            WaveManager.Instance.ForceRestartWave();
            PlayerBaseManager.Instance.InitializeGame(true);
        }

        public void RestartCurrentWave()
        {
            if (deathRoutine != null)
                StopCoroutine(deathRoutine);

            deathCountdownPanel.SetActive(false);

            // Set to current wave minus one, since LoadWave will increment to it
            WaveManager.Instance.LoadWave(rollbackWaveIndex);
            WaveManager.Instance.ForceRestartWave();
            PlayerBaseManager.Instance.InitializeGame(true);
        }
        #endregion
    }
}