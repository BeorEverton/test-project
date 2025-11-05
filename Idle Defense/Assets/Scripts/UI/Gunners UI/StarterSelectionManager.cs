using System;
using System.Collections;
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

    [Header("UI")]
    public Transform gridParent;
    public StarterGunnerCardUI cardPrefab;
    public Button confirmButton;
    public GameObject rootPanel;

    private readonly List<StarterGunnerCardUI> _cards = new();
    private readonly List<GunnerSO> _picked = new();

    private void Start()
    {
        StartCoroutine(LateStart());
    }

    IEnumerator LateStart()
    {
        yield return null;
        yield return null;
        if (GunnerManager.Instance.preferredStarterGunner != null)
        {
            if (rootPanel) rootPanel.SetActive(false);
            enabled = false;
            yield break;
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

        // 1) Preferred = FIRST pick (persist in save)
        var preferred = _picked[0];
        gm.SetPreferredStarter(preferred);   // grants ownership + writes via save

        // 2) Grant ownership to all selected (free)
        gm.GrantOwnershipFree(_picked);
                
        // 3) Ensure GunnerManager has the intended starter slot index before equip.
        // If your UI determines a specific starter slot, call SetStarterSlotIndex(desiredIndex) here.
        // Otherwise, it uses the manager's default (serialized) value.
        gm.EquipToFirstFreeSlot(preferred.GunnerId, preferSlotIndex: gm.StarterSlotIndex);

        if (rootPanel) rootPanel.SetActive(false);
        enabled = false;

    }
}
