using UnityEngine;

public class DebugHelper : MonoBehaviour
{
    private void OnEnable()
    {
        Debug.Log("DebugHelper enabled");
    }

    private void OnDisable()
    {
        Debug.Log("DebugHelper disabled");
    }

    private void OnDestroy()
    {
        Debug.Log("DebugHelper destroyed");
    }
}
