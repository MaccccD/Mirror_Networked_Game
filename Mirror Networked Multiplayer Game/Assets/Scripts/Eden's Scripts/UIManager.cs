using System.Collections;
using UnityEngine;
using UnityEngine.UI;

//Eden: Handles all the panels for the game flow and also handles role selection 
//and assigning each player to the correct screens
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Startup UI")]
    public GameObject StartPanel;    
    public Button StartButton;    
    public GameObject WaitingPanel;  

    [Header("Story UI")]
    public GameObject StoryPanel; //Eden: Panel to explain our narrative once written

    [Header("Role Selection UI")]
    public GameObject RolePanel; //Eden: Panel for players to select either office player or bomb player 
    public Button OfficeButton; 
    public Button BombButton;

    [Header("Final Game UI")]
    public GameObject OfficeFinalPanel; //Eden: The panel for the actual game play of the office player
    public GameObject BombFinalPanel; //Eden: The panel for the actual game play of the bomb player

    [Header("Bomb Puzzle UI")]
    public GameObject ConfirmPanel;
    public Button ConfirmYesButton;
    public Button ConfirmNoButton;
    public GameObject WinPanel;

    public Button PinkWireButton;
    public Button RedWireButton;
    public Button OrangeWireButton;

    [Header("Screen Shake")]
    public RectTransform uiRoot;       // for shaking UI directly
    public float shakeDuration = 0.5f;
    public float shakeMagnitude = 20f; // in UI units

    bool puzzleRoleIsOffice;
    string selectedWire;

    void Awake()
    {
        //Eden: Singleton setup ensures only one UIManager exists at any time
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        //Eden: Hide everything at start except start panel
        StartPanel.SetActive(true);
        WaitingPanel.SetActive(false);
        StoryPanel.SetActive(false);
        RolePanel.SetActive(false);
        OfficeFinalPanel.SetActive(false);
        BombFinalPanel.SetActive(false);
        ConfirmPanel.SetActive(false);
        WinPanel.SetActive(false);

        StartButton.onClick.RemoveAllListeners();
        StartButton.onClick.AddListener(OnStartClicked);

        // Wire up role choice buttons
        OfficeButton.onClick.AddListener(() => MakeRoleChoice("Office"));
        BombButton.onClick.AddListener(() => MakeRoleChoice("Bomb"));

        // Confirmation dialog
        ConfirmYesButton.onClick.AddListener(OnConfirmYes);
        ConfirmNoButton.onClick.AddListener(OnConfirmNo);

        // Wire puzzle
        PinkWireButton.onClick.AddListener(() => StartConfirm("Pink"));
        RedWireButton.onClick.AddListener(() => StartConfirm("Red"));
        OrangeWireButton.onClick.AddListener(() => StartConfirm("Orange"));
    }

    //Eden: Called by PlayerNetwork.OnStartLocalPlayer() on each client
    //and activates the UI 
    public void ShowStartUI()
    {
        StartPanel.SetActive(true);
        WaitingPanel.SetActive(false);
        StoryPanel.SetActive(false);

        //Eden: Clear old listeners and listen for new clicks
        //StartButton.onClick.RemoveAllListeners();
        //StartButton.onClick.AddListener(OnStartClicked);
    }

    /*Eden: Handles the start buttons OnClick functionality
     * if player has pressed the button and is waiting for other player (communicates through server)
     * then wait panel is displayed*/
    
    void OnStartClicked()
    {
        StartPanel.SetActive(false);

        //Eden: if its the first click on start button show waiting panel
        if (!GameSessionManager.Instance.HasFirstPressedStart)
            WaitingPanel.SetActive(true);

        //Eden: this tells the server that this player pressed start
        GameSessionManager.Instance.CmdPressStart();
    }

    //Eden: Called via RpcBeginStory() once both players have pressed start button
    public void EnterStory()
    {
        //Eden: Once both pressed, the players go to a narrative or story panel 
        WaitingPanel.SetActive(false);
        StoryPanel.SetActive(true);
    }

    //Eden: This is called by the role buttons (onClick)
    //Eden: It disables the chosen button, displayes waiting panel if you pressed first (communicates with server)
    public void MakeRoleChoice(string role)
    {
        Debug.Log($"[UI] MakeRoleChoice('{role}')");

        //Eden: Disable chosen button locally so player cant click again
        if (role == "Office") OfficeButton.interactable = false;
        else BombButton.interactable = false;

        //Eden: hide roles panel and activate waiting panel if chose first
        RolePanel.SetActive(false);
        WaitingPanel.SetActive(true);

        //Eden: Send choice to server
        GameSessionManager.Instance.CmdSelectRole(role);
    }

    /*Eden: SyncVar hook (in gamesessionmanager) runs on all clients when first player choice is received.
     * It disables the same button on the other players screen so they can only select other button
     * ensuring players each have unique role*/
    public void OnFirstRoleChosen(string firstChoice)
    {
        Debug.Log($"[UI] OnFirstRoleChosen('{firstChoice}')");

        if (firstChoice == "Office")
            OfficeButton.interactable = false;
        else
            BombButton.interactable = false;
    }

    //Eden: ClientRpc (in gamesessionmanager) called once both players have chosen and changes panels to appropriate screens
    public void OnBothPlayersChosen()
    {
        Debug.Log("[UI] OnBothPlayersChosen()");

        WaitingPanel.SetActive(false);
        RolePanel.SetActive(false);

        //Eden: Determine this client’s role by which button ended up disabled first
        bool isOffice = !OfficeButton.interactable && BombButton.interactable;
        puzzleRoleIsOffice = isOffice;

        OfficeFinalPanel.SetActive(isOffice);
        BombFinalPanel.SetActive(!isOffice);
    }

    void StartConfirm(string wire)
    {
        selectedWire = wire;
        BombFinalPanel.SetActive(false);
        ConfirmPanel.SetActive(true);
    }

    void OnConfirmNo()
    {
        ConfirmPanel.SetActive(false);
        BombFinalPanel.SetActive(true);
    }

    void OnConfirmYes()
    {
        ConfirmPanel.SetActive(false);
        GameSessionManager.Instance.CmdAttemptCut(selectedWire);
    }

    // === SCREEN SHAKE on WRONG CUT ===
    public void ShakeUI()
    {
        StartCoroutine(DoUIShake());
    }

    IEnumerator DoUIShake()
    {
        Vector2 orig = uiRoot.anchoredPosition;
        float elapsed = 0f;
        while (elapsed < shakeDuration)
        {
            float x = Random.Range(-1f, 1f) * shakeMagnitude;
            float y = Random.Range(-1f, 1f) * shakeMagnitude;
            uiRoot.anchoredPosition = orig + new Vector2(x, y);
            elapsed += Time.deltaTime;
            yield return null;
        }
        uiRoot.anchoredPosition = orig;
    }

    // === WIN PANEL & RESET ===
    public void ShowWinPanel()
    {
        WinPanel.SetActive(true);
        StartCoroutine(WinSequence());
    }

    IEnumerator WinSequence()
    {
        yield return new WaitForSeconds(5f);
        WinPanel.SetActive(false);

        if (puzzleRoleIsOffice) OfficeFinalPanel.SetActive(true);
        else BombFinalPanel.SetActive(true);
    }

    // === DISABLE ALL WIRES ONCE SOLVED ===
    public void DisableWireButtons()
    {
        PinkWireButton.interactable = false;
        RedWireButton.interactable = false;
        OrangeWireButton.interactable = false;
    }
}
