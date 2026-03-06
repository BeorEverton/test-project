using System.Collections.Generic;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
using UnityEngine;

public class PrestigeTreeUI : MonoBehaviour
{
    [Header("Data")]
    public PrestigeTreeSO tree;

    [Header("Layout")]
    public RectTransform canvasRoot;       // parent container (Screen Space - Overlay or Camera)
    public GameObject nodePrefab;          // prefab with PrestigeNodeView, Image, Button, TMP
    public GameObject linePrefab;          // prefab with Image + PrestigeConnectionView

    // Runtime maps
    private readonly Dictionary<string, PrestigeNodeView> nodeViews = new Dictionary<string, PrestigeNodeView>();
    private readonly List<(PrestigeConnectionViewLR cv, string parentId, string childId)> connections = new();

    [Header("Runtime Build")]
    [SerializeField] private bool buildNodesAtRuntimeIfEmpty = false;      // default OFF: use editor-built
    [SerializeField] private bool buildConnectionsAtRuntimeIfEmpty = false; // default OFF: use editor-built

    private bool subscribed;

    private void OnEnable()
    {
        if (!subscribed && PrestigeManager.Instance != null)
        {
            PrestigeManager.Instance.OnPrestigeChanged += HandlePrestigeChanged;
            subscribed = true;
        }

        // 1) Hydrate from scene (editor-built nodes/lines)
        HydrateFromScene();

        // 2) Optionally build missing pieces at runtime (usually keep these OFF)
        if (nodeViews.Count == 0 && buildNodesAtRuntimeIfEmpty)
            BuildNodesFromTree_Runtime();

        if (connections.Count == 0 && buildConnectionsAtRuntimeIfEmpty)
            BuildConnectionsFromTree_Runtime();

        RefreshAll();
    }


    private void OnDisable()
    {
        if (subscribed && PrestigeManager.Instance != null)
        {
            PrestigeManager.Instance.OnPrestigeChanged -= HandlePrestigeChanged;
            subscribed = false;
        }
    }

    // Editor-time creator (used by Context_CreateMissingConnections)
    private void TryMakeConnection(string parentId, string childId)
    {
        if (!nodeViews.ContainsKey(parentId) || !nodeViews.ContainsKey(childId)) return;

        var go = Instantiate(linePrefab, canvasRoot);
        go.name = $"Line_{parentId}_to_{childId}";
        var cv = go.GetComponent<PrestigeConnectionViewLR>();
        if (cv == null) cv = go.AddComponent<PrestigeConnectionViewLR>();
        // Make sure this line uses the same local space as canvasRoot
        var rt = go.transform as RectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition3D = Vector3.zero;
        rt.localRotation = Quaternion.identity;
        rt.localScale = Vector3.one;
        cv.parentNodeId = parentId;
        cv.childNodeId = childId;

        // The baked points are authored in canvasRoot space, so convert from that space into the line's local.
        cv.spaceRoot = canvasRoot;

        connections.Add((cv, parentId, childId));
    }

    // Runtime creator (only used if you enable buildConnectionsAtRuntimeIfEmpty)
    private void TryMakeConnection_Runtime(string parentId, string childId)
    {
        if (!nodeViews.ContainsKey(parentId) || !nodeViews.ContainsKey(childId)) return;

        var go = Instantiate(linePrefab, canvasRoot);
        go.name = $"Line_{parentId}_to_{childId}";
        var cv = go.GetComponent<PrestigeConnectionViewLR>();
        if (cv == null) cv = go.AddComponent<PrestigeConnectionViewLR>();
        cv.parentNodeId = parentId;
        cv.childNodeId = childId;

        // If we’re making lines at runtime, create a simple straight path so it’s visible.
        var a = nodeViews[parentId].transform as RectTransform;
        var b = nodeViews[childId].transform as RectTransform;
        Vector2 p = LocalPos(a, canvasRoot);
        Vector2 q = LocalPos(b, canvasRoot);

        var pts = new List<Vector3> { new Vector3(p.x, p.y, 0f), new Vector3(q.x, q.y, 0f) };
        cv.SetBakedLocalPoints(pts);
        cv.ApplyBakedPathToRenderer();

        connections.Add((cv, parentId, childId));
    }


    private void HandlePrestigeChanged()
    {
        RefreshAll();
    }

    public void RefreshAll()
    {
        // 1) Refresh nodes
        foreach (var kv in nodeViews)
            kv.Value.RefreshVisual();

        // 2) Refresh lines per rule:
        // - Full Alpha if parent is unlocked (owned).
        // - If child missing exactly one dependency -> greyed.
        // - If child missing more than one dependency -> black & faded.
        var pm = PrestigeManager.Instance;
        if (pm == null) return;

        foreach (var c in connections)
        {
            // If already owned, treat as fully active.
            bool owned = pm.Owns(c.childId);

            bool canBuy = false;
            if (!owned)
                canBuy = pm.CanBuy(c.childId, out _);

            if (owned || canBuy)
            {
                // Available => black full alpha
                c.cv.SetColor(new Color(0f, 0f, 0f, 1f));
            }
            else
            {
                // Not available => grey alpha 0.75
                c.cv.SetColor(new Color(0.5f, 0.5f, 0.5f, 0.75f));
            }
        }
    }

    private static int CountMissingDeps(PrestigeManager pm, PrestigeNodeSO node)
    {
        int missing = 0;

        // All
        if (node.RequiresAll != null && node.RequiresAll.Count > 0)
        {
            for (int i = 0; i < node.RequiresAll.Count; i++)
                if (!pm.Owns(node.RequiresAll[i])) missing++;
        }

        // Any: treat as 1 missing if none are owned
        if (node.RequiresAny != null && node.RequiresAny.Count > 0)
        {
            bool anyOwned = false;
            for (int i = 0; i < node.RequiresAny.Count; i++)
                if (pm.Owns(node.RequiresAny[i])) { anyOwned = true; break; }
            if (!anyOwned) missing++;
        }

        return missing;
    }

    private void HydrateFromScene()
    {
        nodeViews.Clear();
        connections.Clear();

        if (canvasRoot == null) return;

        var views = canvasRoot.GetComponentsInChildren<PrestigeNodeView>(true);
        for (int i = 0; i < views.Length; i++)
        {
            var v = views[i];
            string id = !string.IsNullOrEmpty(v.NodeId) ? v.NodeId : (v.NodeSO ? v.NodeSO.NodeId : null);
            if (string.IsNullOrEmpty(id)) continue;
            if (!nodeViews.ContainsKey(id))
                nodeViews[id] = v;
        }

        var lines = canvasRoot.GetComponentsInChildren<PrestigeConnectionViewLR>(true);
        for (int i = 0; i < lines.Length; i++)
        {
            var lr = lines[i];
            if (string.IsNullOrEmpty(lr.parentNodeId) || string.IsNullOrEmpty(lr.childNodeId)) continue;
            connections.Add((lr, lr.parentNodeId, lr.childNodeId));

            // Ensure baked points are applied (just in case)
            lr.ApplyBakedPathToRenderer();
        }
    }

    // Old BuildIfNeeded mixed nodes+lines creation and always ran.
    // Keep a runtime-only builder but only when toggles are true.
    private void BuildNodesFromTree_Runtime()
    {
        if (tree == null || canvasRoot == null || nodePrefab == null) return;

        foreach (var n in tree.Nodes.Where(n => n != null))
        {
            if (nodeViews.ContainsKey(n.NodeId)) continue; // already present in scene

            var go = Instantiate(nodePrefab, canvasRoot);
            go.name = $"Node_{n.NodeId}";
            var rt = go.transform as RectTransform;
            rt.anchoredPosition = n.AnchoredPosition;

            var view = go.GetComponent<PrestigeNodeView>();
            if (view == null) view = go.AddComponent<PrestigeNodeView>();
            view.NodeSO = n;
            view.Init(n);

            nodeViews[n.NodeId] = view;
        }
    }

    private void BuildConnectionsFromTree_Runtime()
    {
        if (tree == null || canvasRoot == null || linePrefab == null) return;

        // Build only missing connections. These will NOT be baked at runtime.
        foreach (var child in tree.Nodes.Where(n => n != null))
        {
            if (child.RequiresAll != null)
                foreach (var pid in child.RequiresAll)
                    TryMakeConnection_Runtime(pid, child.NodeId);

            if (child.RequiresAny != null)
                foreach (var pid in child.RequiresAny)
                    TryMakeConnection_Runtime(pid, child.NodeId);
        }
    }


    // ---------------------- EDITOR TOOLS ----------------------


#if UNITY_EDITOR

    [ContextMenu("Create All (Rebuild)")]
    public void Editor_CreateAll_Rebuild()
    {
        if (tree == null || canvasRoot == null || nodePrefab == null || linePrefab == null)
        {
            Debug.LogWarning("[PrestigeUI] Missing refs (tree/canvasRoot/nodePrefab/linePrefab).");
            return;
        }

        // Delete existing nodes + connections under canvasRoot
        {
            var existingNodes = canvasRoot.GetComponentsInChildren<PrestigeNodeView>(true);
            for (int i = existingNodes.Length - 1; i >= 0; i--)
            {
                if (existingNodes[i] != null)
                    DestroyImmediate(existingNodes[i].gameObject);
            }

            var existingLines = canvasRoot.GetComponentsInChildren<PrestigeConnectionViewLR>(true);
            for (int i = existingLines.Length - 1; i >= 0; i--)
            {
                if (existingLines[i] != null)
                    DestroyImmediate(existingLines[i].gameObject);
            }
        }

        // 1) Create nodes from tree (needed so we can bake line paths)
        for (int i = 0; i < tree.Nodes.Count; i++)
        {
            var n = tree.Nodes[i];
            if (n == null) continue;

            var go = (GameObject)PrefabUtility.InstantiatePrefab(nodePrefab, canvasRoot);
            if (go == null) go = Instantiate(nodePrefab, canvasRoot);

            go.name = $"Node_{n.NodeId}";

            var rt = go.transform as RectTransform;
            if (rt != null)
                rt.anchoredPosition = n.AnchoredPosition;

            var view = go.GetComponent<PrestigeNodeView>();
            if (view == null) view = go.AddComponent<PrestigeNodeView>();

            view.NodeSO = n;
            view.Init(n);

            EditorUtility.SetDirty(go);
            EditorUtility.SetDirty(view);
        }

        // Refresh maps so connection building can find the node views
        HydrateFromScene();

        // 2) Create connections from RequiresAll + RequiresAny
        var created = new HashSet<string>(); // "parent->child"
        for (int i = 0; i < tree.Nodes.Count; i++)
        {
            var child = tree.Nodes[i];
            if (child == null) continue;

            if (child.RequiresAll != null)
            {
                for (int j = 0; j < child.RequiresAll.Count; j++)
                    Editor_CreateConnectionIfValid(child.RequiresAll[j], child.NodeId, created);
            }

            if (child.RequiresAny != null)
            {
                for (int j = 0; j < child.RequiresAny.Count; j++)
                    Editor_CreateConnectionIfValid(child.RequiresAny[j], child.NodeId, created);
            }
        }

        // 3) Bake / sync everything (positions + line paths)
        Editor_UpdateAll_Sync();

        EditorSceneManager.MarkSceneDirty(gameObject.scene);
        Debug.Log($"[PrestigeUI] Rebuilt all nodes + connections. Nodes={tree.Nodes.Count}, Connections={created.Count}");
    }

    [ContextMenu("Update All (Sync)")]
    public void Editor_UpdateAll_Sync()
    {
        if (tree == null || canvasRoot == null)
        {
            Debug.LogWarning("[PrestigeUI] Missing refs (tree/canvasRoot).");
            return;
        }

        HydrateFromScene();

        // Sync nodes (SO -> view)
        int nodesUpdated = 0;
        for (int i = 0; i < tree.Nodes.Count; i++)
        {
            var n = tree.Nodes[i];
            if (n == null) continue;

            if (!nodeViews.TryGetValue(n.NodeId, out var view) || view == null)
                continue;

            view.NodeSO = n;

            var rt = view.transform as RectTransform;
            if (rt != null)
                rt.anchoredPosition = n.AnchoredPosition;

            view.Init(n);

            EditorUtility.SetDirty(view);
            nodesUpdated++;
        }

        // Sync connections (straight-line baked path)
        int connsUpdated = 0;
        for (int i = 0; i < connections.Count; i++)
        {
            var c = connections[i];
            if (c.cv == null) continue;

            if (!nodeViews.TryGetValue(c.parentId, out var parentView) || parentView == null) continue;
            if (!nodeViews.TryGetValue(c.childId, out var childView) || childView == null) continue;

            var a = parentView.transform as RectTransform;
            var b = childView.transform as RectTransform;

            Vector2 p = LocalPos(a, canvasRoot);
            Vector2 q = LocalPos(b, canvasRoot);

            var pts = new List<Vector3>(2)
            {
                new Vector3(p.x, p.y, 0f),
                new Vector3(q.x, q.y, 0f)
            };

            c.cv.parentNodeId = c.parentId;
            c.cv.childNodeId = c.childId;

            // Ensure conversion space is correct for shifted-positions safety
            c.cv.spaceRoot = canvasRoot;

            c.cv.SetBakedLocalPoints(pts);
            c.cv.ApplyBakedPathToRenderer();

            EditorUtility.SetDirty(c.cv);
            connsUpdated++;
        }

        EditorSceneManager.MarkSceneDirty(gameObject.scene);
        Debug.Log($"[PrestigeUI] Sync complete. NodesUpdated={nodesUpdated}, ConnectionsUpdated={connsUpdated}");
    }

    private void Editor_CreateConnectionIfValid(string parentId, string childId, HashSet<string> created)
    {
        if (string.IsNullOrEmpty(parentId) || string.IsNullOrEmpty(childId))
            return;

        if (!nodeViews.ContainsKey(parentId) || !nodeViews.ContainsKey(childId))
        {
            Debug.LogWarning($"[PrestigeUI] Skipping connection {parentId}->{childId} (missing node view).");
            return;
        }

        var key = parentId + "->" + childId;
        if (created.Contains(key)) return;

        var go = (GameObject)PrefabUtility.InstantiatePrefab(linePrefab, canvasRoot);
        if (go == null) go = Instantiate(linePrefab, canvasRoot);

        go.name = $"Line_{parentId}_to_{childId}";

        var cv = go.GetComponent<PrestigeConnectionViewLR>();
        if (cv == null) cv = go.AddComponent<PrestigeConnectionViewLR>();

        // Make sure RectTransform is sane for UI (full stretch)
        var rt = go.transform as RectTransform;
        if (rt != null)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localRotation = Quaternion.identity;
            rt.localScale = Vector3.one;
        }

        cv.parentNodeId = parentId;
        cv.childNodeId = childId;

        // Important: baked points are in canvasRoot space, and we want correct conversion
        cv.spaceRoot = canvasRoot;

        // Important: ensure connections render behind nodes by hierarchy order
        go.transform.SetAsFirstSibling();

        EditorUtility.SetDirty(go);
        EditorUtility.SetDirty(cv);

        created.Add(key);
    }

#endif

    // ---------------------- LOCAL UI SPACE HELPERS ----------------------
    private static Vector2 LocalPos(RectTransform rt, RectTransform parent)
    {
        // Use the RECT center in WORLD space, then convert to parent-local space.
        // This works with any anchors, pivots, nested transforms, scaling, etc.
        Vector3 worldCenter = rt.TransformPoint(rt.rect.center);
        Vector3 local = parent.InverseTransformPoint(worldCenter);
        return new Vector2(local.x, local.y);
    }
}
