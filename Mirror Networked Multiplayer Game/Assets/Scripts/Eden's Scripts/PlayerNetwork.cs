using Mirror;

//Eden: This script ensures that the start screen automatically shown (even if other player has
//not joined yet. Locally shows UI

public class PlayerNetwork : NetworkBehaviour
{
    //Eden: This function called on local client when player object set up (by mirror)
    public override void OnStartLocalPlayer()
    {
        UIManager.Instance.ShowStartUI();
    }

    [Command]
    public void CmdTellServerToPressStart()
    {
        GameSessionManager.Instance.CmdPressStart();
    }
}
