using Assets.Scripts.UI;
using UnityEngine;

public class ToggleVisibility : MonoBehaviour
{
    [SerializeField] private GameObject target;
    [SerializeField] private bool pauseWhenVisible = false;

    public void Toggle()
    {
        if (target == null) return;

        bool newState = !target.activeSelf;
        target.SetActive(newState);

        if (pauseWhenVisible)
        {
            if (newState)
                UIManager.Instance.PauseGame(true);   // Activating the panel: pause
            else
                UIManager.Instance.PauseGame(false); // Deactivating the panel: unpause
        }
    }
}
