using Assets.Scripts.Systems;      // Currency, GameManager
using TMPro;
using UnityEngine;

namespace Assets.Scripts.UI
{
    public class CurrencyDisplayUI : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private Currency currency = Currency.Scraps;

        [Header("UI Refs")]
        [SerializeField] private TextMeshProUGUI currencyText; // shows icon (⚙ / § / Ø)

        // Cache last shown value to avoid redundant UI updates
        private ulong _lastValue = ulong.MaxValue;
        private bool _listening;

        private string icon;

        private void Awake()
        {
            // Auto-find if not wired in the inspector (optional, keeps it plug-and-play)
            if (currencyText == null)
                currencyText = transform.Find("Currency DisplayText")?.GetComponent<TextMeshProUGUI>();
        }

        private void Start()
        {
            TrySubscribe();
            RefreshNow(); // initial value
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void OnEnable()
        {
            TrySubscribe();
            RefreshNow();
        }

        private void TrySubscribe()
        {
            if (_listening) return;
            var gm = GameManager.Instance;
            if (gm == null) return;

            gm.OnCurrencyChanged += HandleCurrencyChanged;
            _listening = true;
        }

        private void Unsubscribe()
        {
            if (!_listening) return;
            var gm = GameManager.Instance;
            if (gm != null)
                gm.OnCurrencyChanged -= HandleCurrencyChanged;
            _listening = false;
        }

        private void HandleCurrencyChanged(Currency changed, ulong amount)
        {
            if (changed != currency) return;
            UpdateValue(amount);
        }

        public void RefreshNow()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            UpdateValue(gm.GetCurrency(currency));
        }

        private void UpdateValue(ulong amount)
        {
            if (amount == _lastValue) return; // no-op if unchanged
            _lastValue = amount;

            if (string.IsNullOrEmpty(icon))
                icon = UIManager.GetCurrencyIcon(currency);

            // Use the same abbreviation style as the rest of the UI
            currencyText.SetText(icon + " " + UIManager.AbbreviateNumber(amount));
        }
    }
}
