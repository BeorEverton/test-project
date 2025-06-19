using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebrisPool : MonoBehaviour
{
    public static DebrisPool Instance { get; private set; }

    [SerializeField] private GameObject particlePrefab;
    [SerializeField] private int initialPoolSize = 30;

    private readonly Queue<GameObject> pool = new();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        for (int i = 0; i < initialPoolSize; i++)
        {
            GameObject obj = Instantiate(particlePrefab, transform);
            obj.SetActive(false);
            pool.Enqueue(obj);
        }
    }

    public void Play(Vector3 position)
    {
        GameObject obj = pool.Count > 0 ? pool.Dequeue() : Instantiate(particlePrefab, transform);
        obj.transform.position = position;
        obj.SetActive(true);

        var ps = obj.GetComponent<ParticleSystem>();
        ps.Play();

        StartCoroutine(ReturnAfterLifetime(obj, ps.main.duration + ps.main.startLifetime.constantMax));
    }

    private IEnumerator ReturnAfterLifetime(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        obj.SetActive(false);
        pool.Enqueue(obj);
    }
}
