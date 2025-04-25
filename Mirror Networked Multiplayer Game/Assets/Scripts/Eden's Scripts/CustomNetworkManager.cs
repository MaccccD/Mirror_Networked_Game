using System.Collections;
using Mirror;
using UnityEngine;

//Eden: This script automatically assigns players as either host or client depending on who runs game first

public class CustomNetworkManager : NetworkManager
{
    [Header("Auto‑Start Settings")]
    [Tooltip("How many seconds to wait for a host before starting one.")]
    public float clientConnectTimeout = 1.0f;
    //Dumi: the game session script is going be spanwed as a networked object  bc initially , with the new IP address /port set up, it was not registering the active client.
    public GameObject gameSessionManagerPrefab;

    public override void OnServerAddPlayer(NetworkConnectionToClient conn) //Dumi// dynamically spawning the GSM as a networked prefab so that all the lofic there can run and it registering the active clients and server.
    {
       
        if (GameSessionManager.Instance == null)
        {
            Debug.Log(" No gsm yet  but its getting  assigned!");
            GameObject gsm = Instantiate(gameSessionManagerPrefab);
            NetworkServer.Spawn(gsm); // Dumi : this will ensure that gsm is networked and all clients can call it as an instance
        }
        else
        {
            Debug.Log("GSM already spawned!!!");
        }
    }

    void Start()
    {
        //Eden: Begin the auto‑start logic as game starts
      //  StartCoroutine(AutoStart()); Dumi change: i commented this out bc since  this automatically assign the roles based on who runs the game first,  that is  not how we are supposed to set up the networking.
      //24/04/2025 Update : Players now use the Custom Lobby UI script where they input the unique IP address and port number and then if they match (verified by the Network Address) , it allows them to have an option to choose to be host , server or client.
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
    }

    //Eden: logs when client disconnects from server
    public override void OnClientDisconnect()
    {
        if (!NetworkClient.isConnected)
            Debug.Log("[AutoStart] Disconnected as client.");
    }
}
