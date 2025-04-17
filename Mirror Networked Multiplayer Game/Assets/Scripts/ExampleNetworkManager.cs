using UnityEngine;
using Mirror;
public class ExampleNetworkManager: NetworkManager
{
    // Called when a player joins the game
    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        base.OnServerAddPlayer(conn); // call base method that spawns the player prefab

        GameObject player = conn.identity.gameObject; //get the player game object from its ID
        var playerScript = player.GetComponent<Example>(); //get the exmaple script on the player prefab
        if (numPlayers == 1) //the very first player to join the game, numPlayer is a property from the Network Manager script from Mirror
        {
            // First player (usually the host)
            playerScript.TargetShowWelcomeMessage(conn, "You are the host! Welcome to the game.");
        }
        else

        {
            // Any player after that
            playerScript.TargetShowWelcomeMessage(conn, "You are a client. Get ready to play!");

        }
    }
}
