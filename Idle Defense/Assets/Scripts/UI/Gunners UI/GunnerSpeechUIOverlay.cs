using System.Collections.Generic;
using UnityEngine;

public class GunnerSpeechUIOverlay : MonoBehaviour, IGunnerSpeechUI
{
    [Header("Layout")]
    [SerializeField] private RectTransform container;     // parent for toasts (e.g., a vertical group)
    [SerializeField] private GunnerSpeechToast toastPrefab;
    [SerializeField] private int poolSize = 6;

    [Header("Portrait Fallback")]
    [SerializeField] private Sprite defaultPortrait;

    private readonly Queue<GunnerSpeechToast> _pool = new Queue<GunnerSpeechToast>();

    private void Awake()
    {
        if (!container) container = (RectTransform)transform;

        // warm pool
        for (int i = 0; i < poolSize; i++)
        {
            var t = Instantiate(toastPrefab, container);
            t.gameObject.SetActive(false);
            _pool.Enqueue(t);
        }
    }

    public void ShowLine(GunnerSO speaker, GunnerSO listener, string text)
    {
        Debug.Log($"[GunnerSpeechUIOverlay] ShowLine from {speaker?.GunnerId} to {listener?.GunnerId}: {text}");
        if (toastPrefab == null || container == null) return;
        var toast = GetToast();

        // portrait pick
        Sprite face = defaultPortrait;
        if (speaker != null)
        {
            if (speaker.gunnerSprite) face = speaker.gunnerSprite;
        }

        // guarantee active + enabled before starting the toast coroutine
        if (!toast.gameObject.activeSelf) toast.gameObject.SetActive(true);
        if (!toast.enabled) toast.enabled = true;
        Debug.Log($"[GunnerSpeechUIOverlay] Showing toast for {speaker?.GunnerId} with face {face?.name}");
        toast.transform.SetAsLastSibling();
        toast.Init(face, text, speaker.DisplayName);
    }

    private GunnerSpeechToast GetToast()
    {
        // 1) try an inactive child first
        foreach (Transform child in container)
        {
            var t = child.GetComponent<GunnerSpeechToast>();
            if (t != null && !t.gameObject.activeSelf) return t;
        }

        // 2) if the warm pool still has objects, instantiate one more (don’t dequeue a hidden reference)
        return Instantiate(toastPrefab, container);
    }

}
