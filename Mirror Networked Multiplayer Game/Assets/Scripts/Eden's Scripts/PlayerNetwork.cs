using Mirror;

//Eden: This script ensures that the start screen automatically shown (even if other player has
//not joined yet. Locally shows UI

public class PlayerNetwork : NetworkBehaviour
{
    [SyncVar] //Dumi: allowing players to have usernmes that are sunced acriss the netwwork for the chat functionality
    public string username; 
    //Eden: This function called on local client when player object set up (by mirror)
    public override void OnStartLocalPlayer()
    {
        UIManager.Instance.ShowStartUI();
        //Dumi : grab the username from the local player
        CmdSetUsername(CustomLobbyUI.PlayerInfo.Username);
    }


    [Command]
    void CmdSetUsername(string _username)
    {
        username = _username;
        
    }

    [Command]
    public void CmdSendMessage(string message)
    {
        string formattedMessage = $"{username}: {message}";
        RpcReceiveMessage(formattedMessage);
    }

    [ClientRpc]
    void RpcReceiveMessage(string formattedMessage)
    {
        // Find and update the Chat UI
        var chatUI = FindObjectOfType<ChattyUI>(true);
        if (chatUI != null)
        {
            chatUI.AppendMessage(formattedMessage);
        }
    }
}
