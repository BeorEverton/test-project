using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Assets.Scripts.Systems
{
    public class GameSpeedManager : MonoBehaviour
    {
        [SerializeField] private Button speedToggleButton;
        [SerializeField] private TextMeshProUGUI speedLabel; // Or use TMP_Text if needed

        private List<float> speedOptions;
        private int currentSpeedIndex = 0;

        private void Start()
        {
            // Define speed steps based on platform
#if UNITY_EDITOR
            speedOptions = new List<float> { 1f, 1.5f, 2f, 3f, 5f, 10f };
#else
            speedOptions = new List<float> { 1f, 1.5f, 2f };
#endif

            speedToggleButton.onClick.AddListener(AdvanceGameSpeed);
            SetGameSpeed(speedOptions[currentSpeedIndex]);
        }

        private void AdvanceGameSpeed()
        {
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
            speedLabel.SetText(speed +"x");
        }
    }
}
