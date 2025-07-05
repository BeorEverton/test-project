using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.WaveSystem
{
    public class ObjectPool
    {
        private Dictionary<string, Queue<GameObject>> _pool = new();
        private Transform _poolParent;

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
            {
                _pool[key] = new Queue<GameObject>();
            }

            obj.SetActive(false);
            _pool[key].Enqueue(obj);
        }

        public void Prewarm(string key, GameObject prefab, int count)
        {
            if (!_pool.ContainsKey(key))
            {
                _pool[key] = new Queue<GameObject>();
            }

            for (int i = 0; i < count; i++)
            {
                GameObject obj = Object.Instantiate(prefab);
                obj.SetActive(false);
                obj.transform.SetParent(_poolParent);
                _pool[key].Enqueue(obj);
            }
        }

    }
}