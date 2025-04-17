using System.Collections;
using TMPro;
using UnityEngine;

public class TypingText : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textUI;
    [SerializeField, TextArea] private string fullText;
    [SerializeField] private float typingSpeed = 0.05f;
    [SerializeField] private HeartbeatLine waveAnimator;

    public void StartTyping(string newText)
    {
        fullText = newText;
        StartCoroutine(TypeTextRoutine());
    }

    private IEnumerator TypeTextRoutine()
    {
        waveAnimator.SetTypingActive(true);

        textUI.text = "";
        foreach (char c in fullText)
        {
            textUI.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }

        waveAnimator.SetTypingActive(false);
    }
}
