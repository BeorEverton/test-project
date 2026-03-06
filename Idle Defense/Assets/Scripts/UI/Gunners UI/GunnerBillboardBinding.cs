using Assets.Scripts.UI;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class GunnerBillboardBinding : MonoBehaviour
{
    [Header("3D Binding (optional)")]
    public GunnerModelBinding ModelBinding;

    [Header("Bars on this billboard")]
    public DualPhaseBarUI HealthBar;

    [Header("Limit Break UI")]
    public Button LimitBreakButton;      // assign in prefab
    public Image LimitBreakIcon;         // should be an Image set up as Filled
    public GameObject LimitBreakGlow;    // optional VFX highlight

    [Header("Limit Break Icon Visuals")]
    [SerializeField] private Color LimitBreakNotFullColor = new Color(0.5f, 0.5f, 0.5f, 0.7f); // grey + faded
    [SerializeField] private Color LimitBreakFullColor = new Color(1f, 1f, 1f, 1f);              // white + full alpha

    private GunnerRuntime _rt;
    private bool _lbButtonLocked = false;

    void Awake()
    {
        if (LimitBreakButton)
        {
            LimitBreakButton.onClick.RemoveAllListeners();
            LimitBreakButton.onClick.AddListener(OnClickLimitBreak);
        }

        // We only control fillAmount; the fill method/origin/clockwise should be configured on the prefab.
        if (LimitBreakIcon && LimitBreakIcon.type != Image.Type.Filled)
            LimitBreakIcon.type = Image.Type.Filled;
    }

    public void InitializeFromRuntime(GunnerRuntime rt, Sprite limitBreakIcon)
    {
        _rt = rt;

        if (HealthBar != null)
        {
            HealthBar.SetMax(rt.MaxHealth);
            HealthBar.SetValue(rt.CurrentHealth);
        }

        if (LimitBreakIcon)
            LimitBreakIcon.sprite = limitBreakIcon;

        RefreshLimitBreakUI();
    }

    public void RefreshLimitBreak(float current)
    {
        if (_rt == null) return;

        _rt.LimitBreakCurrent = current;
        RefreshLimitBreakUI();
    }

    private void RefreshLimitBreakUI()
    {
        bool hasRt = _rt != null;
        float max = hasRt ? _rt.LimitBreakMax : 0f;
        float cur = hasRt ? _rt.LimitBreakCurrent : 0f;

        float t = (max <= 0f) ? 0f : Mathf.Clamp01(cur / max);
        bool full = hasRt && (cur >= max) && (max > 0f);

        // Icon becomes the "bar"
        if (LimitBreakIcon)
        {
            if (LimitBreakIcon.type != Image.Type.Filled)
                LimitBreakIcon.type = Image.Type.Filled;

            LimitBreakIcon.fillAmount = t;
            LimitBreakIcon.color = full ? LimitBreakFullColor : LimitBreakNotFullColor;
        }

        // Button is always visible; only clickable when full (and not locked)
        if (LimitBreakButton)
        {
            if (!LimitBreakButton.gameObject.activeSelf)
                LimitBreakButton.gameObject.SetActive(true);

            LimitBreakButton.interactable = full && !_lbButtonLocked;

            // If not full anymore, clear the lock so next time it fills we can click again.
            if (!full) _lbButtonLocked = false;
        }

        // Ready highlight (optional)
        if (ModelBinding != null) ModelBinding.SetLimitBreakReady(full);
        else if (LimitBreakGlow) LimitBreakGlow.SetActive(full);
    }

    private void OnClickLimitBreak()
    {
        if (_rt == null) return;
        if (_lbButtonLocked) return;

        bool full = (_rt.LimitBreakMax > 0f) && (_rt.LimitBreakCurrent >= _rt.LimitBreakMax);
        if (!full) return;

        _lbButtonLocked = true;
        if (LimitBreakButton) LimitBreakButton.interactable = false;

        bool ok = (LimitBreakManager.Instance != null) && LimitBreakManager.Instance.TryActivate(_rt.GunnerId);
        if (ok)
        {
            _rt.LimitBreakCurrent = 0f;

            // Propagate to the rest of UI and systems that mirror this value.
            GunnerManager.Instance?.NotifyLimitBreakChanged(_rt.GunnerId);

            RefreshLimitBreakUI();
        }
        else
        {
            // Activation failed; unlock if still full.
            _lbButtonLocked = false;
            RefreshLimitBreakUI();
        }
    }
}