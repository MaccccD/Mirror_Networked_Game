// Assets/Scripts/GameSessionManager.cs
using UnityEngine;
using Mirror;

public class GameSessionManager : NetworkBehaviour
{
    public static GameSessionManager Instance { get; private set; }

    // Tracks whether someone has already pressed Start
    [SyncVar] private bool firstPressedStart = false;
    // Tracks the first role choice; hook fires on all clients
    [SyncVar(hook = nameof(OnFirstRoleChoiceChanged))]
    private string firstRoleChoice = "";

    // Exposed for UI logic
    public bool HasFirstPressedStart => firstPressedStart;
    public string FirstRoleChoice => firstRoleChoice;

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    // Called by both players when they click the Start button
    [Command(requiresAuthority = false)]
    public void CmdPressStart()
    {
        if (!firstPressedStart)
        {
            // first click among all clients
            firstPressedStart = true;
        }
        else
        {
            // second click → tell everyone to enter the story
            RpcBeginStory();
        }
    }

    [ClientRpc]
    void RpcBeginStory()
    {
        UIManager.Instance.EnterStory();
    }

    // Called by each client when they pick a role
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
        // if same role twice, ignore
    }

    // Called on each client when firstRoleChoice changes
    void OnFirstRoleChoiceChanged(string oldChoice, string newChoice)
    {
        UIManager.Instance.OnFirstRoleChosen(newChoice);
    }

    [ClientRpc]
    void RpcBothPlayersChosen()
    {
        UIManager.Instance.OnBothPlayersChosen();
    }
}
