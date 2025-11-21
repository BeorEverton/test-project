using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Assets.Scripts.WaveSystem;

public class DebugWaveJumpUI : MonoBehaviour
{
    [SerializeField] private TMP_InputField waveInput;
    [SerializeField] private Button jumpButton;

    private void Awake()
    {
        if (jumpButton != null) jumpButton.onClick.AddListener(OnJumpClicked);
    }

    private void OnDestroy()
    {
        if (jumpButton != null) jumpButton.onClick.RemoveListener(OnJumpClicked);
    }

    private void OnJumpClicked()
    {
        if (WaveManager.Instance == null) { Debug.LogWarning("WaveManager not ready."); return; }
        if (waveInput == null || string.IsNullOrWhiteSpace(waveInput.text)) { Debug.LogWarning("Wave input empty."); return; }

        if (!int.TryParse(waveInput.text, out int target))
        {
            Debug.LogWarning($"Invalid wave: {waveInput.text}");
            return;
        }

        WaveManager.Instance.DebugJumpToWave(target);
    }
}
