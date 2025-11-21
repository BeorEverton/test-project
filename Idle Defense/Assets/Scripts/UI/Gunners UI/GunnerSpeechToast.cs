using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GunnerSpeechToast : MonoBehaviour
{
    [Header("Refs")]
    public Image portrait;
    public TMP_Text label, gunnerName;
    public CanvasGroup group;

    [Header("Timing")]
    public float lifetime = 3.5f;
    public float fadeOut = 0.35f;

    private Coroutine _routine;

    public void Init(Sprite face, string text, string _gunnerName)
    {
        if (portrait) portrait.sprite = face;
        if (label) label.text = text;
        if (gunnerName) gunnerName.text = _gunnerName;
        if (group) group.alpha = 1f;

        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(LifeRoutine());
    }

    private void OnEnable()
    {
        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(LifeRoutine());
    }

    private IEnumerator LifeRoutine()
    {
        yield return new WaitForSecondsRealtime(lifetime);

        float t = 0f;
        float start = group ? group.alpha : 1f;
        while (t < fadeOut)
        {
            t += Time.deltaTime;
            if (group) group.alpha = Mathf.Lerp(start, 0f, t / fadeOut);
            yield return null;
        }

        gameObject.SetActive(false);
    }
}
