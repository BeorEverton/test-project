using UnityEngine;
using UnityEngine.UI;

public class TabSelectorButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Color selectedColor = Color.red;
    [SerializeField] private Color normalColor = Color.white;

    private static TabSelectorButton currentlySelected;

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (backgroundImage == null)
            backgroundImage = GetComponent<Image>();

        button.onClick.AddListener(OnClicked);
    }

    private void OnClicked()
    {
        Select();
    }

    public void Select()
    {
        // Deselect the previous one
        if (currentlySelected != null && currentlySelected != this)
            currentlySelected.Deselect();

        // Mark this as selected
        currentlySelected = this;
        backgroundImage.color = selectedColor;
    }

    public void Deselect()
    {
        backgroundImage.color = normalColor;
    }
}
