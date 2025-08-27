using Assets.Scripts.Systems;
using Assets.Scripts.Turrets;
using Assets.Scripts.WaveSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI
{
    public class TurretUnlockRequirement : MonoBehaviour
    {
        [SerializeField] private GameObject _lockedOverlay, _turretToUnlock;
        [SerializeField] private Button _unlockButton;              // Button shown once unlockable
        [SerializeField] private TextMeshProUGUI _unlockText;
        [SerializeField] private int _requiredWave = 10;
        [SerializeField] private ulong _unlockCost = 100;

        private bool _isUnlocked;

        private void Start()
        {
            WaveManager.Instance.OnWaveStarted += OnWaveStarted;
            _unlockButton.onClick.AddListener(UnlockTurret);

            _isUnlocked = _turretToUnlock.GetComponent<BaseTurret>().IsUnlocked();
            if (!_isUnlocked)
                _turretToUnlock.SetActive(false);

            RefreshState(WaveManager.Instance.GetCurrentWaveIndex());
        }

        private void OnDestroy()
        {
            WaveManager.Instance.OnWaveStarted -= OnWaveStarted;
            _unlockButton.onClick.RemoveListener(UnlockTurret);
        }

        private void OnWaveStarted(object sender, WaveManager.OnWaveStartedEventArgs e)
        {
            RefreshState(e.WaveNumber);
        }

        private void RefreshState(int currentWave)
        {
            if (_isUnlocked)
            {
                _lockedOverlay.SetActive(false);
                _unlockText.gameObject.SetActive(false);
                _unlockButton.gameObject.SetActive(false);
                return;
            }

            if (currentWave >= _requiredWave)
            {
                _unlockButton.gameObject.SetActive(true);
                _unlockText.color = Color.black;
                _lockedOverlay.SetActive(true);
                _unlockText.text = $"Unlock ${UIManager.AbbreviateNumber(_unlockCost)}";
            }
            else
            {
                _lockedOverlay.SetActive(true);
                _unlockButton.gameObject.SetActive(false);
                _unlockText.color = Color.white;
                _unlockText.text = $"Locked\nWave {_requiredWave}";
            }
        }

        private void UnlockTurret()
        {
            if (GameManager.Instance.GetCurrency(Currency.BlackSteel) >= _unlockCost)
            {
                GameManager.Instance.SpendCurrency(Currency.BlackSteel, _unlockCost);
                _isUnlocked = true;
                RefreshState(WaveManager.Instance.GetCurrentWaveIndex());

                _turretToUnlock.SetActive(true);
                _turretToUnlock.GetComponent<BaseTurret>().UnlockTurret();
            }
            else
            {
                // Sound to represent no money
            }
        }
    }
}