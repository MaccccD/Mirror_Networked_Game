using UnityEngine;
using Mirror;
using System.Collections.Generic;
using System.Linq;


/*Eden: This script coordinates multiplayer session flow and ALL puzzle logic:
 *1.Tracks the first start click
 * 2. Records first player's role choice and disables that option for both players 
 * 3. When the second choice is received, both clients go into their appropriate screens
 */

//Dumi: Consolidated session manager with all puzzle logic integrated. Handles ALL puzzle logic from Act 1 to Act 4

public class GameSessionManager : NetworkBehaviour
{
    //Eden: Singleton setup for easy access from UIManager
    public static GameSessionManager Instance { get; private set; }

    //Eden: SyncVar detects first start click fm any client
    [SyncVar] private bool firstPressedStart = false;

    //Eden: SyncVar for role selection, hooks OnFirstRoleChoiceChanged() on clients
    [SyncVar(hook = nameof(OnFirstRoleChoiceChanged))]
    private string firstRoleChoice = "";

    //Dumi: Dictionary to track each player's role
    private Dictionary<NetworkConnection, PlayerRole> playerRoles = new Dictionary<NetworkConnection, PlayerRole>();
    [SyncVar(hook = nameof(OnPlayerRoleChanged))]
    public PlayerRole localPlayerRole = PlayerRole.None;

    [Header("All Puzzle States")]
    [SyncVar(hook = nameof(OnPuzzle1SolvedChanged))]
    public bool puzzle1Solved = false; //D: Code Input puzzle with the word chalk and the animals
    [SyncVar(hook = nameof(OnPuzzle2SolvedChanged))]
    public bool puzzle2Solved = false; //D: Wire Cutting Puzzle w the electricity due date logic
    [SyncVar(hook = nameof(OnLightSwitchCompleteChanged))]
    public bool lightSwitchComplete = false; //Light switch puzzle
    [SyncVar(hook = nameof(OnPeriodicTableCompleteChanged))]
    public bool periodicTableComplete = false; //periodic table puzzle
    [SyncVar(hook = nameof(OnAnagramCompleteChanged))]
    public bool anagramComplete = false;// anagram puzzle logic 
    [SyncVar(hook = nameof(OnMoralChoiceCompleteChanged))]
    public bool moralChoiceComplete = false;



    //Dumi: puzzle bools to validate the correct implementation of them (the new Puzzles )
    [SyncVar] public bool bombdifuseComplete = false;

    [Header("Story Integration")] //dumi: story intergration tracking
    [SyncVar] public int storyPoints = 0; // Tracks moral choices
    [SyncVar] public int communicationStyle = 0; // Tracks how players communicate
    [SyncVar] public float bombTimer = 600f; // 10 minutes

    //Eden: Exposed for UI logic
    public bool HasFirstPressedStart => firstPressedStart;
    public string FirstRoleChoice => firstRoleChoice;

    // Component References
    private UIManager uiManager;

    void Awake()
    {
        //Eden: Singleton setup ensures only one GameSessionManager exists at any time
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        Instance = this;
    }

    void Start()
    {
        uiManager = FindObjectOfType<UIManager>();
    }

    void Update()
    {
        if (isServer && bombTimer > 0)
        {
            bombTimer -= Time.deltaTime;
            uiManager.TimerText.text = bombTimer.ToString("F0"); //Dumi: Show the actual timer for both players to see
            if (bombTimer <= 0)
            {
                RpcGameOver(false);
                return;
            }
        }
    }

    #region Session Management

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
            //Dumi:  Instead of accessing uiManager directly on server, use ClientRpc so that the client has access to the ui and not the server
            RpcShowRolePanel();
        }
    }


    //Dumi: New ClientRpc to show role panel on all clients
    [ClientRpc]
    void RpcShowRolePanel()
    {
        uiManager.RolePanel.SetActive(true);
    }
    //Eden: Rpc to all clients to hide the waiting UI and show the story panel
    [ClientRpc]
    void RpcBeginStory()
    {
        UIManager.Instance.EnterStory();


        if (isServer) //Dumi :  trigger the story flow from Act 1 after the players have selected their roles and have connected
        {
            StoryManager storyManager = FindObjectOfType<StoryManager>();
            if (storyManager != null)
            {
                storyManager.BeginStoryFlow();
            }
            else
            {
                Debug.LogError("StoryManager not found! Make sure it's spawned before GameSessionManager.");
            }
        }
    }

    /*Eden: This is called by a client when they choose either bomb player or office player
     * if firstRoleChoice is empty (nobody selected), it will record the selection as the first choice
     * else if the pick is the alternate, invoke RpcBothPlayersChosen()*/
    [Command(requiresAuthority = false)]
    public void CmdSelectRole(string role, NetworkConnectionToClient sender = null)
    {
        PlayerRole selectedRole = role == "BombPlayer" ? PlayerRole.BombPlayer : PlayerRole.OfficePlayer;

        if (string.IsNullOrEmpty(firstRoleChoice))
        {
            // First player chooses
            firstRoleChoice = role;
            playerRoles[sender] = selectedRole;

            // 🎯 THIS IS THE IMPORTANT PART - Call TargetRPC
            Debug.Log($"[Server] Calling TargetSetPlayerRole for first player: {selectedRole}");
            TargetSetPlayerRole(sender, selectedRole);

            Debug.Log($"[Server] firstRoleChoice set to '{role}' for connection {sender.connectionId}");
        }
        else if (role != firstRoleChoice)
        {
            // Second player chooses different role
            playerRoles[sender] = selectedRole;

            // 🎯 THIS IS THE IMPORTANT PART - Call TargetRPC  
            Debug.Log($"[Server] Calling TargetSetPlayerRole for second player: {selectedRole}");
            TargetSetPlayerRole(sender, selectedRole);

            Debug.Log($"[Server] second pick = '{role}' for connection {sender.connectionId}, both chosen");
            RpcBothPlayersChosen();
        }
        // If same role twice, ignore (no change needed)
    }
    // Target RPC to set role for specific client
    [TargetRpc]
    public void TargetSetPlayerRole(NetworkConnection target, PlayerRole role)
    {
        Debug.Log($"🎯 [TargetRPC] Setting role to: {role} for this client");
        Debug.Log($"🎯 [TargetRPC] Before: localPlayerRole = {localPlayerRole}");

        localPlayerRole = role;

        Debug.Log($"🎯 [TargetRPC] After: localPlayerRole = {localPlayerRole}");
        Debug.Log($"🎯 [TargetRPC] Role assignment complete!");
    }
    // Hook for when role changes
    public void OnPlayerRoleChanged(PlayerRole oldRole, PlayerRole newRole)
    {
        Debug.Log($"[Client] Role changed from {oldRole} to {newRole}");
    }


    #endregion

    #region Role Management

    //Dumi: Method to get a player's role by their connection
    public PlayerRole GetPlayerRole(NetworkConnection connection)
    {
        if (playerRoles.ContainsKey(connection))
        {
            return playerRoles[connection];
        }
        return PlayerRole.None;
    }

    //Dumi: Method for clients to get their own role
    public PlayerRole GetLocalPlayerRole()
    {
        if (NetworkClient.connection != null)
        {
            return GetPlayerRole(NetworkClient.connection);
        }
        return PlayerRole.None;
    }

    private PlayerRole GetClientRole()
    {
        return localPlayerRole;
    }

    #endregion

    #region Act 1 Puzzles

    [Server]
    public void StartLightSwitchPuzzle()
    {
        Debug.Log("[Server] Starting light switch puzzle");
        RpcInitializeLightSwitchPuzzle();
        
    }

    [ClientRpc]
    void RpcInitializeLightSwitchPuzzle()
    {
        // TEMPORARY: Manually assign roles for testing
        if (isServer)
        {
            localPlayerRole = PlayerRole.OfficePlayer; // Host gets office
           // Debug.Log("🧪 [TEST] Host assigned OfficePlayer role");
        }
        else
        {
            localPlayerRole = PlayerRole.BombPlayer; // Client gets bomb
            Debug.Log("Activating BOMB player UI");
            uiManager.ShowLightPuzzleForBombPlayer();
            // Debug.Log("🧪 [TEST] Client assigned BombPlayer role");
        }


        // DEBUG: Verify network connection
        Debug.Log($"[ROLE DEBUG] Local connection: {NetworkClient.connection != null}");

        PlayerRole role = GetClientRole();
        Debug.Log($"[ROLE DEBUG] Final role: {role}");

        if (uiManager == null)
        {
            uiManager = UIManager.Instance;
        }

        if (role == PlayerRole.OfficePlayer)
        {
            Debug.Log("Activating OFFICE player UI");
            uiManager.ShowLightPuzzleForOfficePlayer();
            StartCoroutine(ShowLightSwitchPatternCoroutine());
        }
        else if (role == PlayerRole.BombPlayer) 
        {
            Debug.Log("Activating BOMB player UI");
            uiManager.ShowLightPuzzleForBombPlayer();
        }
        else
        {
            Debug.LogError($"Invalid role detected: {role}");
        }
    }

    private System.Collections.IEnumerator ShowLightSwitchPatternCoroutine()
    {
        Debug.Log("[Client] Showing light pattern");

        // Show pattern
        uiManager.DisplayPattern(5f);
        yield return new WaitForSeconds(5f);

        // Hide pattern and show instructions
        uiManager.HidePattern();
        uiManager.ShowInstructionText("Communicate the pattern to your partner!");

        Debug.Log("[Client] Pattern shown and hidden");
    }

    [Command(requiresAuthority = false)]
    public void CmdLightSwitchInput(NetworkConnectionToClient sender = null)
    {
        if (!isServer) return;

        if (!lightSwitchComplete)
        {
            lightSwitchComplete = true;
            AddStoryPoints(1);
            Debug.Log("[Server] Light Switch Puzzle complete!");
        }

        else
        {
            RpcLightSwitchFailure();
            ModifyBombTimer(-10f);
            communicationStyle -= 1;
        }
    }

    [Server]
    public void StartAnagramPuzzle(string scrambled, string solution)
    {
        Debug.Log("[Server] Starting anagram puzzle");
        RpcInitializeAnagramPuzzle(scrambled, solution);
    }

    [ClientRpc]
    void RpcInitializeAnagramPuzzle(string scrambled, string solution)
    {
        PlayerRole role = GetClientRole();
        if (uiManager != null)
        {
            if (role == PlayerRole.OfficePlayer)
            {
                uiManager.ShowAnagramForOfficePlayer(scrambled);
            }
            else
            {
                uiManager.ShowAnagramForBombPlayer();
            }
        }

    }

    [Command(requiresAuthority = false)]
    public void CmdSubmitAnagram(string playerAnswer, NetworkConnectionToClient sender = null)
    {
        if (anagramComplete) return;

        bool isCorrect = playerAnswer.ToUpper().Trim() == "NOTSMARTENOUGH";
        if (isCorrect)
        {
            anagramComplete = true;
            Debug.Log("[Server] Anagram puzzle complete!");
            RpcAnagramSuccess();
            AddStoryPoints(1);
        }
        else
        {
            RpcAnagramFailure();
            ModifyBombTimer(-5f);
        }
    }
    void OnAnagramCompleteChanged(bool oldVal, bool newVal)
    {
        if (newVal)
        {
            RpcCleanupAnagramUI();
        }
    }

    [ClientRpc]
    void RpcCleanupAnagramUI()
    {
        if (uiManager != null)
        {
            uiManager.OnAnagramComplete();
        }
    }


    #endregion

    #region Act 2 Puzzles

    [Server]
    public void StartPeriodicTablePuzzle(int[] elements, string solution)
    {
        RpcInitializePeriodicTablePuzzle(elements, solution);
        Debug.Log("[Server] Periodic table puzzle  has started!");
    }

    [ClientRpc]
    void RpcInitializePeriodicTablePuzzle(int[] elements, string solution)
    {
        Debug.Log($"RPC received! isServer: {isServer}, isClient: {isClient}");

        // TEMPORARY: Manually assign roles for testing
        if (isServer)
        {
            localPlayerRole = PlayerRole.OfficePlayer; // Host gets office player role
            Debug.Log("Setting role to OfficePlayer");
            if (uiManager != null)
            {
                uiManager.ShowPeriodicTableForOfficePlayer(elements);
            }

        }
        else
        {
            localPlayerRole = PlayerRole.BombPlayer; //sever/client gets the bomb player role
            Debug.Log("Setting role to BombPlayer - Activating BOMB player UI");
            Debug.Log("Activating BOMB player UI");
            if (uiManager != null)
            {
                uiManager.ShowPeriodicTableForBombPlayer();
            }
        }
    }


    [Command(requiresAuthority = false)]
    public void CmdSubmitPeriodicSolution(string solution, NetworkConnectionToClient sender = null)
    {
        if (periodicTableComplete) return;
        bool isCorrrect = solution.ToUpper() == "GENIUS";
        if (isCorrrect)
        {
            periodicTableComplete = true;
            Debug.Log("[Server] Periodic table puzzle complete!");
            RpcPeriodicTableSuccess();
            AddStoryPoints(1);
        }
        else
        {
            RpcPeriodicTableFailure();
            ModifyBombTimer(-15f);
        }
    }


    void OnPeriodicTableCompleteChanged(bool oldVal, bool newVal)
    {
        if (newVal)
        {
            RpcCleanupPeriodicTableUI();
        }
    }

    [ClientRpc]
    void RpcCleanupPeriodicTableUI()
    {
        if (uiManager != null)
        {
            uiManager.OnPeriodicTableComplete();
        }
    }

    #endregion

    #region Act 3 Puzzzles
    [Server]
    public void StartWireCutRepresentation( string representation)
    {
        RpcRevealWireStory(representation);
    }


    [ClientRpc]
    void RpcRevealWireStory(string wireColor)
    {
        switch (wireColor.ToLower())
        {
            case "red":
                uiManager.ShowStoryReveal("Red wire cut... Represents Zipho's anger from the humiliation.");
                break;
            case "blue":
                uiManager.ShowStoryReveal("Blue wire cut... Represents the sadness from the lost confidence.");
                break;
            case "yellow":
                uiManager.ShowStoryReveal("Yellow wire cut... Represents the fear of failure that followed him.");
                break;
            case "green":
                uiManager.ShowStoryReveal("Green wire cut... Represents the envy of students who had supportive teachers.");
                break;
        }
    }

    [Server]
    public void StartWireCutPuzzle()
    {
        RpcInitializeWireCutPuzzle();
    }

    [ClientRpc]
    void RpcInitializeWireCutPuzzle()
    {
        PlayerRole role = GetClientRole();

        if (role == PlayerRole.OfficePlayer) //Dumi: the office player will type in the correct answer to this puzzle as discussed
        {
            uiManager.OfficeFinalPanel.SetActive(true);
            uiManager.BombFinalPanel.SetActive(false);
            
        }
        else if (role == PlayerRole.BombPlayer) 
        {
            uiManager.OfficeFinalPanel.SetActive(false);
            uiManager.BombFinalPanel.SetActive(true);
            
        }
    }

    [Command(requiresAuthority = false)]
    public void CmdAttemptCut(string wireColor)
    {
        if (!isServer) return;
        if (puzzle2Solved) return;

        if (wireColor == "Red")
        {
            puzzle2Solved = true;
            RpcShowWin();
            Debug.Log("[Server] Wire cutting puzzle solved!");
        }
        else
        {
            RpcScreenShake();
            ModifyBombTimer(-20f); //Dumi: Heavy penalty for wrong wire
            Debug.Log($"[Server] Wrong wire '{wireColor}'");
        }
    }


    [Server]
    public void StartChalkPuzzle ()
    {
        RpcInitializeChalkPuzzle();
    }


    [ClientRpc]

    void RpcInitializeChalkPuzzle()
    {
        PlayerRole role = GetClientRole();

        if (role == PlayerRole.OfficePlayer) //Dumi: the office player will type in the correct answer to this puzzle as discussed
        {
            uiManager.OfficeFinalPanel.SetActive(true);
            uiManager.chalkclueTxt.gameObject.SetActive(true);
            uiManager.BombFinalPanel.SetActive(false);

        }
        else if (role == PlayerRole.BombPlayer) 
        {
            uiManager.OfficeFinalPanel.SetActive(false);
            uiManager.BombFinalPanel.SetActive(true);

        }
    }

    #endregion


    #region Act 4 - Final Puzzles
    [Server]
    public void StartBombDisablePuzzle()
    {
        RpcInitializeBombDisablePuzzle();
    }


    [ClientRpc]
    void RpcInitializeBombDisablePuzzle()
    {
        PlayerRole role = GetClientRole();

        if (role == PlayerRole.OfficePlayer) 
        {
            uiManager.OfficeFinalPanel.SetActive(true);
            uiManager.chalkclueTxt.gameObject.SetActive(false);
            uiManager.BombFinalPanel.SetActive(false);

        }
        else if (role == PlayerRole.BombPlayer) 
        {
            uiManager.OfficeFinalPanel.SetActive(false);
            uiManager.BombFinalPanel.SetActive(true);

        }
    }



    [Command(requiresAuthority = false)]
    public void CmdRiddleSolved()
    {
        bombdifuseComplete = true;

    }

    [Server]
    public void StartMoralChoicePuzzle()
    {
        Debug.Log("[SERVER] starting moral choice puzzle");
        RpcInitializeMoralChoicePuzzle();

    }

    [ClientRpc]
    void RpcInitializeMoralChoicePuzzle() //D: both players  see this
    {
        Debug.Log("[Client] Initializing moral choice");
        if (uiManager != null)
        {
            uiManager.ShowMoralChoiceInterface();
        }
    }

    [Command(requiresAuthority = false)]
    public void CmdMakeMoralChoice(int choiceIndex, NetworkConnectionToClient sender = null)
    {
        if (moralChoiceComplete) return;


        if (ValidateBothPlayersChoice(choiceIndex))
        {
            moralChoiceComplete = true;

            if (choiceIndex == 0) // Complete neutralization. So they choose forgivenss
            {
                AddStoryPoints(3);
                RpcShowChoiceResult("You chose complete forgiveness. The past is buried.");
            }
            else // Symbolic message
            {
                AddStoryPoints(-1);
                RpcShowChoiceResult("You chose to leave a message. Sometimes lessons need to be taught.");
            }
        }
        else
        {
            RpcShowChoiceConflict("You and your partner have different views on justice...");
        }
    }
    void OnMoralChoiceCompleteChanged(bool oldVal, bool newVal)
    {
        if (newVal)
        {
            RpcCleanupMoralChoiceUI();
        }
    }

    [ClientRpc]
    void RpcCleanupMoralChoiceUI()
    {
        if (uiManager != null)
        {
            uiManager.OnMoralChoiceComplete();
        }
    }

    #endregion


    #region Story Management

    [Server]
    public void AddStoryPoints(int points)
    {
        storyPoints += points;
    }

    [Server]
    public void ModifyBombTimer(float timeChange)
    {
        bombTimer += timeChange;
        bombTimer = Mathf.Max(0, bombTimer);
    }

    #endregion

    #region Utility Methods

    bool ValidateLightSwitchInput()
    {
        Debug.Log("Light switch puzzle solved successfully"); //Sibahle: double-checking that execution of light switch puzzle is successful
        return true; // Simplified for example
    }

    bool ValidateBothPlayersChoice(int choice)
    {
        // Ensure both players made the same moral choice
        return true; // Simplified for example
    }

    #endregion

    #region Public Getters for StoryManager

    public bool IsLightSwitchComplete() => lightSwitchComplete;
    public bool IsPeriodicTableComplete() => periodicTableComplete;
    public bool IsAnagramComplete() => anagramComplete;
    public bool IsWireCutComplete() => puzzle2Solved;

    public bool IsChalkPuzzleComplete () => puzzle1Solved;

    public bool isBombDiffuseComplete() => bombdifuseComplete;

    public bool IsMoralChoiceComplete() => moralChoiceComplete;
    
    #endregion

    #region Network Callbacks and Hooks

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
        //Dumi: after players have chosen roles , begin the story flow:
        RpcBeginStory();
        Debug.Log("Story beats are now showing for each player based on their chosen roles");
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

    //Eden: Hook fired on each client when puzzle2Solved changes
    void OnPuzzle2SolvedChanged(bool oldVal, bool newVal)
    {
        if (newVal)
        {
            UIManager.Instance.DisableWireButtons();
            UIManager.Instance.riddleContainer.gameObject.SetActive(true);
        }
    }
    void OnLightSwitchCompleteChanged(bool oldVal, bool newVal)
    {
        if (newVal)
        {
            RpcLightSwitchComplete();
        }
    }

    [ClientRpc]
    void RpcLightSwitchComplete()
    {
        if (uiManager != null)
        {
            lightSwitchComplete = true;
            uiManager.OnLightSwitchComplete();
           
        }
    }

    // All the RPC methods for puzzle feedback
    [ClientRpc]
    void RpcLightSwitchSuccess()
    {
        uiManager.ShowSuccess("Power restored! The bomb is now visible.");
    }

    [ClientRpc]
    void RpcLightSwitchFailure()
    {
        uiManager.ShowFailure();
    }

    [ClientRpc]
    void RpcAnagramSuccess()
    {
        uiManager.ShowSuccess("Message decoded: FUELED BY HATE");
    }

    [ClientRpc]
    void RpcAnagramFailure()
    {
        uiManager.ShowFailure();
    }

    [ClientRpc]
    void RpcPeriodicTableSuccess()
    {
        uiManager.ShowSuccess("Code cracked: GENIUS");
    }

    [ClientRpc]
    void RpcPeriodicTableFailure()
    {
        uiManager.ShowFailure();
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

    [ClientRpc]
    void RpcTriggerStoryMoment(string storyText)
    {
        uiManager.ShowStoryMoment(storyText, 4f);
    }

    [ClientRpc]
    void RpcShowChoiceResult(string result)
    {
        uiManager.ShowChoiceResult(result);
    }

    [ClientRpc]
    void RpcShowChoiceConflict(string conflictText)
    {
        uiManager.ShowConflictMessage(conflictText);
    }

    [ClientRpc]
    void EndGameLogic()
    {
        UIManager.Instance.ShowBombDeactivationWin();
    }

    [ClientRpc]
    void RpcGameOver(bool success)
    {
        uiManager.ShowGameOverScreen(success, bombTimer);
    }

    #endregion

    
}