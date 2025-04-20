// Assets/Scripts/UIManager.cs
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Startup UI")]
    public GameObject StartPanel;    // “Press Start” panel
    public Button StartButton;    // the button inside it
    public GameObject WaitingPanel;  // “Waiting for other player…” panel

    [Header("Story UI")]
    public GameObject StoryPanel;    // your introductory story panel

    [Header("Role Selection UI")]
    public GameObject RolePanel;     // panel containing Office/Bomb buttons
    public Button OfficeButton;   // drag in “Office” button
    public Button BombButton;     // drag in “Bomb” button

    [Header("Final Game UI")]
    public GameObject OfficeFinalPanel; // shown to the Office-role player
    public GameObject BombFinalPanel;   // shown to the Bomb-role player

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // hide everything at start
        StartPanel.SetActive(false);
        WaitingPanel.SetActive(false);
        StoryPanel.SetActive(false);
        RolePanel.SetActive(false);
        OfficeFinalPanel.SetActive(false);
        BombFinalPanel.SetActive(false);
    }

    // === START PHASE ===

    // Called by PlayerNetwork.OnStartLocalPlayer()
    public void ShowStartUI()
    {
        StartPanel.SetActive(true);
        WaitingPanel.SetActive(false);
        StoryPanel.SetActive(false);

        StartButton.onClick.RemoveAllListeners();
        StartButton.onClick.AddListener(OnStartClicked);
    }

    void OnStartClicked()
    {
        StartPanel.SetActive(false);

        // if first click overall, show waiting
        if (!GameSessionManager.Instance.HasFirstPressedStart)
            WaitingPanel.SetActive(true);

        GameSessionManager.Instance.CmdPressStart();
    }

    // Called via RpcBeginStory()
    public void EnterStory()
    {
        WaitingPanel.SetActive(false);
        StoryPanel.SetActive(true);
        // YOUR inspector‑driven story logic should then activate RolePanel
    }

    // === ROLE SELECTION PHASE ===

    // Wire these in the Inspector:
    // OfficeButton.OnClick() → UIManager.MakeRoleChoice("Office")
    // BombButton.OnClick()   → UIManager.MakeRoleChoice("Bomb")
    public void MakeRoleChoice(string role)
    {
        Debug.Log($"[UI] MakeRoleChoice('{role}')");

        // disable chosen button locally
        if (role == "Office") OfficeButton.interactable = false;
        else BombButton.interactable = false;

        // switch to waiting screen
        RolePanel.SetActive(false);
        WaitingPanel.SetActive(true);

        GameSessionManager.Instance.CmdSelectRole(role);
    }

    // Called on all clients when the first role is set (SyncVar hook)
    public void OnFirstRoleChosen(string firstChoice)
    {
        Debug.Log($"[UI] OnFirstRoleChosen('{firstChoice}')");

        if (firstChoice == "Office")
            OfficeButton.interactable = false;
        else
            BombButton.interactable = false;
    }

    // Called on all clients via RpcBothPlayersChosen()
    public void OnBothPlayersChosen()
    {
        Debug.Log("[UI] OnBothPlayersChosen()");

        WaitingPanel.SetActive(false);
        RolePanel.SetActive(false);

        // determine this client’s role by which button was disabled
        bool isOffice = !OfficeButton.interactable && BombButton.interactable;

        OfficeFinalPanel.SetActive(isOffice);
        BombFinalPanel.SetActive(!isOffice);
    }
}
