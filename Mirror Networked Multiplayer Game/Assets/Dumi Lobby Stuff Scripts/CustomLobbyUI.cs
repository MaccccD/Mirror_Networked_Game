using UnityEngine;
using Mirror;
using kcp2k;
using TMPro;
// Dumi : what we are doing here is replacing the network manager hud with a dynamic UI , that way we dont have to have the HUd component or have to use it .
//this is basically another way to set up the client /server and host syncing.
public class CustomLobbyUI : MonoBehaviour
{
    public TMP_InputField ipInputField;
    public TMP_InputField portInputField;
    public TMP_InputField usernameInputField;
    public GameObject lobbyPanel;
    public GameObject endgamePanel;
    public GameObject startPanel;
    public GameObject chatBtn;


    //references to Mirror's NetworkManager and Transport 
    public NetworkManager networkManager;
    public KcpTransport transport;

    // Called when the player clicks the "Host" button
    // Starts both the server and the local client

    
    public static class PlayerInfo
    {
        public static string Username = "";
    }
    public void OnClickHost()
    {
        SetNetworkAddress(); // Set the IP/port before connecting
        SetUsername();
        networkManager.StartHost();
        OnConnected();
        startPanel.gameObject.SetActive(true);
        chatBtn.gameObject.SetActive(true);
    }

    // Called when the player clicks the "Server" button - starts a dedicated server (no client)
    public void OnClickServer()
    {
        SetNetworkAddress();
        SetUsername();
        networkManager.StartServer();
        OnConnected();
        startPanel.gameObject.SetActive(true);
        chatBtn.gameObject.SetActive(true);
    }

    // Called when the player clicks the "Client" button - connects to a server at the given IP and port
    public void OnClickClient()
    {
        SetNetworkAddress();
        SetUsername();
        networkManager.StartClient(); // Start client only
        OnConnected();
        startPanel.gameObject.SetActive(true);
        chatBtn.gameObject.SetActive(true);
    }

    // Sets the network address and port based on the user's input - this is called before connecting (host, client, or server)
    private void SetNetworkAddress()
    {
        // If the IP input field is not empty, update the network address with the value
        if (!string.IsNullOrEmpty(ipInputField.text))
            networkManager.networkAddress = ipInputField.text; //Dumi: so we get the string that the person has put into field as their ip address 

        // Try to convert the port input (string) into a number if successful, set it on the transport component
        if (ushort.TryParse(portInputField.text, out ushort port)) // Dumi : ushort == a diff type of integer or whole number. it an be longer. signed(positive number) gives you 32 bits. can be super long. Unsigned (a positive and negative number )
            transport.port = port; //  Dumi : ushort can never be a negative number
    }
    private void SetUsername()
    {
        //Dumi: here im setting the usernames the player(s) joins as:
        if (!string.IsNullOrWhiteSpace(usernameInputField.text))
        {
            PlayerInfo.Username = usernameInputField.text;
            Debug.Log("Username set to: " + PlayerInfo.Username);
        }
    }
    private void OnConnected()
    {
        Debug.Log("Connected — hide lobby UI");
        lobbyPanel.SetActive(false); // when we are hidng the panel when both players are connected since they would be done using the panel
    }
  
    public  void OnEndGame()// Dumi : Here I'm making players disconnect from the networked game by resetting their connections and taking them back to the lobby scree to connect or restart the game.
    {
        if (NetworkServer.active && NetworkClient.isConnected)
        {
            networkManager.StopHost();
            Debug.Log("The host has been disconnected");
        }

        else if (NetworkClient.isConnected)
        {
            networkManager.StopClient();
            Debug.Log("the client has been disconnected");
        }

        else if (NetworkServer.active)
        {
            networkManager.StopServer();
            Debug.Log("the server has been disconnected");
        }

        lobbyPanel.SetActive(true); // D: take pkayers back to lobby so they can restart and connected again over the network
        endgamePanel.SetActive(false); //D: close the end game panel
        startPanel.SetActive(false);
        chatBtn.SetActive(false);
        Debug.Log("Restart screen back to lobby executed successfullyyy, yayyy!");
    }

    public void ExitGame()
    {
        Application.Quit();
    }
}