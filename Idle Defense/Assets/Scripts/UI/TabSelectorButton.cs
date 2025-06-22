using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TabSelectorButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private RectTransform buttonTransform;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Color selectedColor = Color.red;
    [SerializeField] private Color normalColor = Color.white;

    private Vector3 originalScale;
    private Color originalColor;

    private static TabSelectorButton currentlySelected;

    private void Awake()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
            buttonTransform = button.GetComponent<RectTransform>();
        }

        if (backgroundImage == null)
            backgroundImage = GetComponent<Image>();

        button.onClick.AddListener(AnimateBuyButtonClick);
        //button.onClick.AddListener(OnClicked);
        originalScale = button.GetComponent<RectTransform>().localScale;
        originalColor = button.GetComponent<Image>().color;

        // Automatically add EventTrigger events
        AddEventTrigger(EventTriggerType.PointerEnter, OnPointerEnter);
        AddEventTrigger(EventTriggerType.PointerExit, OnPointerExit);
    }

    /* REMOVED BEHAVIOUR TO KEEP RED OVERLAY ON CLICK
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
    }*/

    public void OnPointerEnter(BaseEventData eventData)
    {
        button.GetComponent<RectTransform>().DOScale(originalScale * 0.96f, 0.05f)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true); // Use unscaled time

        button.GetComponent<Image>().DOColor(Color.red, 0.05f)
            .SetUpdate(true); // Use unscaled time

        Assets.Scripts.Systems.Audio.AudioManager.Instance.PlayHoverSound();

    }


    public void OnPointerExit(BaseEventData eventData)
    {
        button.GetComponent<RectTransform>().DOScale(originalScale, 0.1f)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true);

        button.GetComponent<Image>().DOColor(originalColor, 0.1f)
            .SetUpdate(true);
    }


    public void AnimateBuyButtonClick()
    {
        button.DOKill();

        buttonTransform.DOScale(Vector3.one * 1.15f, 0.1f)
            .SetEase(Ease.OutBack)
            .SetUpdate(true)
            .OnComplete(() => {
                buttonTransform.DOScale(Vector3.one, 0.1f)
                    .SetEase(Ease.InOutSine)
                    .SetUpdate(true);
            });
    }

    private void AddEventTrigger(EventTriggerType eventType, UnityEngine.Events.UnityAction<BaseEventData> action)
    {
        EventTrigger trigger = GetComponent<EventTrigger>();
        if (trigger == null)
            trigger = gameObject.AddComponent<EventTrigger>();

        EventTrigger.Entry entry = new EventTrigger.Entry { eventID = eventType };
        entry.callback.AddListener(action);
        trigger.triggers.Add(entry);
    }

}
