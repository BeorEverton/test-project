using UnityEngine;
using UnityEngine.UI;
using Assets.Scripts.Turrets;
using Assets.Scripts.Systems;
using Assets.Scripts.UI;

public class TurretUpgradePanelUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button unequipButton;
    [SerializeField] private Transform contentRoot;   // Parent of all TurretUpgradeButtons
    private TurretStatsInstance _stats;          // stats of the turret in this slot

    private int _slotIndex;
    private BaseTurret _baseTurret;

    public void Open(int slotIndex, TurretStatsInstance stats, BaseTurret baseTurret)
    {
        _slotIndex = slotIndex;
        _stats = stats;                     
        _baseTurret = baseTurret;

        // Assign turret to all upgrade buttons inside this panel
        foreach (var btn in contentRoot.GetComponentsInChildren<TurretUpgradeButton>())
        {
            btn._baseTurret = _baseTurret;
            btn.enabled = true;
            btn.Init();
            btn.SetTurret();             // ensures UpgradeManager is assigned
            btn.UpdateDisplayFromType();
        }

        gameObject.SetActive(true);
    }

    public void Close() => gameObject.SetActive(false);

    private void Start()
    {
        unequipButton.onClick.AddListener(OnClickUnequip);
    }

    private void OnClickUnequip()
    {
        TurretSlotManager.I.Unequip(_slotIndex);
        Close();
    }
}
