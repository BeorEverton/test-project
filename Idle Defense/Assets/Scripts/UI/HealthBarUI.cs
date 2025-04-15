using Assets.Scripts.Systems;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI
{
    public class HealthBarUI : MonoBehaviour
    {
        [SerializeField] private Slider _frontSlider; // instant value
        [SerializeField] private Slider _backSlider;  // delayed value
        [SerializeField] private TextMeshProUGUI _healthText;

        [SerializeField] private float _lerpDuration = 0.4f; // time to fully catch up
        private float _lerpProgress = 0f;
        private float _previousBackValue;
        private float _targetValue;

        private bool _isLerping;
        private float _lastKnownMax = -1;

        private void Start()
        {
            PlayerBaseManager.Instance.OnHealthChanged += OnHealthChanged;
            SetMaxHealth(PlayerBaseManager.Instance.MaxHealth);
            SetHealth(PlayerBaseManager.Instance.CurrentHealth);
            _healthText.SetText(UIManager.AbbreviateNumber(PlayerBaseManager.Instance.CurrentHealth) + "/" + UIManager.AbbreviateNumber(PlayerBaseManager.Instance.MaxHealth));
        }

        private void OnDestroy()
        {
            if (PlayerBaseManager.Instance != null)
                PlayerBaseManager.Instance.OnHealthChanged -= OnHealthChanged;
        }

        private void OnHealthChanged(float current, float max)
        {
            if (!Mathf.Approximately(max, _lastKnownMax))
            {
                SetMaxHealth(max);
                _lastKnownMax = max;
            }

            SetHealth(current);
            _healthText.SetText(UIManager.AbbreviateNumber(current) + "/" + UIManager.AbbreviateNumber(max));
        }

        public void SetMaxHealth(float max)
        {
            _frontSlider.maxValue = max;
            _backSlider.maxValue = max;

            _frontSlider.value = max;
            _backSlider.value = max;

            _targetValue = max;
            _previousBackValue = max;
            _isLerping = false;
        }

        public void SetHealth(float current)
        {
            _frontSlider.value = current;

            if (!_isLerping)
            {
                _previousBackValue = _backSlider.value;
                _lerpProgress = 0f;
                _isLerping = true;
            }
            else
            {
                // Restart the lerp from current _backSlider position to new target
                _previousBackValue = _backSlider.value;
                _lerpProgress = 0f;
            }

            _targetValue = current;
        }

        private void Update()
        {
            if (!_isLerping)
                return;

            _lerpProgress += Time.deltaTime / _lerpDuration;
            _backSlider.value = Mathf.Lerp(_previousBackValue, _targetValue, _lerpProgress);

            if (Mathf.Approximately(_backSlider.value, _targetValue) || _lerpProgress >= 1f)
            {
                _backSlider.value = _targetValue;
                _isLerping = false;
            }
        }
    }
}