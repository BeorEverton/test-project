using Assets.Scripts.UI;
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
        if (LimitBreakButton) LimitBreakButton.gameObject.SetActive(full);

        // Prefer 3D VFX if available
        if (ModelBinding != null) ModelBinding.SetLimitBreakReady(full);
        else if (LimitBreakGlow) LimitBreakGlow.SetActive(full);
    }


    private void OnClickLimitBreak()
    {
        if (_rt == null) return;
        bool ok = LimitBreakManager.Instance != null && LimitBreakManager.Instance.TryActivate(_rt.GunnerId);
        if (ok)
        {
            if (LimitBreakBar != null) LimitBreakBar.SetValue(0f);
            RefreshLimitBreakUI();
        }
    }
}
