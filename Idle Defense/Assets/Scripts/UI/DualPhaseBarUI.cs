using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI
{
    /// <summary>
    /// Generic dual-phase bar (back then front) for Health / EXP / Limit Break.
    /// - Call SetMax(...) and SetValue(...), with float or ulong.
    /// - Back slider animates to the new value first; when done, front slider follows.
    /// - Optional normalized mode keeps slider range [0..1] to avoid float precision issues for huge values.
    /// </summary>
    public class DualPhaseBarUI : MonoBehaviour
    {
        public enum SliderMode { Direct, Normalized }

        [Header("UI")]
        [SerializeField] private Slider frontSlider; // updates second
        [SerializeField] private Slider backSlider;  // updates first
        [SerializeField] private TextMeshProUGUI valueText; // optional "current/max" or "%"

        [Header("Behavior")]
        [SerializeField] private SliderMode sliderMode = SliderMode.Direct;
        [SerializeField] private float backLerpDuration = 0.40f;
        [SerializeField] private float frontLerpDuration = 0.20f;
        [SerializeField] private float delayBetweenPhases = 0.00f; // small pause after back finishes

        [Header("Display")]
        [SerializeField] private bool showAsPercent = false;     // if true → "87.5%"
        [SerializeField] private int percentDecimals = 1;        // 1 → "87.5%"
        [SerializeField] private bool abbreviateNumbers = true;  // uses UIManager.AbbreviateNumber if present

        // Raw values kept in double to support very large (ulong) inputs.
        private double _rawMax = 1d;
        private double _rawCurrent = 1d;     // last committed current
        private double _rawTarget = 1d;      // newest target after SetValue

        // Phase state
        private enum Phase { Idle, BackLerp, Delay, FrontLerp }
        private Phase _phase = Phase.Idle;

        private float _backT;       // 0..1
        private float _frontT;      // 0..1
        private float _delayT;      // 0..delayBetweenPhases

        private float _backStart;   // slider-space
        private float _backEnd;
        private float _frontStart;  // slider-space
        private float _frontEnd;

        private void Awake()
        {
            InitSliders();
            ApplyImmediate(_rawCurrent); // initialize both sliders
        }

        /* ========================= Public API ========================= */

        // Float overloads
        public void SetMax(float max) => SetMaxInternal(max);
        public void SetValue(float val) => SetValueInternal(val);

        // ULong overloads
        public void SetMax(ulong max) => SetMaxInternal((double)max);
        public void SetValue(ulong val) => SetValueInternal((double)val);

        public void Set(float current, float max)
        {
            SetMaxInternal(max);
            SetValueInternal(current);
        }

        public void Set(ulong current, ulong max)
        {
            SetMaxInternal((double)max);
            SetValueInternal((double)current);
        }

        /* ========================= Core Logic ========================= */

        private void SetMaxInternal(double max)
        {
            _rawMax = Mathf.Max(1f, (float)max); // clamp at least 1
            InitSliders();                       // update slider ranges if needed

            // Clamp current/target to new max
            _rawCurrent = System.Math.Min(_rawCurrent, _rawMax);
            _rawTarget = System.Math.Min(_rawTarget, _rawMax);

            // Snap both bars (caller can SetValue again if they want an animation from the old)
            ApplyImmediate(_rawCurrent);
            UpdateLabel(_rawCurrent, _rawMax);
        }

        private void SetValueInternal(double value)
        {
            _rawTarget = System.Math.Max(0d, System.Math.Min(value, _rawMax));

            // Phase 1: animate BACK to target from its current position.
            _backStart = GetSliderValue(backSlider);
            _backEnd = MapToSlider(_rawTarget);
            _backT = 0f;

            // Phase 2 prepared: front will animate afterwards.
            _frontStart = GetSliderValue(frontSlider);
            _frontEnd = _backEnd; // same destination
            _frontT = 0f;

            _delayT = 0f;
            _phase = Phase.BackLerp;
        }

        private void Update()
        {
            switch (_phase)
            {
                case Phase.BackLerp:
                    if (Animate(backSlider, ref _backT, backLerpDuration, _backStart, _backEnd))
                    {
                        _phase = delayBetweenPhases > 0f ? Phase.Delay : Phase.FrontLerp;
                        _delayT = 0f;
                    }
                    break;

                case Phase.Delay:
                    _delayT += Time.deltaTime;
                    if (_delayT >= delayBetweenPhases)
                        _phase = Phase.FrontLerp;
                    break;

                case Phase.FrontLerp:
                    if (Animate(frontSlider, ref _frontT, frontLerpDuration, _frontStart, _frontEnd))
                    {
                        // Commit new current when front reaches target
                        _rawCurrent = _rawTarget;
                        UpdateLabel(_rawCurrent, _rawMax);
                        _phase = Phase.Idle;
                    }
                    break;

                case Phase.Idle:
                default:
                    break;
            }
        }

        /* ========================= Helpers ========================= */

        private void InitSliders()
        {
            if (frontSlider == null || backSlider == null) return;

            if (sliderMode == SliderMode.Direct)
            {
                frontSlider.minValue = 0f;
                backSlider.minValue = 0f;
                frontSlider.maxValue = (float)_rawMax;
                backSlider.maxValue = (float)_rawMax;
            }
            else // Normalized
            {
                frontSlider.minValue = 0f;
                backSlider.minValue = 0f;
                frontSlider.maxValue = 1f;
                backSlider.maxValue = 1f;
            }
        }

        private void ApplyImmediate(double raw)
        {
            float v = MapToSlider(raw);
            SetSliderValue(backSlider, v);
            SetSliderValue(frontSlider, v);
            UpdateLabel(raw, _rawMax);
            _phase = Phase.Idle;
        }

        private bool Animate(Slider s, ref float t, float duration, float from, float to)
        {
            if (duration <= 0f)
            {
                SetSliderValue(s, to);
                return true;
            }

            t += Time.deltaTime / duration;
            float v = Mathf.Lerp(from, to, Mathf.Clamp01(t));
            SetSliderValue(s, v);
            return t >= 1f || Mathf.Approximately(v, to);
        }

        private float MapToSlider(double raw)
        {
            if (sliderMode == SliderMode.Direct)
                return (float)raw;

            // Normalized 0..1 to retain precision for huge values
            if (_rawMax <= 0d) return 0f;
            return (float)(raw / _rawMax);
        }

        private void SetSliderValue(Slider s, float v)
        {
            if (s != null) s.value = v;
        }

        private float GetSliderValue(Slider s)
        {
            return s != null ? s.value : 0f;
        }

        private void UpdateLabel(double current, double max)
        {
            if (valueText == null) return;

            if (showAsPercent)
            {
                double pct = max <= 0d ? 0d : (current / max) * 100d;
                valueText.SetText(pct.ToString("F" + Mathf.Clamp(percentDecimals, 0, 3)) + "%");
            }
            else
            {
                // Use your existing abbreviator if desired
                if (abbreviateNumbers && typeof(UIManager).GetMethod("AbbreviateNumber") != null)
                {
                    string cur = UIManager.AbbreviateNumber((float)current);
                    string mx = UIManager.AbbreviateNumber((float)max);
                    valueText.SetText(cur + "/" + mx);
                }
                else
                {
                    valueText.SetText(((float)current).ToString("0.##") + "/" + ((float)max).ToString("0.##"));
                }
            }
        }
    }
}
