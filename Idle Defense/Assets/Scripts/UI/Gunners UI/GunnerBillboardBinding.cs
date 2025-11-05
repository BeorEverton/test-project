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
    public DualPhaseBarUI LimitBreakBar;

    [Header("Limit Break UI")]
    public Button LimitBreakButton;    // assign in prefab
    public Image LimitBreakIcon;      // optional icon image
    public GameObject LimitBreakGlow;  // optional VFX highlight

    private GunnerRuntime _rt;

    private bool _lbButtonLocked = false;

    void Awake()
    {
        if (LimitBreakButton)
        {
            LimitBreakButton.onClick.RemoveAllListeners();
            LimitBreakButton.onClick.AddListener(OnClickLimitBreak);
        }
    }

    public void InitializeFromRuntime(GunnerRuntime rt, Sprite limitBreakIcon)
    {
        _rt = rt;

        if (HealthBar != null)
        {
            HealthBar.SetMax(rt.MaxHealth);
            HealthBar.SetValue(rt.CurrentHealth);
        }

        if (LimitBreakBar != null)
        {
            LimitBreakBar.SetMax(rt.LimitBreakMax);
            LimitBreakBar.SetValue(rt.LimitBreakCurrent);
        }

        LimitBreakIcon.sprite = limitBreakIcon;

        RefreshLimitBreakUI();
    }
    public void RefreshLimitBreak(float current)
    {
        if (_rt == null) return;

        _rt.LimitBreakCurrent = current;

        bool full = _rt.LimitBreakCurrent >= _rt.LimitBreakMax;

        if (LimitBreakBar != null)
        {
            if (full)
            {
                // Snap to full, no animation
                LimitBreakBar.SetValue(_rt.LimitBreakMax);
            }
            else
            {
                // Animate normally toward the new value
                LimitBreakBar.SetValue(_rt.LimitBreakCurrent);
            }
        }

        RefreshLimitBreakUI();
    }


    private void RefreshLimitBreakUI()
    {
        bool full = _rt != null && _rt.LimitBreakCurrent >= _rt.LimitBreakMax;

        if (LimitBreakButton)
        {
            // Show only when full; interactable only if not locked.
            LimitBreakButton.gameObject.SetActive(full);
            LimitBreakButton.interactable = full && !_lbButtonLocked;

            // If not full anymore, clear the lock so next time it fills we can click again.
            if (!full) _lbButtonLocked = false;
        }

        // Prefer 3D VFX if available
        if (ModelBinding != null) ModelBinding.SetLimitBreakReady(full);
        else if (LimitBreakGlow) LimitBreakGlow.SetActive(full);
    }


    private void OnClickLimitBreak()
    {
        if (_rt == null) return;
        if (_lbButtonLocked) return; // prevent double-press during same frame

        _lbButtonLocked = true;
        if (LimitBreakButton) LimitBreakButton.interactable = false; // immediate lock

        bool ok = (LimitBreakManager.Instance != null) && LimitBreakManager.Instance.TryActivate(_rt.GunnerId);
        if (ok)
        {
            // Force-clear the runtime meter so the button hides even if debug "no cost" is enabled.
            _rt.LimitBreakCurrent = 0f;
            if (LimitBreakBar != null) LimitBreakBar.SetValue(0f);

            // Propagate to the rest of UI and systems that mirror this value.
            GunnerManager.Instance?.NotifyLimitBreakChanged(_rt.GunnerId);

            // Will hide the button now (since not full) and also clear the lock for the next charge.
            RefreshLimitBreakUI();
        }
        else
        {
            // Activation failed (requirements, cooldown, etc.). Re-enable click if still full.
            _lbButtonLocked = false;
            if (LimitBreakButton)
            {
                bool stillFull = _rt.LimitBreakCurrent >= _rt.LimitBreakMax;
                LimitBreakButton.interactable = stillFull;
            }
        }
    }

}
