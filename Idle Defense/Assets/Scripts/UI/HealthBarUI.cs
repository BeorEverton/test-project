using UnityEngine;
using UnityEngine.UI;
using Assets.Scripts.Systems;
using TMPro;

namespace Assets.Scripts.UI
{
    public class HealthBarUI : MonoBehaviour
    {
        [SerializeField] private Slider frontSlider; // instant value
        [SerializeField] private Slider backSlider;  // delayed value
        [SerializeField] private TextMeshProUGUI healthText;

        [SerializeField] private float lerpDuration = 0.4f; // time to fully catch up
        private float lerpProgress = 0f;
        private float previousBackValue;
        private float targetValue;

        private bool isLerping;
        private float lastKnownMax = -1;


        private void Start()
        {
            PlayerBaseManager.Instance.OnHealthChanged += OnHealthChanged;
            SetMaxHealth(PlayerBaseManager.Instance.MaxHealth);
            SetHealth(PlayerBaseManager.Instance.CurrentHealth);
        }

        private void OnDestroy()
        {
            if (PlayerBaseManager.Instance != null)
                PlayerBaseManager.Instance.OnHealthChanged -= OnHealthChanged;
        }

        private void OnHealthChanged(float current, float max)
        {
            if (!Mathf.Approximately(max, lastKnownMax))
            {
                SetMaxHealth(max);
                lastKnownMax = max;
            }

            SetHealth(current);
            healthText.SetText(UIManager.AbbreviateNumber(current) + "/" + UIManager.AbbreviateNumber(max));
        }


        public void SetMaxHealth(float max)
        {
            frontSlider.maxValue = max;
            backSlider.maxValue = max;

            frontSlider.value = max;
            backSlider.value = max;

            targetValue = max;
            previousBackValue = max;
            isLerping = false;
        }

        public void SetHealth(float current)
        {
            frontSlider.value = current;

            if (!isLerping)
            {
                previousBackValue = backSlider.value;
                lerpProgress = 0f;
                isLerping = true;
            }
            else
            {
                // Restart the lerp from current backSlider position to new target
                previousBackValue = backSlider.value;
                lerpProgress = 0f;
            }

            targetValue = current;
        }



        private void Update()
        {
            if (!isLerping) return;

            lerpProgress += Time.deltaTime / lerpDuration;
            backSlider.value = Mathf.Lerp(previousBackValue, targetValue, lerpProgress);

            if (Mathf.Approximately(backSlider.value, targetValue) || lerpProgress >= 1f)
            {
                backSlider.value = targetValue;
                isLerping = false;
            }
        }

    }
}
