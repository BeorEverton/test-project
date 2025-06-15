using UnityEngine;
using ByteBrewSDK;
using System.Collections.Generic;

public class AnalyticsManager : MonoBehaviour
{    public static AnalyticsManager Instance { get; private set; }
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            if (ByteBrew.IsInitilized)
            {
                Debug.Log("ByteBrew is already initialized.");
                return;
            }
            // Initialize ByteBrew SDK
            ByteBrew.InitializeByteBrew();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /*var mapCheckpointParameters = new Dictionary<string, string>()
    {
        { "earned", "KARMA" },
        { "amount", "250" },
        { "character", "Hunterwizard" },
        { "selectedPowerup", "ExtraArrow" },
        { "level", "32" },
        { "runDeathCount", "2" }
    };
    ByteBrew.NewCustomEvent("MapCheckPointHit", mapCheckpointParameters);*/

    public void SendCustomEvent(string eventName, Dictionary<string, string> parameters)
    {
        if (ByteBrew.IsInitilized)
        {
            ByteBrew.NewCustomEvent(eventName, parameters);
        }
        else
        {
            Debug.LogWarning("ByteBrew is not initialized. Cannot send custom event.");
        }
    }

}
