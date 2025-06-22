using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
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

        // Drag to increase variables
        private Vector2 _startDragPos;
        private bool _isDragging = false;
        private float _dragThreshold = 100f; // pixels needed to trigger a change
        private float _accumulatedDrag = 0f;


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
            if (_isDragging) return;
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

        public void OnPointerDown()
        {
            
            _startDragPos = Input.mousePosition;
            _isDragging = true;
            _accumulatedDrag = 0f;
        }

        public void OnDrag()
        {
            
            if (!_isDragging) return;
            

            float deltaX = Input.mousePosition.x - _startDragPos.x;
            _accumulatedDrag += deltaX;

            if (_accumulatedDrag > _dragThreshold)
            {
                ChangeBuyAmount(1);
                _accumulatedDrag = 0f;
            }
            else if (_accumulatedDrag < -_dragThreshold)
            {
                ChangeBuyAmount(-1);
                _accumulatedDrag = 0f;
            }
        }

        public void OnPointerUp()
        {
            
            _isDragging = false;
            _accumulatedDrag = 0f;
        }

        private void ChangeBuyAmount(int direction)
        {
            _currentIndex = Mathf.Clamp(_currentIndex + direction, 0, _amountOptions.Count - 1);
            SetBuyAmount(_amountOptions[_currentIndex]);
            OnBuyAmountChanged?.Invoke(this, EventArgs.Empty);
        }


    }
}