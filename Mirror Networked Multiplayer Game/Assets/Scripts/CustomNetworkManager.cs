using UnityEngine;
using Mirror;
using System.Collections.Generic;
public class CustomNetworkManager : NetworkManager
{
    [Header("Player Prefabs")]
    public List<GameObject> playerPrefabs = new List<GameObject>(); // Drag all your player prefabs here

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        // Safety check
        if (playerPrefabs.Count == 0)
        {
            Debug.LogError("No players assigned!");
            return;
        }

        // int index = Random.Range(0, playerPrefabs.Count); //for assigning randomly

        // Example logic: assign based on player index
        //What will i
        int index = numPlayers % playerPrefabs.Count; // get an index from the remainder from how many pl;ayers are in the list. numPlayers is players connected who have jouned.
        //then we assign that remainder index

        GameObject selectedPrefab = playerPrefabs[index]; // can have as many player prefabs and it will always be dynamic

        GameObject player = Instantiate(selectedPrefab); // instantiated based on the remainder so for e.g if there is 3 players, the ramainders.
        NetworkServer.AddPlayerForConnection(conn, player); // then you add it to the server
    }
}
