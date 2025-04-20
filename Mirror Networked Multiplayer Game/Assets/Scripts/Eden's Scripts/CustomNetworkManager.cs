using System.Collections;
using Mirror;
using UnityEngine;

public class CustomNetworkManager : NetworkManager
{
    [Header("Auto‑Start Settings")]
    [Tooltip("How many seconds to wait for a host before starting one.")]
    public float clientConnectTimeout = 1.0f;

    void Start()
    {
        // Begin the auto‑start coroutine on launch
        StartCoroutine(AutoStart());
    }

    IEnumerator AutoStart()
    {
        // 1. Attempt to join as client
        Debug.Log("[AutoStart] Trying to connect as client...");
        StartClient();

        float startTime = Time.time;
        // 2. Wait until either we connect or timeout elapses
        while (Time.time < startTime + clientConnectTimeout)
        {
            if (NetworkClient.isConnected)
            {
                Debug.Log("[AutoStart] Successfully connected as client.");
                yield break;
            }
            yield return null;
        }

        // 3. Timeout expired and still not connected → become host
        Debug.Log("[AutoStart] No host found—starting as host.");
        StartHost();
    }

    // Optional: log success or failures
    public override void OnClientDisconnect()
    {
        if (!NetworkClient.isConnected)
            Debug.Log("[AutoStart] Disconnected as client.");
    }
}
