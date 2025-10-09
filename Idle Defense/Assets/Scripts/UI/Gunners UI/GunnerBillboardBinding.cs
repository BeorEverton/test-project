using Assets.Scripts.UI;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class GunnerBillboardBinding : MonoBehaviour
{
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
        if (LimitBreakBar != null) LimitBreakBar.SetValue(current);
        RefreshLimitBreakUI();
    }

    private void RefreshLimitBreakUI()
    {
        bool full = _rt != null && _rt.LimitBreakCurrent >= _rt.LimitBreakMax;
        if (LimitBreakButton) LimitBreakButton.gameObject.SetActive(full);
        if (LimitBreakGlow) LimitBreakGlow.SetActive(full);
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
