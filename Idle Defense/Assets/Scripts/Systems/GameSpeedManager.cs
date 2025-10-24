using Assets.Scripts.WaveSystem;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


#if UNITY_EDITOR
#endif

namespace Assets.Scripts.Systems
{
    public class GameSpeedManager : MonoBehaviour
    {
        [SerializeField] private Button speedToggleButton;
        [SerializeField] private TextMeshProUGUI speedLabel; // Or use TMP_Text if needed

        private List<float> speedOptions;
        private int currentSpeedIndex = 0;
        public static GameSpeedManager Instance { get; private set; }
        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        private void Start()
        {
            // Define speed steps based on platform
            //#if UNITY_EDITOR
            speedOptions = new List<float> { 1f, 1.5f, 2f, 3f, 5f, 10f };
            //#else
            //speedOptions = new List<float> { 1f, 1.5f, 2f };
            //#endif

            speedToggleButton.onClick.AddListener(AdvanceGameSpeed);
            WaveManager.Instance.OnWaveStarted += OnWaveStarted;

        }

        private void AdvanceGameSpeed()
        {
            if (Time.timeScale == 0) return; // Prevent changing speed when game is paused
            currentSpeedIndex = (currentSpeedIndex + 1) % speedOptions.Count;
            SetGameSpeed(speedOptions[currentSpeedIndex]);
        }

        private void SetGameSpeed(float speed)
        {
            Time.timeScale = speed;
            UpdateLabel(speed);
        }

        private void UpdateLabel(float speed)
        {
            speedLabel.SetText($"Speed\n{speed}x");
        }

        public void UnlockSpeed(float newSpeed)
        {
            if (!speedOptions.Contains(newSpeed))
            {
                speedOptions.Add(newSpeed);
                speedOptions.Sort(); // Optional: keep order clean
            }
        }

        private void OnWaveStarted(object sender, WaveManager.OnWaveStartedEventArgs e)
        {
            int wave = e.WaveNumber;

            if (wave >= 30) UnlockSpeed(2.5f);
            if (wave >= 50) UnlockSpeed(3f);
            if (wave >= 80) UnlockSpeed(4f);
            if (wave >= 100) UnlockSpeed(5f);
        }

        public void ResetGameSpeed()
        {
            currentSpeedIndex = 0;
            SetGameSpeed(speedOptions[currentSpeedIndex]);
            UpdateLabel(speedOptions[currentSpeedIndex]);
        }
    }
}
