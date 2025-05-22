using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI
{
    public class MultipleBuyOption : MonoBehaviour
    {
        public static MultipleBuyOption Instance { get; private set; }

        [SerializeField] private Button _buyAmountToggleButton;
        [SerializeField] private TextMeshProUGUI _amountLabel;

        private List<int> _amountOptions;
        private int _currentIndex = 0;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        public int GetBuyAmount() => _amountOptions[_currentIndex];

        private void Start()
        {
            _amountOptions = new List<int> { 1, 5, 10, 25, 50, 9999 };

            _buyAmountToggleButton.onClick.AddListener(AdvanceBuyAmount);
        }

        private void AdvanceBuyAmount()
        {
            _currentIndex = (_currentIndex + 1) % _amountOptions.Count;
            SetBuyAmount(_amountOptions[_currentIndex]);
        }

        private void SetBuyAmount(int amount)
        {
            UpdateLabel(amount);
        }

        private void UpdateLabel(int amount)
        {
            if (amount == 9999)
            {
                _amountLabel.SetText("MAX");
                return;
            }
            _amountLabel.SetText(amount.ToString());
        }
    }
}
