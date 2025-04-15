using System.Collections;
using UnityEngine;

public class EnemyDeathEffect : MonoBehaviour
{
    [Header("Effect Settings")]
    [SerializeField] private float scaleUpAmount = 1.1f;
    [SerializeField] private float scaleDownDuration = 0.2f;
    [SerializeField] private float fadeDuration = 0.2f;
    [SerializeField] private SpriteRenderer burstSprite; // A red star/splash sprite
    [SerializeField] private float burstDuration = 0.08f;

    private SpriteRenderer[] spriteRenderers;
    private Vector3 originalScale;

    private void Awake()
    {
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        originalScale = transform.localScale;

        if (burstSprite != null)
            burstSprite.gameObject.SetActive(false);
    }

    public void PlayDeathEffect()
    {
        StartCoroutine(PlayEffectRoutine());
    }

    private IEnumerator PlayEffectRoutine()
    {
        // Show burst sprite
        if (burstSprite != null)
        {
            burstSprite.gameObject.SetActive(true);
            burstSprite.color = Color.red;
            yield return new WaitForSeconds(burstDuration);
            burstSprite.gameObject.SetActive(false);
        }

        // Scale up
        transform.localScale = originalScale * scaleUpAmount;

        // Fade and scale down
        float t = 0f;
        while (t < scaleDownDuration)
        {
            t += Time.deltaTime;
            float progress = t / scaleDownDuration;
            transform.localScale = Vector3.Lerp(originalScale * scaleUpAmount, Vector3.zero, progress);

            foreach (var sr in spriteRenderers)
            {
                Color c = sr.color;
                c.a = Mathf.Lerp(1f, 0f, progress);
                sr.color = c;
            }

            yield return null;
        }

        gameObject.SetActive(false); // Return to pool or destroy
    }
}
