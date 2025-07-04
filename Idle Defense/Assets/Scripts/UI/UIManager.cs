using Assets.Scripts.Systems;
using Assets.Scripts.Systems.Save;
using Assets.Scripts.WaveSystem;
using CrazyGames;
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
        [SerializeField] private Image _decreaseDelayFill;
        private Coroutine _delayFillRoutine;

        // Equip management
        [SerializeField] private GameObject equipPanel;
        [SerializeField] private GameObject unequipPanel;
        [SerializeField] private TextMeshProUGUI toast;
        [SerializeField] private GameObject[] rightPanels;
        public GameObject wallUpgradePanel;

        [Tooltip("Death Screen")]
        [SerializeField] private GameObject deathCountdownPanel, startGamePanel;
        [SerializeField] private TextMeshProUGUI countdownText;
        [SerializeField] private Button immediateRestartButton;
        [SerializeField] private Button restartSameWaveButton;
        private Coroutine deathRoutine;
        private int rollbackWaveIndex;
        public float timeSpeedOnDeath;
        public int amountOfWavesToRollBack;

        private int activeSlot; // for the equipment

        private int _enemyCount;

        private float timeScaleOnPause;
        public bool gamePaused = false;

        private bool stopOnDeath = false; // used to pause the game on death and resume with standard speed

        [Tooltip("Boss Reward")]
        [SerializeField] private GameObject bossRewardPanel;
        [SerializeField] private TextMeshProUGUI bossRewardText;
        [SerializeField] private TextMeshProUGUI multiplyBossRewardText;
        [SerializeField] private Slider bossRewardTimer;
        private double bossReward;

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
            if (CrazySDK.IsInitialized)
                CrazySDK.Game.GameplayStart();
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

        public void StartDelayFill(float delayDuration)
        {
            if (_delayFillRoutine != null)
                StopCoroutine(_delayFillRoutine);

            _decreaseDelayFill.fillAmount = 1f;
            _decreaseDelayFill.gameObject.SetActive(true);

            _delayFillRoutine = StartCoroutine(DelayFillRoutine(delayDuration));
        }

        private IEnumerator DelayFillRoutine(float duration)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                _decreaseDelayFill.fillAmount = 1f - (t / duration);
                yield return null;
            }

            _decreaseDelayFill.fillAmount = 0f;
            _decreaseDelayFill.gameObject.SetActive(false);
            _delayFillRoutine = null;
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
            _money.SetText("⚙" + AbbreviateNumber(value));
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

            PauseGame(true); // Pause the game

            deathCountdownPanel.SetActive(true);

            // Calculate and store rollback wave
            int currentWave = WaveManager.Instance.GetCurrentWaveIndex();
            rollbackWaveIndex = Mathf.Max(1, currentWave - amountOfWavesToRollBack); // clamp to wave 1

            countdownText.text = $"Restarting from Zone {rollbackWaveIndex} in {Mathf.CeilToInt(seconds)}...";

            immediateRestartButton.onClick.RemoveAllListeners();
            immediateRestartButton.onClick.AddListener(SkipDeathCountdown);

            if (CrazySDK.IsInitialized && CrazySDK.Ad.AdblockStatus == AdblockStatus.Missing
                || CrazySDK.Ad.AdblockStatus == AdblockStatus.Detecting)
            {
                restartSameWaveButton.gameObject.SetActive(true);
            }
            else
            {
                restartSameWaveButton.gameObject.SetActive(false);
            }

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
            PauseGame(false); // Resume game at previous speed
        }

        public void RestartCurrentWave()
        {
            if (deathRoutine != null)
                StopCoroutine(deathRoutine);

            PauseGame(true);
            if (CrazySDK.IsInitialized)
            {
                CrazySDK.Ad.RequestAd(CrazyAdType.Rewarded,
                        () => {
                            
                        }, // onStart
                        (error) => {                            
                        }, // onError
                        () => // Success
                        {
                            PauseGame(false);
                        });
            }
            rollbackWaveIndex = WaveManager.Instance.GetCurrentWaveIndex();
            deathCountdownPanel.SetActive(false);

            // Set to current wave minus one, since LoadWave will increment to it
            PauseGame(false);
            WaveManager.Instance.LoadWave(rollbackWaveIndex);
            WaveManager.Instance.ForceRestartWave();
            PlayerBaseManager.Instance.InitializeGame(true);
        }

        public void StopOnDeath()
        {
            GameSpeedManager.Instance.ResetGameSpeed(); // Reset game speed to default
            PauseGame(true); // Pause the game
            stopOnDeath = true; // Set flag to pause on death
            startGamePanel.SetActive(true);
            rollbackWaveIndex = 1; // Reset rollback wave index
            GameManager.Instance.ChangeGameState(GameState.Management); // Change game state to GameOver
        }

        public void ClickStart()
        {
            startGamePanel.SetActive(false);
            WaveManager.Instance.LoadWave(1); // Load wave 1
            WaveManager.Instance.ForceRestartWave();
            PlayerBaseManager.Instance.InitializeGame(true); // Reset player base stats
            if (!stopOnDeath)
                PauseGame(false); // Resume game at previous speed
            else
                Time.timeScale = 1f; // Resume game at normal speed if stopOnDeath is true
            GameManager.Instance.ChangeGameState(GameState.InGame); // Change game state to regular
        }
        #endregion

        public void PauseGame(bool pause)
        {
            if (pause)
            {
                if (CrazySDK.IsInitialized)
                    CrazySDK.Game.GameplayStop();
                // Save game state or perform any necessary actions on pause
                gamePaused = true;
                SaveGameManager.Instance.SaveGame();
                if (Time.timeScale == 0f)
                    return; // Already paused
                timeScaleOnPause = Time.timeScale; // Store current time scale
                Time.timeScale = 0f; // Pause the game                
            }
            else
            {
                Time.timeScale = timeScaleOnPause; // Resume the game
                gamePaused = false;
                if (CrazySDK.IsInitialized)
                    CrazySDK.Game.GameplayStart();
            }
        }

        public void BossRewardPanel(double coins)
        {
            if (CrazySDK.IsInitialized && CrazySDK.Ad.AdblockStatus == AdblockStatus.Missing)
            {
                StartCoroutine(DisableBossPanel(coins));
            }
            else
            {
                bossRewardPanel.SetActive(false);
            }
        }

        // Disable Boss panel, show slider going down for the time remaining
        IEnumerator DisableBossPanel(double coins)
        {
            yield return new WaitForSeconds(.5f);
            bossReward = coins * 2;
            float duration = 10f;
            bossRewardPanel.SetActive(true);
            bossRewardText.text = $"You earned {AbbreviateNumber(coins)} ⚙!";
            multiplyBossRewardText.text = "Get more " + AbbreviateNumber(bossReward) + " ⚙!";     
            bossRewardTimer.maxValue = duration;
            bossRewardTimer.value = duration;
            for (float t = duration; t >= 0; t -= Time.unscaledDeltaTime)
            {
                bossRewardTimer.value = t;
                yield return null;
            }
            bossRewardPanel.SetActive(false);            
        }

        public void GetBossReward()
        {
            if (CrazySDK.IsInitialized && CrazySDK.Ad.AdblockStatus == AdblockStatus.Missing)
            {
                CrazySDK.Ad.RequestAd(CrazyAdType.Rewarded,
                () => { }, // onStart
                (error) => { }, // onError
                () => // Success
                {
                    GameManager.Instance.AddMoney((ulong)(bossReward));
                    bossRewardPanel.SetActive(false);
                });
            }
        }

    }
}