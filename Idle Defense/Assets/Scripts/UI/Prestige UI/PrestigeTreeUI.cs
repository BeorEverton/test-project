using System.Collections.Generic;
using System.Linq;
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
            var childSO = nodeViews[c.childId].NodeSO;
            int missing = CountMissingDeps(pm, childSO);
            bool parentOwned = pm.Owns(c.parentId);

            if (parentOwned)
            {
                c.cv.SetStateFullAlpha();
            }
            else
            {
                if (missing == 1)
                {
                    c.cv.SetStateGreyed();
                }
                else
                {
                    c.cv.SetStateBlackFaded();
                }
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


    // ---------------------- ROUTE BAKER (EDITOR CONTEXT MENU) ----------------------
    [ContextMenu("Bake Connections")]
    public void Context_BakeConnections()
    {
        if (canvasRoot == null)
        {
            Debug.LogWarning("[PrestigeRoute] Missing canvasRoot.");
            return;
        }

        // Discover nodes and lines under the canvasRoot
        var nodeViewsMap = new Dictionary<string, PrestigeNodeView>();
        var views = canvasRoot.GetComponentsInChildren<PrestigeNodeView>(true);

        foreach (var v in views)
        {
            // Prefer explicit NodeId; fallback to SO id for edit-time safety
            string id = !string.IsNullOrEmpty(v.NodeId)
                ? v.NodeId
                : (v.NodeSO != null ? v.NodeSO.NodeId : null);

            if (string.IsNullOrEmpty(id))
            {
                Debug.LogWarning($"[PrestigeRoute] Skipping node '{v.name}' — missing NodeId (set NodeSO or NodeId).");
                continue;
            }

            if (nodeViewsMap.ContainsKey(id))
            {
                Debug.LogError($"[PrestigeRoute] Duplicate NodeId '{id}' on '{v.name}' and '{nodeViewsMap[id].name}'. " +
                               $"Fix in inspector (NodeSO.NodeId must be unique).");
                continue;
            }

            nodeViewsMap[id] = v;
        }

        var conns = canvasRoot.GetComponentsInChildren<PrestigeConnectionViewLR>(true).ToList();
        if (nodeViewsMap.Count == 0 || conns.Count == 0)
        {
            Debug.LogWarning("[PrestigeRoute] Nothing to bake (no nodes or no connections).");
            return;
        }

        // Settings
        float grid = 32f;
        float padding = 18f;
        float z = 0f;

        // Build rect map for node obstacles (in local space of canvasRoot)
        var nodeRects = new Dictionary<string, Rect>();
        foreach (var kv in nodeViewsMap)
        {
            var rt = kv.Value.GetComponent<RectTransform>();
            Rect r = GetLocalRect(rt, canvasRoot);
            r.xMin -= padding; r.xMax += padding; r.yMin -= padding; r.yMax += padding;
            nodeRects[kv.Key] = r;
        }

        var usedSegments = new List<(Vector2 a, Vector2 b)>();
        int routed = 0, fallback = 0;

        foreach (var c in conns)
        {
            if (string.IsNullOrEmpty(c.parentNodeId) || string.IsNullOrEmpty(c.childNodeId)) continue;
            if (!nodeViewsMap.ContainsKey(c.parentNodeId) || !nodeViewsMap.ContainsKey(c.childNodeId)) continue;

            Vector2 p = LocalPos(nodeViewsMap[c.parentNodeId].transform as RectTransform, canvasRoot);
            Vector2 q = LocalPos(nodeViewsMap[c.childNodeId].transform as RectTransform, canvasRoot);

            p = Snap(p, grid);
            q = Snap(q, grid);

            // Try straight
            var straight = new List<Vector2> { p, q };
            if (!Intersects(straight, nodeRects, usedSegments))
            {
                SetConn(c, straight, z);
                AddSegments(usedSegments, straight);
                routed++;
                continue;
            }

            // Try 1-bend (L)
            Vector2 bend1 = Snap(new Vector2(p.x, q.y), grid);
            Vector2 bend2 = Snap(new Vector2(q.x, p.y), grid);

            var L1 = new List<Vector2> { p, bend1, q };
            if (!Intersects(L1, nodeRects, usedSegments))
            {
                SetConn(c, L1, z);
                AddSegments(usedSegments, L1);
                routed++;
                continue;
            }

            var L2 = new List<Vector2> { p, bend2, q };
            if (!Intersects(L2, nodeRects, usedSegments))
            {
                SetConn(c, L2, z);
                AddSegments(usedSegments, L2);
                routed++;
                continue;
            }

            // Try 2-bend with a few midY probes
            float yMin = Mathf.Min(p.y, q.y), yMax = Mathf.Max(p.y, q.y);
            bool success = false;
            for (int i = 1; i <= 6 && !success; i++)
            {
                float midY = Snap(yMin + (yMax - yMin) * (i / 7f), grid);
                var poly = new List<Vector2> { p, new Vector2(p.x, midY), new Vector2(q.x, midY), q };
                if (!Intersects(poly, nodeRects, usedSegments))
                {
                    SetConn(c, poly, z);
                    AddSegments(usedSegments, poly);
                    routed++;
                    success = true;
                }
            }

            if (!success)
            {
                // Fallback: straight (you can hand-adjust later)
                SetConn(c, straight, z);
                AddSegments(usedSegments, straight);
                fallback++;
            }
        }

#if UNITY_EDITOR
        // Mark scene dirty so baked points persist
        foreach (var c in conns)
            UnityEditor.EditorUtility.SetDirty(c);
#endif

        Debug.Log($"[PrestigeRoute] Bake complete. Routed={routed} Fallbacks={fallback}");
    }

    [ContextMenu("Build Nodes From Tree")]
    public void Context_BuildNodesFromTree()
    {
        if (tree == null || canvasRoot == null || nodePrefab == null)
        {
            Debug.LogWarning("[PrestigeUI] Missing refs (tree/canvasRoot/nodePrefab).");
            return;
        }

        // Clear existing node views? (comment out if you want to keep existing)
        // foreach (var v in canvasRoot.GetComponentsInChildren<PrestigeNodeView>(true)) DestroyImmediate(v.gameObject);

        foreach (var n in tree.Nodes)
        {
            if (n == null) continue;
            // Skip if already exists
            var exists = FindNodeViewById(n.NodeId);
            if (exists != null) continue;

            var go = Instantiate(nodePrefab, canvasRoot);
            go.name = $"Node_{n.NodeId}";
            var rt = go.transform as RectTransform;
            rt.anchoredPosition = n.AnchoredPosition;

            var view = go.GetComponent<PrestigeNodeView>();
            if (view == null) view = go.AddComponent<PrestigeNodeView>();
            view.NodeSO = n;                // OnValidate will copy NodeId and Icon
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(view);
#endif
        }

        Debug.Log("[PrestigeUI] Nodes built from tree.");
    }

    [ContextMenu("Create Missing Connections")]
    public void Context_CreateMissingConnections()
    {
        if (tree == null || canvasRoot == null || linePrefab == null)
        {
            Debug.LogWarning("[PrestigeUI] Missing refs (tree/canvasRoot/linePrefab).");
            return;
        }

        // Map existing lines
        var lines = new HashSet<string>(); // "parent->child"
        foreach (var lr in canvasRoot.GetComponentsInChildren<PrestigeConnectionViewLR>(true))
            lines.Add($"{lr.parentNodeId}->{lr.childNodeId}");

        // Helper to spawn a line
        void MakeLine(string parentId, string childId)
        {
            var key = $"{parentId}->{childId}";
            if (lines.Contains(key)) return;

            var go = Instantiate(linePrefab, canvasRoot);
            go.name = $"Line_{parentId}_to_{childId}";
            var cv = go.GetComponent<PrestigeConnectionViewLR>();
            if (cv == null) cv = go.AddComponent<PrestigeConnectionViewLR>();
            cv.parentNodeId = parentId;
            cv.childNodeId = childId;
            lines.Add(key);
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(cv);
#endif
        }

        foreach (var child in tree.Nodes)
        {
            if (child == null) continue;
            if (child.RequiresAll != null)
                foreach (var pid in child.RequiresAll)
                    MakeLine(pid, child.NodeId);

            if (child.RequiresAny != null)
                foreach (var pid in child.RequiresAny)
                    MakeLine(pid, child.NodeId);
        }

        Debug.Log("[PrestigeUI] Missing connections created.");
    }

    [ContextMenu("Validate Setup")]
    public void Context_ValidateSetup()
    {
        var nodes = canvasRoot != null ? canvasRoot.GetComponentsInChildren<PrestigeNodeView>(true) : null;
        var lines = canvasRoot != null ? canvasRoot.GetComponentsInChildren<PrestigeConnectionViewLR>(true) : null;

        int nodesOk = 0, nodesMissingId = 0;
        var seen = new HashSet<string>();

        if (nodes != null)
        {
            foreach (var v in nodes)
            {
                string id = !string.IsNullOrEmpty(v.NodeId) ? v.NodeId : (v.NodeSO ? v.NodeSO.NodeId : null);
                if (string.IsNullOrEmpty(id)) { nodesMissingId++; continue; }
                if (seen.Contains(id)) Debug.LogError($"[PrestigeUI] Duplicate NodeId '{id}' on {v.name}");
                else { seen.Add(id); nodesOk++; }
            }
        }

        int linesOk = 0, linesBad = 0;
        if (lines != null)
        {
            foreach (var l in lines)
            {
                if (string.IsNullOrEmpty(l.parentNodeId) || string.IsNullOrEmpty(l.childNodeId))
                {
                    linesBad++;
                    Debug.LogWarning($"[PrestigeUI] Line '{l.name}' missing IDs. parent='{l.parentNodeId}', child='{l.childNodeId}'");
                    continue;
                }

                bool parentOk = seen.Contains(l.parentNodeId);
                bool childOk = seen.Contains(l.childNodeId);

                if (!parentOk || !childOk)
                {
                    linesBad++;
                    Debug.LogWarning($"[PrestigeUI] Line '{l.name}' bad refs: parentOk={parentOk}({l.parentNodeId}) " +
                                     $"childOk={childOk}({l.childNodeId}). " +
                                     $"Ensure both nodes exist in this canvas (Build Nodes From Tree) and IDs match NodeSO.NodeId.");
                    continue;
                }
                linesOk++;
            }

        }

        Debug.Log($"[PrestigeUI] Validate: Nodes OK={nodesOk}, MissingId={nodesMissingId}. Lines OK={linesOk}, Bad={linesBad}.");
    }

    private PrestigeNodeView FindNodeViewById(string nodeId)
    {
        if (string.IsNullOrEmpty(nodeId) || canvasRoot == null) return null;
        var views = canvasRoot.GetComponentsInChildren<PrestigeNodeView>(true);
        for (int i = 0; i < views.Length; i++)
        {
            var v = views[i];
            var id = !string.IsNullOrEmpty(v.NodeId) ? v.NodeId : (v.NodeSO ? v.NodeSO.NodeId : null);
            if (id == nodeId) return v;
        }
        return null;
    }

    // ---------------------- Geom helpers (runtime-safe) ----------------------
    private static Rect GetLocalRect(RectTransform rt, RectTransform parent)
    {
        // anchoredPosition is relative to the parent; rect.size/pivot give us the box
        Vector2 size = rt.rect.size;
        Vector2 pivot = rt.pivot;
        Vector2 pos = rt.anchoredPosition;        // << key change
        Vector2 min = pos - Vector2.Scale(size, pivot);
        return new Rect(min, size);
    }

    private static Vector2 LocalPos(RectTransform rt, RectTransform parent)
    {
        // Keep it simple and stable for UI: use anchoredPosition directly.
        return rt.anchoredPosition;               // << key change
    }


    private static Vector2 Snap(Vector2 p, float g) => new Vector2(Mathf.Round(p.x / g) * g, Mathf.Round(p.y / g) * g);
    private static float Snap(float v, float g) => Mathf.Round(v / g) * g;

    private static void SetConn(PrestigeConnectionViewLR c, List<Vector2> pts, float z)
    {
        // Ensure line object's local origin doesn't add offsets
        var rt = c.transform as RectTransform;
        if (rt != null)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition3D = Vector3.zero;     // << important
            rt.localRotation = Quaternion.identity;
            rt.localScale = Vector3.one;
        }

        var local3 = new List<Vector3>(pts.Count);
        for (int i = 0; i < pts.Count; i++)
            local3.Add(new Vector3(pts[i].x, pts[i].y, z));

        c.SetBakedLocalPoints(local3);
    }


    private static void AddSegments(List<(Vector2, Vector2)> used, List<Vector2> poly)
    {
        for (int i = 0; i < poly.Count - 1; i++)
            used.Add((poly[i], poly[i + 1]));
    }

    private static bool Intersects(List<Vector2> poly, Dictionary<string, Rect> rects, List<(Vector2, Vector2)> usedSegs)
    {
        // Against node rects
        for (int i = 0; i < poly.Count - 1; i++)
        {
            var a = poly[i]; var b = poly[i + 1];
            foreach (var r in rects.Values)
                if (SegmentRectIntersect(a, b, r)) return true;
        }
        // Against already used segments
        for (int i = 0; i < poly.Count - 1; i++)
        {
            var a = poly[i]; var b = poly[i + 1];
            for (int j = 0; j < usedSegs.Count; j++)
                if (SegmentsIntersect(a, b, usedSegs[j].Item1, usedSegs[j].Item2)) return true;
        }
        return false;
    }

    private static bool SegmentRectIntersect(Vector2 a, Vector2 b, Rect r)
    {
        if (r.Contains(a) || r.Contains(b)) return true;
        var ca = new Vector2(r.xMin, r.yMin);
        var cb = new Vector2(r.xMax, r.yMin);
        var cc = new Vector2(r.xMax, r.yMax);
        var cd = new Vector2(r.xMin, r.yMax);
        if (SegmentsIntersect(a, b, ca, cb)) return true;
        if (SegmentsIntersect(a, b, cb, cc)) return true;
        if (SegmentsIntersect(a, b, cc, cd)) return true;
        if (SegmentsIntersect(a, b, cd, ca)) return true;
        return false;
    }

    private static bool SegmentsIntersect(Vector2 p, Vector2 p2, Vector2 q, Vector2 q2)
    {
        float o1 = Orient(p, p2, q);
        float o2 = Orient(p, p2, q2);
        float o3 = Orient(q, q2, p);
        float o4 = Orient(q, q2, p2);

        if (o1 * o2 < 0f && o3 * o4 < 0f) return true;

        if (o1 == 0 && OnSegment(p, q, p2)) return true;
        if (o2 == 0 && OnSegment(p, q2, p2)) return true;
        if (o3 == 0 && OnSegment(q, p, q2)) return true;
        if (o4 == 0 && OnSegment(q, p2, q2)) return true;

        return false;
    }

    private static float Orient(Vector2 a, Vector2 b, Vector2 c)
    {
        return (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);
    }

    private static bool OnSegment(Vector2 a, Vector2 b, Vector2 c)
    {
        return Mathf.Min(a.x, c.x) <= b.x && b.x <= Mathf.Max(a.x, c.x) &&
               Mathf.Min(a.y, c.y) <= b.y && b.y <= Mathf.Max(a.y, c.y);
    }

}
