using UnityEngine;
using Mirror;
using kcp2k;
using TMPro;
// what we are doing here is replacing the network manager hud with a dynamic UI , that way we dont have to have the HUd component or have to use it . this is basically another way to set up the client /server and host syncing.
public class CustomLobbyUI : MonoBehaviour
{
    public TMP_InputField ipInputField;
    public TMP_InputField portInputField;
    public GameObject lobbyPanel;

    //references to Mirror's NetworkManager and Transport 
    public NetworkManager networkManager;
    public KcpTransport transport;

    // Called when the player clicks the "Host" button
    // Starts both the server and the local client
    public void OnClickHost()
    {
        SetNetworkAddress(); // Set the IP/port before connecting
        networkManager.StartHost();
        OnConnected();
    }

    // Called when the player clicks the "Server" button - starts a dedicated server (no client)
    public void OnClickServer()
    {
        SetNetworkAddress();
        networkManager.StartServer();
        OnConnected();
    }

    // Called when the player clicks the "Client" button - connects to a server at the given IP and port
    public void OnClickClient()
    {
        SetNetworkAddress();
        networkManager.StartClient(); // Start client only
        OnConnected();
    }

    // Sets the network address and port based on the user's input - this is called before connecting (host, client, or server)
    private void SetNetworkAddress()
    {
        // If the IP input field is not empty, update the network address with the value
        if (!string.IsNullOrEmpty(ipInputField.text))
            networkManager.networkAddress = ipInputField.text; //so we get the string that the person has put into field as their ip address 

        // Try to convert the port input (string) into a number if successful, set it on the transport component
        if (ushort.TryParse(portInputField.text, out ushort port)) //ushort == a diff type of integer or whole number. it an be longer. signed(positive number) gives you 32 bits. can be super long. Unsigned (a positive and negative number )
            transport.port = port; // ushort can never be a neagtive number
    }

    private void OnConnected()
    {
        Debug.Log("Connected — hide lobby UI");
        lobbyPanel.SetActive(false); // when we are hidng the panel when both players are connected since they would be done using the panel
    } 

}
