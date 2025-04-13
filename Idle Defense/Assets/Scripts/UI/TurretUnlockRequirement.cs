using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Assets.Scripts.WaveSystem;
using Assets.Scripts.UI;
using Assets.Scripts.Systems;

public class TurretUnlockRequirement : MonoBehaviour
{
    [SerializeField] private GameObject lockedOverlay, turretToUnlock;         
    [SerializeField] private Button unlockButton;              // Button shown once unlockable
    [SerializeField] private TextMeshProUGUI unlockText;
    [SerializeField] private int requiredWave = 10;
    [SerializeField] private ulong unlockCost = 100;

    private bool isUnlocked = false;

    private void Start()
    {
        WaveManager.Instance.OnWaveStarted += OnWaveStarted;
        unlockButton.onClick.AddListener(UnlockTurret);

        RefreshState(WaveManager.Instance.GetCurrentWaveIndex());
    }

    private void OnDestroy()
    {
        WaveManager.Instance.OnWaveStarted -= OnWaveStarted;
        unlockButton.onClick.RemoveListener(UnlockTurret);
    }

    private void OnWaveStarted(object sender, WaveManager.OnWaveStartedEventArgs e)
    {
        RefreshState(e.WaveNumber);
    }

    private void RefreshState(int currentWave)
    {
        if (isUnlocked)
        {
            lockedOverlay.SetActive(false);
            unlockButton.gameObject.SetActive(false);
            return;
        }

        if (currentWave >= requiredWave)
        {
            unlockButton.gameObject.SetActive(true);
            lockedOverlay.SetActive(true);
            unlockText.text = $"Unlock ${UIManager.AbbreviateNumber(unlockCost)}";
        }
        else
        {
            lockedOverlay.SetActive(true);
            unlockButton.gameObject.SetActive(false);
            unlockText.text = $"Locked\nWave {requiredWave}";
        }
    }

    private void UnlockTurret()
    {
        if (GameManager.Instance.Money >= unlockCost)
        {
            GameManager.Instance.SpendMoney(unlockCost);
            isUnlocked = true;
            RefreshState(WaveManager.Instance.GetCurrentWaveIndex());

            turretToUnlock.SetActive(true);
        }
        else
        {
            // Sound to represent no money
        }
    }
}
