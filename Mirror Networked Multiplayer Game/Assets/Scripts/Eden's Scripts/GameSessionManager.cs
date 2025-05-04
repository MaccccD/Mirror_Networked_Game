using UnityEngine;
using Mirror;

/*Eden: This script coordinates multiplayer session flow:
 * 1. Tracks the first start click
 * 2. Records first player's role choice and disables that option for both players 
 * 3. When the second choice is received, both clients go into their appropriate screens*/
public class GameSessionManager : NetworkBehaviour
{
    //Eden: Singleton setup for easy access from UIManager
    public static GameSessionManager Instance { get; private set; }

    //Eden: SyncVar detects first start click fm any client
    [SyncVar] private bool firstPressedStart = false;

    [SyncVar(hook = nameof(OnPuzzle1SolvedChanged))]
    private bool puzzle1Solved = false;

    //Eden: SyncVar for role selection, hooks OnFirstRoleChoiceChanged() on clients
    [SyncVar(hook = nameof(OnFirstRoleChoiceChanged))]
    private string firstRoleChoice = "";

    //Eden: Exposed for UI logic
    public bool HasFirstPressedStart => firstPressedStart;
    public string FirstRoleChoice => firstRoleChoice;

    [SyncVar(hook = nameof(OnPuzzle2SolvedChanged))]
    private bool puzzle2Solved = false;

    //Dumi// Calling the narrative manager here to enable the sync of the narrative between the host and the clients.
    public NarrativeManager narrativeManager;

    void Awake()
    {
        //Eden: Singleton setup ensures only one GameSessionManager exists at any time
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        Instance = this;
    }

    /*Eden: This is called when either player clicks start
     * First click: sets firstPressedStart to true
     * Second click: calls RpcBeginStory() for all clients to go to story panels*/
    [Command(requiresAuthority = false)]
    public void CmdPressStart()
    {
        if (!firstPressedStart)
        {
            //Eden: First click among all clients
            firstPressedStart = true;
        }
        else
        {
            //Eden: Second click, tell everyone to enter the story
            RpcBeginStory();
        }
    }

    //Eden: Rpc to all clients to hide the waiting UI and show the story panel
    [ClientRpc]
    void RpcBeginStory()
    {
        UIManager.Instance.EnterStory();
        narrativeManager.OnStartClient(); //Dumi:// start the narrative for both the host and the clients

    }

    /*Eden: This is called by a client when they choose either bomb player or office player
     * if firstRoleChoice is empty (nobody selected), it will record the selection as the first choice
     * else if the pick is the alternate, invoke RpcBothPlayersChosen()*/
    [Command(requiresAuthority = false)]
    public void CmdSelectRole(string role)
    {
        if (string.IsNullOrEmpty(firstRoleChoice))
        {
            // record the first choice
            firstRoleChoice = role;
            Debug.Log($"[Server] firstRoleChoice set to '{role}'");
        }
        else if (role != firstRoleChoice)
        {
            // second pick → notify both clients that selection is done
            Debug.Log($"[Server] second pick = '{role}', both chosen");
            RpcBothPlayersChosen();
        }
        //Eden: If same role twice, ignore
    }

    /*Eden: Hook that runs on every client when firstRoleChoice changes on server
     * Disables the same button on each client to ensure players select diff roles*/
    void OnFirstRoleChoiceChanged(string oldChoice, string newChoice)
    {
        UIManager.Instance.OnFirstRoleChosen(newChoice);
    }

    //Eden: Rpc to all clients indicating both players chosen, triggers appropriate screens for each player
    [ClientRpc]
    void RpcBothPlayersChosen()
    {
        UIManager.Instance.OnBothPlayersChosen();
    }

    [Command(requiresAuthority = false)]
    public void CmdPuzzle1Complete()
    {
        if (puzzle1Solved) return;
        puzzle1Solved = true;
        Debug.Log("[Server] Puzzle 1 complete!");
    }

    //Eden: Hook fired on each client when puzzle1Solved changes
    void OnPuzzle1SolvedChanged(bool oldVal, bool newVal)
    {
        if (newVal)
            RpcPuzzle1Complete();
    }

    [ClientRpc]
    void RpcPuzzle1Complete()
    {
        Debug.Log("[Client] Puzzle 1 RPC fired!");
        UIManager.Instance.OnPuzzle1Complete();
    }

    [Command(requiresAuthority = false)]
    public void CmdAttemptCut(string wireColor)
    {
        if (!isServer) return;

        if (puzzle2Solved) return;

        if (wireColor == "Red")
        {
            puzzle2Solved = true;
            Debug.Log("[Server] Puzzle solved!");
            RpcShowWin();
        }
        else
        {
            Debug.Log($"[Server] Wrong wire '{wireColor}'");
            RpcScreenShake();
        }
    }


    //Eden: Hook fired on each client when puzzle2Solved changes
    void OnPuzzle2SolvedChanged(bool oldVal, bool newVal)
    {
        if (newVal)
            UIManager.Instance.DisableWireButtons();
           UIManager.Instance.riddleContainer.gameObject.SetActive(true); //Dumi: for debugging purposes , im ensuring that the riddle shows up after the second puzzle is solved
    }

    [ClientRpc]
    void RpcScreenShake()
    {
        UIManager.Instance.ShakeUI();
    }

    [ClientRpc]
    void RpcShowWin()
    {
        UIManager.Instance.ShowWinPanel();
    }
}
