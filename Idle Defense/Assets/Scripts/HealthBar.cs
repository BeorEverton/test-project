using Assets.Scripts.Enemies;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts
{
    public class HealthBar : MonoBehaviour
    {
        [SerializeField] private Slider _slider;

        private Enemy _enemy;

        private void Awake()
        {
            _enemy = GetComponentInParent<Enemy>();
            _enemy.OnMaxHealthChanged += UpdateMaxHealth;
            _enemy.OnCurrentHealthChanged += UpdateCurrentHealthHealth;
        }

        private void OnEnable()
        {
            _slider.value = _slider.maxValue;
            _slider.gameObject.SetActive(false);
        }

        private void UpdateMaxHealth(object sender, EventArgs e)
        {
            _slider.maxValue = _enemy.MaxHealth;
        }

        private void UpdateCurrentHealthHealth(object sender, EventArgs e)
        {
            if (_slider.gameObject.activeInHierarchy == false)
            {
                _slider.gameObject.SetActive(true);
            }
            _slider.value = _enemy.CurrentHealth;
        }
    }
}