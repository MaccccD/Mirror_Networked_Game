using System.Collections;
using Mirror;
using UnityEngine;

//Eden: This script automatically assigns players as either host or client depending on who runs game first

public class CustomNetworkManager : NetworkManager
{
    [Header("Auto‑Start Settings")]
    [Tooltip("How many seconds to wait for a host before starting one.")]
    public float clientConnectTimeout = 1.0f;

    /*/void Start()
    {
        //Eden: Begin the auto‑start logic as game starts
        StartCoroutine(AutoStart());
    }

    IEnumerator AutoStart()
    {
        //Eden: 1. Attempt to join as client
        Debug.Log("[AutoStart] Trying to connect as client...");
        StartClient();

        float startTime = Time.time;
        //Eden: 2. Wait until either we connect or timeout elapses
        while (Time.time < startTime + clientConnectTimeout)
        {
            if (NetworkClient.isConnected)
            {
                Debug.Log("[AutoStart] Successfully connected as client.");
                yield break;
            }
            yield return null;
        }

        //Eden: 3. Timeout expired and still not connected, therefore become host
        Debug.Log("[AutoStart] No host found—starting as host.");
        StartHost();
    }*/

    //Eden: logs when client disconnects from server
    public override void OnClientDisconnect()
    {
        if (!NetworkClient.isConnected)
            Debug.Log("[AutoStart] Disconnected as client.");
    }
}
