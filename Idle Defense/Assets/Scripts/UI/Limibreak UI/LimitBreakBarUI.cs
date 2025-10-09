using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI
{
    // Lightweight view for a single concurrent Limit Break
    public class LimitBreakBarUI : MonoBehaviour
    {
        [Header("Wiring")]
        [SerializeField] private Slider _slider;
        [SerializeField] private Image _durationFill;          // optional radial or bar overlay
        [SerializeField] private TextMeshProUGUI _timerText;
        [SerializeField] private TextMeshProUGUI _labelText;    // e.g., "Dmg +", "Spd +", or skill name
        [SerializeField] private Image _icon;

        // Session identity from the manager so UIManager can route updates
        public string SessionId { get; private set; }

        private float _maxCap;

        public void Init(string sessionId, string displayName, Sprite icon, float maxCap, float baselinePct)
        {
            SessionId = sessionId;
            _maxCap = Mathf.Max(0f, maxCap);

            if (_slider != null)
            {
                _slider.minValue = 0f;
                _slider.maxValue = _maxCap;
                _slider.value = Mathf.Clamp(baselinePct, 0f, _maxCap);
                _slider.gameObject.SetActive(true);
            }

            if (_icon != null) _icon.sprite = icon;
            if (_labelText != null) _labelText.text = displayName; // keep it simple; UIManager may override

            // Clear timer at start; UIManager will feed ticks
            if (_timerText != null) _timerText.text = string.Empty;
            if (_durationFill != null)
            {
                _durationFill.fillAmount = 1f; // start as full
                _durationFill.gameObject.SetActive(true);
            }
        }

        public void UpdateValue(float rawPercent)
        {
            if (_slider == null) return;
            _slider.value = Mathf.Clamp(rawPercent, 0f, _maxCap);
        }

        public void UpdateTimer(float remaining, float total)
        {
            if (_durationFill != null && total > 0f)
                _durationFill.fillAmount = Mathf.Clamp01(remaining / total);

            if (_timerText != null)
                _timerText.text = UIManager.FormatTime(TimeSpan.FromSeconds(Mathf.Max(0f, remaining)));
        }

        public void UpdateLabelNumeric(string prefix, float rawPercent)
        {
            // Example: "Dmg + 135%"
            if (_labelText == null) return;
            _labelText.text = $"{prefix} {rawPercent:F0}%";
        }
    }
}
