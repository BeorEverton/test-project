using UnityEngine;
using UnityEngine.UI;

public class GunnerCursorGhost : MonoBehaviour
{
    [Header("UI")]
    public Canvas canvas;        // parent canvas (screen space overlay or camera)
    public GameObject root;      // root object to enable/disable
    private RectTransform rootTransform => root.GetComponent<RectTransform>();
    public Image image;          // UI Image that follows the mouse
    public Vector2 pixelOffset;  // slight offset from cursor if desired

    private bool _active;

    public void Begin(Sprite s)
    {
        if (!image) return;
        image.sprite = s;
        root.SetActive(true);
        _active = true;
    }

    public void End()
    {
        _active = false;
        root.SetActive(false);
    }

    void Update()
    {
        if (!_active || !image) return;
        if (Input.GetMouseButtonDown(1))
        {
            // On right click cancel
            root.SetActive(false);
            _active = false;
            return;
        }
        Vector2 pos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            Input.mousePosition,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
            out pos
        );
        rootTransform.anchoredPosition = pos + pixelOffset;
    }
}
