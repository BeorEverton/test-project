using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class PrestigeTreeZoom : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private RectTransform content;
    [SerializeField] private RectTransform viewport;

    [Header("Zoom")]
    [SerializeField] private float minScale = 0.6f;
    [SerializeField] private float maxScale = 2.0f;
    [SerializeField] private float wheelZoomSpeed = 0.15f;

    float currentScale = 1f;

    void Awake()
    {
        if (scrollRect == null) scrollRect = GetComponent<ScrollRect>();
        if (viewport == null && scrollRect != null) viewport = scrollRect.viewport;
        if (content == null && scrollRect != null) content = scrollRect.content;

        currentScale = content != null ? content.localScale.x : 1f;
    }

    void Update()
    {
        if (scrollRect == null || content == null || viewport == null) return;

        float zoomDelta = 0f;

        // Mouse wheel: up = zoom in, down = zoom out
        zoomDelta += Input.mouseScrollDelta.y;

        // Keyboard: zoom in with '+' and ',' ; zoom out with '-' and '.'
        if (Input.GetKey(KeyCode.Equals) || Input.GetKey(KeyCode.KeypadPlus) || Input.GetKey(KeyCode.Comma))
            zoomDelta += .5f;

        if (Input.GetKey(KeyCode.Minus) || Input.GetKey(KeyCode.KeypadMinus) || Input.GetKey(KeyCode.Period))
            zoomDelta -= .5f;

        if (Mathf.Abs(zoomDelta) > 0.001f)
        {
            // Use a per-frame step for keys (so holding zooms smoothly)
            // and keep wheel behavior consistent.
            float step = zoomDelta * wheelZoomSpeed;
            float target = Mathf.Clamp(currentScale * (1f + step), minScale, maxScale);

            if (!Mathf.Approximately(target, currentScale))
                ZoomToMouse(target);
        }
    }

    void ZoomToMouse(float newScale)
    {
        Vector2 screen = Input.mousePosition;

        // Convert screen point to local point in viewport space
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport, screen, null, out Vector2 vpLocal))
            return;

        // Figure out where that point is in content space BEFORE scaling
        Vector2 contentLocalBefore;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(content, screen, null, out contentLocalBefore);

        // Apply scale
        currentScale = newScale;
        content.localScale = new Vector3(currentScale, currentScale, 1f);

        // Force ScrollRect to rebuild bounds this frame
        Canvas.ForceUpdateCanvases();

        // Figure out where the same screen point is in content space AFTER scaling
        Vector2 contentLocalAfter;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(content, screen, null, out contentLocalAfter);

        // Offset content so the point under cursor stays stable
        Vector2 delta = contentLocalAfter - contentLocalBefore;
        content.anchoredPosition += delta * currentScale;

        // One more rebuild helps prevent clamp jitter on some setups
        Canvas.ForceUpdateCanvases();
    }
}