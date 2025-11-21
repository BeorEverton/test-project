using System.Collections;
using UnityEngine;

public class HitFlashOverlay : MonoBehaviour
{
    [Header("Target renderer & flash material")]
    [SerializeField] private Renderer targetRenderer;
    [SerializeField] private Material flashMaterial; // shared overlay material

    [Header("Flash settings")]
    [SerializeField] private float flashDuration = 0.07f;
    [SerializeField] private float minDelayBetweenFlashes = 0.1f;
    [SerializeField] private int maxQueuedFlashes = 3;

    private int _flashMaterialIndex = -1;
    private MaterialPropertyBlock _mpb;
    private static readonly int FlashIntensityID = Shader.PropertyToID("_FlashIntensity");

    private Coroutine _flashRoutine;
    private int _pendingFlashes;

    private void Awake()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponent<Renderer>();

        if (targetRenderer == null || flashMaterial == null)
        {
            Debug.LogError($"{name}: HitFlashOverlay missing renderer or flashMaterial.");
            enabled = false;
            return;
        }

        // Ensure the renderer has the flash material, once
        var mats = targetRenderer.sharedMaterials;
        _flashMaterialIndex = System.Array.IndexOf(mats, flashMaterial);

        if (_flashMaterialIndex < 0)
        {
            // Add it once and warn you
            var newMats = new Material[mats.Length + 1];
            for (int i = 0; i < mats.Length; i++)
                newMats[i] = mats[i];

            _flashMaterialIndex = mats.Length;
            newMats[_flashMaterialIndex] = flashMaterial;

            targetRenderer.sharedMaterials = newMats;

            Debug.LogWarning($"{name}: Flash material was missing, automatically added.");
        }

        if (_mpb == null)
            _mpb = new MaterialPropertyBlock();

        // Initialize intensity to 0
        targetRenderer.GetPropertyBlock(_mpb);
        _mpb.SetFloat(FlashIntensityID, 0f);
        targetRenderer.SetPropertyBlock(_mpb);
    }
    public void TriggerFlash()
    {
        if (!isActiveAndEnabled || targetRenderer == null)
            return;

        // Queue a flash, but clamp so we do not build an infinite queue.
        if (_pendingFlashes < maxQueuedFlashes)
            _pendingFlashes++;

        // Start processor if not already running.
        if (_flashRoutine == null)
            _flashRoutine = StartCoroutine(Co_FlashLoop());
    }
    private IEnumerator Co_FlashLoop()
    {
        // Process all pending flashes one by one.
        while (_pendingFlashes > 0)
        {
            _pendingFlashes--;

            // One pulse.
            yield return FlashOnce();

            // Small gap between pulses, if any left.
            if (_pendingFlashes > 0 && minDelayBetweenFlashes > 0f)
                yield return new WaitForSeconds(minDelayBetweenFlashes);
        }

        // Ensure fully off at the end.
        targetRenderer.GetPropertyBlock(_mpb);
        _mpb.SetFloat(FlashIntensityID, 0f);
        targetRenderer.SetPropertyBlock(_mpb);

        StopAndClear();
        _flashRoutine = null;
    }

    private IEnumerator FlashOnce()
    {
        float t = 0f;

        while (t < flashDuration)
        {
            t += Time.deltaTime;
            float k = 1f - (t / flashDuration); // 1 -> 0

            targetRenderer.GetPropertyBlock(_mpb, _flashMaterialIndex);
            _mpb.SetFloat(FlashIntensityID, k);
            targetRenderer.SetPropertyBlock(_mpb, _flashMaterialIndex);

            yield return null;
        }
    }

    /// <summary>
    /// Immediately stops all queued flashes and forces intensity to 0.
    /// </summary>
    public void StopAndClear()
    {
        _pendingFlashes = 0;

        if (_flashRoutine != null)
        {
            StopCoroutine(_flashRoutine);
            _flashRoutine = null;
        }

        if (targetRenderer == null || _mpb == null)
            return;

        targetRenderer.GetPropertyBlock(_mpb, _flashMaterialIndex);
        _mpb.SetFloat(FlashIntensityID, 0f);
        targetRenderer.SetPropertyBlock(_mpb, _flashMaterialIndex);
    }

    private void OnDisable()
    {
        // If this object is pooled and disabled mid-flash,
        // make sure we don't keep the red overlay when it comes back.
        StopAndClear();
    }

}



