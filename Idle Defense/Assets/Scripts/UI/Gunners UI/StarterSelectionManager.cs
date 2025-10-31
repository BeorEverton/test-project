using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StarterSelectionManager : MonoBehaviour
{
    [Header("Config")]
    [Tooltip("All eligible gunners for starter selection.")]
    public List<GunnerSO> candidateGunners;

    [Tooltip("How many the player must choose.")]
    public int requiredSelections = 1;

    [Tooltip("Slot to auto-equip the first pick into.")]
    public int starterSlotIndex = 0;

    [Header("UI")]
    public Transform gridParent;
    public StarterGunnerCardUI cardPrefab;
    public Button confirmButton;
    public GameObject rootPanel;

    private readonly List<StarterGunnerCardUI> _cards = new();
    private readonly List<GunnerSO> _picked = new();

    private const string PREF_STARTER_DONE = "StarterSelectionDone";

    private void Start()
    {
        if (PlayerPrefs.GetInt(PREF_STARTER_DONE, 0) == 1)
        {
            if (rootPanel) rootPanel.SetActive(false);
            enabled = false;
            return;
        }

        BuildGrid();
        UpdateConfirm();
        if (rootPanel) rootPanel.SetActive(true);

        if (confirmButton)
        {
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(OnConfirm);
        }
    }

    private void BuildGrid()
    {
        foreach (Transform c in gridParent) Destroy(c.gameObject);
        _cards.Clear();
        _picked.Clear();

        foreach (var so in candidateGunners)
        {
            var card = Instantiate(cardPrefab, gridParent);
            card.Bind(so, OnSelectClicked);
            _cards.Add(card);
        }
    }

    private void OnSelectClicked(GunnerSO so)
    {
        // Single-selection behavior:
        if (_picked.Contains(so))
        {
            // Toggle off
            _picked.Clear();
        }
        else
        {
            if (requiredSelections <= 1)
            {
                // Switch selection to the new one
                _picked.Clear();
                _picked.Add(so);
            }
            else
            {
                // Fallback for multi-select setups
                if (_picked.Count >= requiredSelections) return;
                _picked.Add(so);
            }
        }

        // Update card button labels
        for (int i = 0; i < _cards.Count; i++)
        {
            var soI = _cards[i].Bound;
            bool isPicked = _picked.Count > 0 && _picked[0] == soI;
            _cards[i].SetPickedIndex(isPicked ? 0 : -1); // 0 -> Selected, -1 -> Select
        }

        UpdateConfirm();
    }


    private void UpdateConfirm()
    {
        if (confirmButton) confirmButton.interactable = _picked.Count == requiredSelections;
    }

    private void OnConfirm()
    {
        if (_picked.Count != requiredSelections) return;
        var gm = GunnerManager.Instance;
        if (gm == null) return;

        // 1) Preferred = FIRST pick (persisted)
        var preferred = _picked[0];
        gm.SetPreferredStarter(preferred); // persists + grants ownership free

        // 2) Grant ownership to all 3 (free)
        gm.GrantOwnershipFree(_picked);

        // 3) Auto-equip FIRST pick on the starter turret slot
        gm.EquipToFirstFreeSlot(preferred.GunnerId, preferSlotIndex: starterSlotIndex); // anchors handled internally

        // 4) Done forever
        PlayerPrefs.SetInt(PREF_STARTER_DONE, 1);
        PlayerPrefs.Save();

        if (rootPanel) rootPanel.SetActive(false);
        enabled = false;
    }
}
