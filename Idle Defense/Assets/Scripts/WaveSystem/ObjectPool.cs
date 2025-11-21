using System.Collections.Generic;
using UnityEngine;

public class ObjectPool
{
    private readonly Dictionary<string, Queue<GameObject>> _pool = new();
    private readonly Transform _poolParent;

    public ObjectPool(Transform poolParent)
    {
        _poolParent = poolParent;
    }

    public GameObject GetObject(string key, GameObject prefab)
    {
        if (_pool.TryGetValue(key, out Queue<GameObject> queue) && queue.Count > 0)
        {
            GameObject obj = queue.Dequeue();
            obj.SetActive(true);
            return obj;
        }

        GameObject newObj = Object.Instantiate(prefab);
        newObj.transform.SetParent(_poolParent);
        return newObj;
    }

    public void ReturnObject(string key, GameObject obj)
    {
        if (!_pool.ContainsKey(key))
            _pool[key] = new Queue<GameObject>();

        obj.SetActive(false);
        _pool[key].Enqueue(obj);
    }

    // --- NEW: memory management ---

    // Destroy every inactive GameObject in the pool and clear all keys.
    public void ClearAll()
    {
        Debug.Log("Clearing all polls");
        foreach (var kv in _pool)
        {
            var q = kv.Value;
            while (q.Count > 0)
            {
                var go = q.Dequeue();
                Debug.Log("Clearing " + go.name);
                if (go != null)
                {
                    Debug.Log("Destroying " + go.name);
                    GameObject.Destroy(go);
                }
            }
        }
        _pool.Clear();
    }

    // Destroy pools for keys not in 'keep'. Keys in 'keep' remain untouched.
    public void ClearAllExcept(HashSet<string> keep)
    {
        // Collect keys to remove (avoid modifying while iterating).
        List<string> toRemove = new List<string>();
        foreach (var key in _pool.Keys)
        {
            if (keep == null || !keep.Contains(key))
                toRemove.Add(key);
        }

        for (int i = 0; i < toRemove.Count; i++)
        {
            var key = toRemove[i];
            var q = _pool[key];
            while (q.Count > 0)
            {
                var go = q.Dequeue();
                if (go != null) Object.Destroy(go);
            }
            _pool.Remove(key);
        }
    }
}
