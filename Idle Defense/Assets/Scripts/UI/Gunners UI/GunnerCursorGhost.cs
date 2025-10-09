using UnityEngine;
using UnityEngine.UI;

public class GunnerCursorGhost : MonoBehaviour
{
    [Header("UI")]
    public Canvas canvas;        // parent canvas (screen space overlay or camera)
    public Image image;          // UI Image that follows the mouse
    public Vector2 pixelOffset;  // slight offset from cursor if desired

    private bool _active;

    void Awake()
    {
        if (image) image.enabled = false;
    }

    public void Begin(Sprite s)
    {
        if (!image) return;
        image.sprite = s;
        image.enabled = true;
        _active = true;
    }

    public void End()
    {
        _active = false;
        if (image) image.enabled = false;
    }

    void Update()
    {
        if (!_active || !image) return;

        Vector2 pos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            Input.mousePosition,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
            out pos
        );
        image.rectTransform.anchoredPosition = pos + pixelOffset;
    }
}
