using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI
{
    public class MultipleBuyOption : MonoBehaviour
    {
        public static MultipleBuyOption Instance { get; private set; }
        public event EventHandler OnBuyAmountChanged;

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

            _amountOptions = new List<int> { 1, 5, 10, 25, 50, 100 };
        }

        private void Start()
        {
            _buyAmountToggleButton.onClick.AddListener(AdvanceBuyAmount);
            UpdateLabel(_amountOptions[0]);
        }

        public int GetBuyAmount() => _amountOptions[_currentIndex];

        private void AdvanceBuyAmount()
        {
            _currentIndex = (_currentIndex + 1) % _amountOptions.Count;
            SetBuyAmount(_amountOptions[_currentIndex]);
            OnBuyAmountChanged?.Invoke(this, EventArgs.Empty);
        }

        private void SetBuyAmount(int amount)
        {
            UpdateLabel(amount);
        }

        private void UpdateLabel(int amount)
        {
            _amountLabel.SetText("Buy " + amount.ToString());
        }
    }
}