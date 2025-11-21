using Assets.Scripts.WaveSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PrestigeUIController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Button prestigeButton;
    [SerializeField] private TextMeshProUGUI buttonText;      // shows "Reach wave X to prestige" or "Prestige (+X CC)"
    [SerializeField] private GameObject confirmPopup;          // the popup root
    [SerializeField] private TextMeshProUGUI popupTitleText;   // "Prestige"
    [SerializeField] private TextMeshProUGUI popupBodyText;    // details, rewards, resets
    [SerializeField] private Button popupYesButton;
    [SerializeField] private Button popupNoButton;

    [Header("Panel Access")]
    [Tooltip("This is the button that opens the Prestige panel. It stays hidden until PrestigeLevel > 0.")]
    [SerializeField] private GameObject openPanelButton;

    [Tooltip("Optional: show a 'wait until wave' suggestion with extra CC preview.")]
    [SerializeField] private int suggestWaveForBonus = 0; // 0 = disabled
    private bool _hooked;

    private void Awake()
    {
        if (prestigeButton) prestigeButton.onClick.AddListener(OnClickPrestige);
        if (popupYesButton) popupYesButton.onClick.AddListener(ConfirmPrestige);
        if (popupNoButton) popupNoButton.onClick.AddListener(() => ShowPopup(false));
    }

    private void OnEnable()
    {
        HookIfReady();
        Refresh();
        if (!_hooked) StartCoroutine(WaitAndHookPrestigeManager());
    }

    private void OnDisable()
    {
        Unhook();
    }

    private System.Collections.IEnumerator WaitAndHookPrestigeManager()
    {
        // Wait until the PrestigeManager singleton exists (covers scene load order)
        while (PrestigeManager.Instance == null) yield return null;
        HookIfReady();
        Refresh();
    }

    private void HookIfReady()
    {
        if (_hooked || PrestigeManager.Instance == null) return;
        PrestigeManager.Instance.OnPrestigeChanged += Refresh;
        PrestigeManager.Instance.OnPrestigeEligibilityChanged += OnElig;
        _hooked = true;
    }

    private void Unhook()
    {
        if (!_hooked || PrestigeManager.Instance == null) { _hooked = false; return; }
        PrestigeManager.Instance.OnPrestigeChanged -= Refresh;
        PrestigeManager.Instance.OnPrestigeEligibilityChanged -= OnElig;
        _hooked = false;
    }


    private void OnElig(bool _, int __, int ___) => Refresh();

    // Match WaveManager's EventHandler<OnWaveStartedEventArgs> signature
    private void OnWaveStarted(object sender, WaveManager.OnWaveStartedEventArgs e)
    {
        Refresh();
    }

    public void Refresh()
    {
        var pm = PrestigeManager.Instance;

        // If PM isn't ready yet, show a neutral disabled state (prevents int.Max).
        if (pm == null)
        {
            if (prestigeButton) prestigeButton.interactable = false;
            if (buttonText) buttonText.text = "Preparing prestige…";
            return;
        }

        bool unlocked = pm != null && pm.GetPrestigeLevel() > 0;
        if (openPanelButton != null)
            openPanelButton.SetActive(unlocked);

        int min = pm.GetMinWaveToPrestige();
        bool canPrestige = pm.CanPrestigeNow();

        if (prestigeButton) prestigeButton.gameObject.SetActive(canPrestige);

        string label = !canPrestige
            ? $"Reach wave {min} to Prestige"
            : $"Prestige (+{pm.PreviewCrimsonForPrestige()} CC)";
        if (buttonText) buttonText.text = label;
    }

    private void OnClickPrestige()
    {
        if (!prestigeButton || !prestigeButton.interactable) return;
        BuildAndShowPopup();
    }

    private void BuildAndShowPopup()
    {
        int cur = WaveManager.Instance ? WaveManager.Instance.GetCurrentWaveIndex() : 0;
        int nowCC = PrestigeManager.Instance ? PrestigeManager.Instance.PreviewCrimsonForPrestige() : 0;

        string bonusLine = "";
        if (suggestWaveForBonus > 0 && cur < suggestWaveForBonus)
        {
            int later = PrestigeManager.Instance ? PrestigeManager.Instance.PreviewCrimsonForPrestige(bestWaveOverride: suggestWaveForBonus) : nowCC;
            int delta = Mathf.Max(0, later - nowCC);
            bonusLine = $"\nPrestige at wave {suggestWaveForBonus} for +{delta} bonus Crimson Core.";
        }

        // Reset summary based on toggles in PrestigeManager
        var pm = PrestigeManager.Instance;
        string resets = pm
            ? BuildResetSummary(pm)
            : "Resets: (see design toggles)";

        string body = $"Prestige now for <b>+{nowCC}</b> Crimson Core."
                    + bonusLine
                    + $"\n\n{resets}"
                    + $"\nNew run will start at wave {(pm ? pm.RestartWaveIndexForUI() : 1)}.";

        if (popupTitleText) popupTitleText.text = "Prestige";
        if (popupBodyText) popupBodyText.text = body;

        ShowPopup(true);
    }

    private string BuildResetSummary(PrestigeManager pm)
    {
        // Expose small getters on PrestigeManager for clean UI (we add them below)
        System.Text.StringBuilder sb = new System.Text.StringBuilder("Resets:\n");
        if (pm.ResetTurretsOwnership) sb.AppendLine("- Turret ownership");
        if (pm.ResetTurretsUpgrades) sb.AppendLine("- Turret upgrades");
        if (pm.ResetGunnersOwnership) sb.AppendLine("- Gunner ownership (keeps preferred starter)");
        if (pm.ResetGunnerLevels) sb.AppendLine("- Gunner levels");
        if (pm.ResetGunnersUpgrades) sb.AppendLine("- Gunner upgrades");
        if (pm.ResetScraps) sb.AppendLine("- Scraps");
        if (pm.ResetBlackSteel) sb.AppendLine("- Black Steel");
        sb.AppendLine("- Player Base rebuilt");
        return sb.ToString();
    }

    private void ShowPopup(bool show)
    {
        if (confirmPopup) confirmPopup.SetActive(show);
    }

    private void ConfirmPrestige()
    {
        ShowPopup(false);
        PrestigeManager.Instance?.PerformPrestigeNow();
    }
}
