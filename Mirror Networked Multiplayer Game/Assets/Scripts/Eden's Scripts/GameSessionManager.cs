using UnityEngine;
using Mirror;
using System.Collections.Generic;

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

    [Header("All Puzzle States")]
    [SyncVar(hook = nameof(OnPuzzle1SolvedChanged))]
    public bool puzzle1Solved = false; //D: Code Input puzzle with the word chalk and the animals

    [SyncVar(hook = nameof(OnPuzzle2SolvedChanged))]
    public bool puzzle2Solved = false; //D: Wire Cutting Puzzle w the electricity due date logic


    //Dumi: puzzle bools to validate the correct implementation of them (the new Puzzles )
    [SyncVar] public bool lightSwitchComplete = false;
    [SyncVar] public bool anagramComplete = false;
    [SyncVar] public bool periodicTableComplete = false;
    [SyncVar] public bool moralChoiceComplete = false;

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
            //Eden: Second click, tell everyone to enter the story
            RpcBeginStory();
        }
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
        //Dumi: Convert string to PlayerRole enum
        PlayerRole selectedRole = role == "BombPlayer" ? PlayerRole.BombPlayer : PlayerRole.OfficePlayer;

        if (string.IsNullOrEmpty(firstRoleChoice))
        {
            //D: record the first choice
            firstRoleChoice = role;
            //D: Store this player's role
            playerRoles[sender] = selectedRole;
            Debug.Log($"[Server] firstRoleChoice set to '{role}' for connection {sender.connectionId}");
        }
        else if (role != firstRoleChoice)
        {
            // Store second player's role (the opposite role)
            playerRoles[sender] = selectedRole;
            // second pick → notify both clients that selection is done
            Debug.Log($"[Server] second pick = '{role}' for connection {sender.connectionId}, both chosen");
            RpcBothPlayersChosen();
        }
        //Eden: If same role twice, ignore
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
        return GetLocalPlayerRole();
    }

    #endregion

    #region Act 1 Puzzles

    [Server]
    public void StartLightSwitchPuzzle()
    {
        RpcInitializeLightSwitchPuzzle();
    }

    [ClientRpc]
    void RpcInitializeLightSwitchPuzzle()
    {
        PlayerRole role = GetClientRole();

        if (role == PlayerRole.OfficePlayer)
        {
            //Dumi:  Office player sees the pattern and story context
            uiManager.ShowSecurityFootage("Zipho is moving through the electrical room...");
            StartCoroutine(ShowLightSwitchPatternCoroutine());
        }
        else if (role == PlayerRole.BombPlayer)
        {
            // Dumi: Bomb player sees the grid they need to manipulate and they put in the answer
            uiManager.ShowLightSwitchGrid();
        }
    }

    private System.Collections.IEnumerator ShowLightSwitchPatternCoroutine()
    {
        //Dumi: Generate and show pattern for 3 seconds.@sibahle you can change the logic if this is not how you envision it to work
        int[] pattern = GenerateLightSwitchPattern();
        uiManager.DisplayPattern(pattern, 3f);
        yield return new WaitForSeconds(3f);
        uiManager.HidePattern();

        uiManager.ShowInstructionText("Communicate the pattern to your partner!");
    }

    private int[] GenerateLightSwitchPattern()
    {
        return new int[] { 1, 3, 5, 2, 4 }; //Dumi: Example pattern. Sibahle you can chang this if this is not hw you want this to work.
    }

    [Command(requiresAuthority = false)]
    public void CmdLightSwitchInput(int gridPosition, NetworkConnectionToClient sender = null)
    {
        bool isCorrect = ValidateLightSwitchInput(gridPosition);

        if (isCorrect)
        {
            if (!lightSwitchComplete)
            {
                lightSwitchComplete = true;
                AddStoryPoints(1);
                Debug.Log("[Server] Light Switch Puzzle complete!");
            }
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
        RpcInitializeAnagramPuzzle(scrambled, solution);
    }

    [ClientRpc]
    void RpcInitializeAnagramPuzzle(string scrambled, string solution)
    {
        PlayerRole role = GetClientRole();

        if (role == PlayerRole.OfficePlayer) //Dumi: The office player gets to do this puzzle so henc the role is the office player
        {
            uiManager.ShowAnagramDisplay($"Message on bomb: {scrambled}");
            uiManager.ShowAnagramInput();
        }
        else
        {
            uiManager.ShowStoryContext("These letters mean something about Zipho. What could they mean ? Try help the office player figure what they mean");//Dumi: what the bomb player sees
        }
    }

    [Command(requiresAuthority = false)]
    public void CmdSubmitAnagram(string playerAnswer, NetworkConnectionToClient sender = null)
    {
        if (playerAnswer.ToUpper().Trim() == "FUELEDBYHATE")
        {
            anagramComplete = true;
            RpcAnagramSuccess();
            AddStoryPoints(1); //Dumi: gain one story point for solving the puzzle correctly.
        }
        else
        {
            RpcAnagramFailure();
            ModifyBombTimer(-5f);
        }
    }

    #endregion

    #region Act 2 Puzzles

    [Server]
    public void StartPeriodicTablePuzzle(int[] elements, string solution)
    {
        RpcInitializePeriodicTablePuzzle(elements, solution);
    }

    [ClientRpc]
    void RpcInitializePeriodicTablePuzzle(int[] elements, string solution)
    {
        PlayerRole role = GetClientRole();

        if (role == PlayerRole.OfficePlayer) //Dumi: the office player will type in the correct answer to this puzzle as discussed
        {
            uiManager.OfficeFinalPanel.SetActive(true);
            uiManager.BombFinalPanel.SetActive(false);
            uiManager.ShowStoryContext("The numbers on the computer have something to do with chemistry.");
        }
        else if (role == PlayerRole.BombPlayer) // dumi: the bomb player will the periodic table and needs to communicate with the O.P.
        {
            uiManager.OfficeFinalPanel.SetActive(false);
            uiManager.BombFinalPanel.SetActive(true);
            uiManager.ShowStoryContext("Cross-reference these numbers with the periodic table.");
        }
    }

    [Command(requiresAuthority = false)]
    public void CmdSubmitPeriodicSolution(string solution, NetworkConnectionToClient sender = null)
    {
        if (solution.ToUpper() == "GENiUS")
        {
            periodicTableComplete = true;
            RpcPeriodicTableSuccess();
            RpcTriggerStoryMoment("The word that started it all... 'You're just not a GENIUS, Zipho.' from the flashback!");
            AddStoryPoints(1);
        }
        else
        {
            RpcPeriodicTableFailure();
            ModifyBombTimer(-15f);
        }
    }

    #endregion

    #region Act 3 Puzzles (Wire Cutting)

    [Command(requiresAuthority = false)]
    public void CmdAttemptCut(string wireColor)
    {
        if (!isServer) return;
        if (puzzle2Solved) return;

        if (wireColor == "Red")
        {
            puzzle2Solved = true;
            RpcRevealWireStory(wireColor);
            RpcShowWin();
            Debug.Log("[Server] Wire cutting puzzle solved!");
        }
        else
        {
            RpcRevealWireStory(wireColor);
            RpcScreenShake();
            ModifyBombTimer(-20f); //Dumi: Heavy penalty for wrong wire
            Debug.Log($"[Server] Wrong wire '{wireColor}'");
        }
    }

    [ClientRpc]
    void RpcRevealWireStory(string wireColor)
    {
        switch (wireColor.ToLower())
        {
            case "red":
                uiManager.ShowStoryReveal("Red wire cut... Zipho's anger begins to fade.");
                break;
            case "blue":
                uiManager.ShowStoryReveal("Blue wire cut... The tears of his family are acknowledged.");
                break;
            case "yellow":
                uiManager.ShowStoryReveal("Yellow wire cut... The fear of inadequacy loses its power.");
                break;
            case "green":
                uiManager.ShowStoryReveal("Green wire cut... Hope can still exist for everyone.");
                break;
        }
    }

    #endregion

    #region Act 4 - Final Puzzles

    [Command(requiresAuthority = false)]
    public void CmdRiddleSolved()
    {
        EndGameLogic();
    }

    [Server]
    public void StartMoralChoicePuzzle()
    {
        RpcInitializeMoralChoicePuzzle();
    }

    [ClientRpc]
    void RpcInitializeMoralChoicePuzzle() //D: both players  see this
    {
        uiManager.ShowMoralChoiceInterface();
        uiManager.ShowChoiceOptions(new string[]
        {
            "Completely neutralize - No message left",
            "Leave symbolic message - 'Words have power. Choose them wisely. - A former student'"
        });
    }

    [Command(requiresAuthority = false)]
    public void CmdMakeMoralChoice(int choiceIndex, NetworkConnectionToClient sender = null)
    {
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

    #endregion

    #region Dumi : (Extra) Communication Tracking

    [Command(requiresAuthority = false)]
    public void CmdLogCommunication(string messageType, NetworkConnectionToClient sender = null)
    {
        switch (messageType.ToLower())
        {
            case "helpful":
                communicationStyle += 1;
                AddStoryPoints(1);
                break;
            case "frustrated":
                communicationStyle -= 1;
                break;
            case "encouraging":
                communicationStyle += 2;
                AddStoryPoints(1);
                break;
            case "harsh":
                communicationStyle -= 2;
                AddStoryPoints(-1);
                break;
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

    bool ValidateLightSwitchInput(int position)
    {
        // D: @sibahle you can add your validation logic here
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
    public bool IsAnagramComplete() => anagramComplete;
    public bool IsPeriodicTableComplete() => periodicTableComplete;
    public bool IsMoralChoiceComplete() => moralChoiceComplete;
    public bool IsPuzzle2Solved => puzzle2Solved; // For wire cutting puzzle

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