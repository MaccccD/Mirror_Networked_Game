using Mirror;

public class PlayerNetwork : NetworkBehaviour
{
    // Runs on each client as soon as their player object is ready
    public override void OnStartLocalPlayer()
    {
        // Bring up the Start screen
        UIManager.Instance.ShowStartUI();
    }
}
