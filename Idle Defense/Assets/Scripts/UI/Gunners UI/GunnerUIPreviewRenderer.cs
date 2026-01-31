using System.Collections.Generic;
using UnityEngine;

public class GunnerUIPreviewRenderer : MonoBehaviour
{
    [Header("Scene refs")]
    [SerializeField] private Transform previewRoot;
    [SerializeField] private Transform decorationRoot;

    [Header("Layering")]
    [SerializeField] private string previewLayerName = "GunnerPreview";

    [Header("Locked look")]
    [SerializeField] private Material lockedMaterial; // unlit black
    private readonly List<Material> _originalMats = new List<Material>();

    [Header("Motion")]
    [SerializeField] private bool subtleSway = true;
    [SerializeField] private float swaySpeed = 0.6f;
    [SerializeField] private float swayAngle = 0.6f;

    private GameObject _currentModel;
    private int _previewLayer;
    private float _swayT;

    private void Awake()
    {
        _previewLayer = LayerMask.NameToLayer(previewLayerName);
        if (_previewLayer < 0)
            Debug.LogWarning($"[GunnerUIPreviewRenderer] Layer '{previewLayerName}' not found.");
    }

    private void Update()
    {
        if (!subtleSway || _currentModel == null) return;

        _swayT += Time.unscaledDeltaTime * swaySpeed;
        float a = Mathf.Sin(_swayT) * swayAngle;
        // tiny Y-rotation reads well for “alive”
        previewRoot.localRotation = Quaternion.Euler(0f, a, 0f);
    }

    public void Show(GameObject modelPrefab, Vector3 localPos, Vector3 localEuler, Vector3 localScale, bool locked)
    {
        Clear();

        if (modelPrefab == null)
            return;

        _currentModel = Instantiate(modelPrefab, previewRoot);
        _currentModel.transform.localPosition = localPos;
        _currentModel.transform.localRotation = Quaternion.Euler(localEuler);
        _currentModel.transform.localScale = localScale;

        SetLayerRecursively(_currentModel.transform, _previewLayer);

        // If you want: force an "Idle" state immediately
        var anim = _currentModel.GetComponentInChildren<Animator>();
        if (anim != null)
        {
            anim.updateMode = AnimatorUpdateMode.UnscaledTime; // UI preview should ignore timescale
            anim.Play("Idle", 0, 0f);
        }

        if (locked)
            ApplyLockedMaterials(_currentModel);
    }

    public void SetDecorations(GameObject decorationPrefab, Vector3 localPos, Vector3 localEuler, Vector3 localScale)
    {
        // Optional: spawn 1 prefab per region.
        if (decorationRoot == null) return;

        for (int i = decorationRoot.childCount - 1; i >= 0; i--)
            Destroy(decorationRoot.GetChild(i).gameObject);

        if (decorationPrefab == null) return;

        var deco = Instantiate(decorationPrefab, decorationRoot);
        deco.transform.localPosition = localPos;
        deco.transform.localRotation = Quaternion.Euler(localEuler);
        deco.transform.localScale = localScale;

        SetLayerRecursively(deco.transform, _previewLayer);
    }

    public void Clear()
    {
        previewRoot.localRotation = Quaternion.identity;
        _swayT = 0f;

        if (_currentModel != null)
        {
            Destroy(_currentModel);
            _currentModel = null;
        }

        if (decorationRoot != null)
        {
            for (int i = decorationRoot.childCount - 1; i >= 0; i--)
                Destroy(decorationRoot.GetChild(i).gameObject);
        }

        _originalMats.Clear();
    }

    private void ApplyLockedMaterials(GameObject root)
    {
        if (lockedMaterial == null) return;

        var renderers = root.GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers)
        {
            var mats = r.sharedMaterials;
            _originalMats.AddRange(mats);

            for (int i = 0; i < mats.Length; i++)
                mats[i] = lockedMaterial;

            r.sharedMaterials = mats;
        }
    }

    private static void SetLayerRecursively(Transform t, int layer)
    {
        if (layer < 0) return;
        t.gameObject.layer = layer;
        for (int i = 0; i < t.childCount; i++)
            SetLayerRecursively(t.GetChild(i), layer);
    }
}
