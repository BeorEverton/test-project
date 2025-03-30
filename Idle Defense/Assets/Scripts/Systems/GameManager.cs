using Assets.Scripts.UI;
using Assets.Scripts.WaveSystem;
using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Assets.Scripts.Systems
{
    public class GameManager : MonoBehaviour
    {
        public float dmgBonus { get; private set; }
        public float spdBonus { get; private set; }

        private const float maxSpdBonus = 200f;
        private const float increaseRate = maxSpdBonus / 10f; // Increase to 200 in 10 seconds
        private const float decreaseRate = maxSpdBonus / 30f; // Decrease to 0 in 30 seconds
        private float _spdBonusDecreaseTimer = 0f;
        private bool _spdBonusDecreasing;
        private const float _spdBonusDecreaseDelay = 2f;

        public static GameManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        private void Start()
        {
            EnemySpawner.Instance.OnWaveCompleted += OnWaveCompleted;
        }

        private void Update()
        {
            if (!EventSystem.current.IsPointerOverGameObject())
                AdjustSpdBonus();
        }

        private void OnWaveCompleted(object sender, EventArgs e)
        {
            dmgBonus++;
            UIManager.Instance.UpdateDmgBonus(dmgBonus);
        }

        private void OnWaveFailed(object sender, EventArgs e)
        {
            dmgBonus = 0;
        }

        private void AdjustSpdBonus()
        {
            if (Input.GetMouseButton(0))
            {
                if (_spdBonusDecreasing)
                {
                    _spdBonusDecreasing = false;
                    _spdBonusDecreaseTimer = 0f;
                }

                IncreaseSpdBonus();
            }
            else
            {
                if (!_spdBonusDecreasing)
                {
                    CountdownSpdBonusDecrease();
                }
                else
                {
                    DecreaseSpdBonus();
                }
            }

            spdBonus = Mathf.Clamp(spdBonus, 0, maxSpdBonus);

            UIManager.Instance.UpdateSpdBonus(spdBonus);
        }

        private void IncreaseSpdBonus()
        {
            spdBonus += increaseRate * Time.deltaTime;
        }

        private void DecreaseSpdBonus()
        {
            spdBonus -= decreaseRate * Time.deltaTime;
        }

        private void CountdownSpdBonusDecrease()
        {
            _spdBonusDecreaseTimer += Time.deltaTime;
            if (_spdBonusDecreaseTimer >= _spdBonusDecreaseDelay)
                _spdBonusDecreasing = true;
        }
    }
}
