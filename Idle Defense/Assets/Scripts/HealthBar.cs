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
            _enemy.OnCurrentChanged += UpdateCurrentHealth;
        }

        private void OnEnable()
        {
            _slider.value = _slider.maxValue;
        }

        private void UpdateMaxHealth(object sender, EventArgs e)
        {
            _slider.maxValue = _enemy.MaxHealth;
        }

        private void UpdateCurrentHealth(object sender, EventArgs e)
        {
            _slider.value = _enemy.CurrentHealth;
        }
    }
}
