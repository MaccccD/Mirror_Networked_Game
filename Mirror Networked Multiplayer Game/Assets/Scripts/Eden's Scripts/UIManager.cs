using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using Mirror.Examples.BilliardsPredicted;
using UnityEngine.InputSystem;
using System.Collections.Generic;

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
    public GameObject endofGamePanel;

    [Header("Puzzle 1 UI")]
    public GameObject puzzle1Container;        
    public TMP_InputField[] letterFields;     
    public Button puzzle1EnterButton;
    public GameObject riddleContainer;

    [Header("Bomb Puzzle UI")]
    public GameObject ConfirmPanel;
    public Button ConfirmYesButton;
    public Button ConfirmNoButton;
    public GameObject WinPanel;

    public Button PinkWireButton;
    public Button RedWireButton;
    public Button OrangeWireButton;

    [Header("Riddle Puzzle UI")] //Sibahle: riddle variables
    public GameObject riddlepuzzleContainer;
    public Button[] RiddleButtons;
    public TMP_Text DeactivationText;
    public GameObject ErrorFlash;

    [Header("Screen Shake")]
    public RectTransform uiRoot;       // for shaking UI directly
    public float shakeDuration = 0.5f;
    public float shakeMagnitude = 20f; // in UI units

    [Header("Error Flash UI")]
    public GameObject errorFlashPanel;  
    //public TMP_Text errorFlashText;

    bool puzzleRoleIsOffice;
    string selectedWire;

    private List<string> playerInput = new List<string>(); // Sibahle: refers to what button the player selects in the list
    public Timer countdownTimer;
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
        StartPanel.SetActive(false);
        WaitingPanel.SetActive(false);
        StoryPanel.SetActive(false);
        RolePanel.SetActive(false);
        OfficeFinalPanel.SetActive(false);
        BombFinalPanel.SetActive(false);
        ConfirmPanel.SetActive(false);
        WinPanel.SetActive(false);
        puzzle1Container.SetActive(false);

        StartButton.onClick.RemoveAllListeners();
        StartButton.onClick.AddListener(OnStartClicked);

        OfficeButton.onClick.AddListener(() => MakeRoleChoice("Office"));
        BombButton.onClick.AddListener(() => MakeRoleChoice("Bomb"));

        puzzle1EnterButton.onClick.AddListener(ValidatePuzzle1);

        ConfirmYesButton.onClick.AddListener(OnConfirmYes);
        ConfirmNoButton.onClick.AddListener(OnConfirmNo);

        PinkWireButton.onClick.AddListener(() => StartConfirm("Pink"));
        RedWireButton.onClick.AddListener(() => StartConfirm("Red"));
        OrangeWireButton.onClick.AddListener(() => StartConfirm("Orange"));

        errorFlashPanel.SetActive(false);
        //errorFlashText.gameObject.SetActive(false);

        //puzzle1EnterButton.onClick.AddListener(ValidatePuzzle1);
    }

    IEnumerator FlashErrorPanel()
    {
        errorFlashPanel.SetActive(true);
        //errorFlashText.gameObject.SetActive(true);

        for (int i = 0; i < 3; i++)
        {
            yield return new WaitForSeconds(0.5f);
            errorFlashPanel.SetActive(!errorFlashPanel.activeSelf);
        }

        errorFlashPanel.SetActive(false);
        //errorFlashText.gameObject.SetActive(false);
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

        //Dumi: disabling the bomb btns functionality here as early as possible before players choose their roles:
        DisableBombBtns();
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

        puzzle1Container.SetActive(true);
        foreach (var f in letterFields)
        {
            f.text = "";
            var bg = f.GetComponent<Image>();
            if (bg != null)
                bg.color = new Color(1, 1, 1, 0); // transparent
        }

        //Eden: Determine this client’s role by which button ended up disabled first
        bool isOffice = !OfficeButton.interactable && BombButton.interactable;
        puzzleRoleIsOffice = isOffice;

        OfficeFinalPanel.SetActive(isOffice);
        BombFinalPanel.SetActive(!isOffice);

    }

    void ValidatePuzzle1()
    {
        string answer = "";
        foreach (var f in letterFields)
            answer += f.text.Trim().ToUpper();

        if (answer == "CHALK")
        {
            GameSessionManager.Instance.CmdPuzzle1Complete();
        }
        else
        {
            StartCoroutine(FlashWrong());
        }
    }

    IEnumerator FlashWrong()
    {
        // flash each input's background red, then clear & reset
        Color[] originals = new Color[letterFields.Length];
        for (int i = 0; i < letterFields.Length; i++)
        {
            var bg = letterFields[i].GetComponent<Image>();
            originals[i] = bg != null ? bg.color : Color.clear;
            if (bg != null) bg.color = Color.red;
        }

        yield return new WaitForSeconds(0.5f);

        for (int i = 0; i < letterFields.Length; i++)
        {
            var bg = letterFields[i].GetComponent<Image>();
            if (bg != null) bg.color = new Color(1, 1, 1, 0);
            letterFields[i].text = "";
        }
    }

    public void OnPuzzle1Complete()
    {
        puzzle1Container.SetActive(false);
        WinPanel.SetActive(true);
        StartCoroutine(Puzzle1WinSequence());
        //riddleContainer.SetActive(true); //Sibahle: Supposed to set active after Password Puzzle deactivates
    }

    IEnumerator Puzzle1WinSequence()
    {
        yield return new WaitForSeconds(5f);
        WinPanel.SetActive(false);
        ActivateWirePuzzle();
    }

     void ActivateWirePuzzle()
    {
        OfficeFinalPanel.SetActive(puzzleRoleIsOffice);
        BombFinalPanel.SetActive(!puzzleRoleIsOffice);

        PinkWireButton.gameObject.SetActive(true);
        RedWireButton.gameObject.SetActive(true);
        OrangeWireButton.gameObject.SetActive(true);
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
        // riddleContainer.gameObject.SetActive(true); //Dumi: so enabling the riddle that will aid in solving the disbaling of the bomb , but this only happnes ater the cutting of the wire has been completed and solved.

        if (selectedWire == "Pink" || selectedWire == "Orange")
        {
            StartCoroutine(FlashErrorPanel());
        }

        foreach (Button riddleBtn in RiddleButtons) //Dumi: so im ensuring that players only get access to the btns in the bomb after the 2nd puzzle has been solved and completed.
        {
            riddleBtn.gameObject.SetActive(true);
            Debug.Log("all riddle btns have been enabled successfully, dankoooooo");
        }
        
      
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

    public void ShowBombDeactivationWin()
    {
        WinPanel.SetActive(true);
        StartCoroutine(BombWinDeactivation());
    }
    IEnumerator BombWinDeactivation()
    {
        yield return new WaitForSeconds(5f);
        WinPanel.SetActive(false);
        endofGamePanel.gameObject.SetActive(true);
        Debug.Log("Win Panel and end of game panel have shown");
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
    public void DisableBombBtns() //Dumi: this will control the disabled bomb btns for the whole game  untill all 2 puzzles are completed.
    {
        foreach(Button riddleBtn in RiddleButtons)
        {
            riddleBtn.gameObject.SetActive(false);
            Debug.Log("All riddle btns have be disabled successfully, yayyy");
        }
       
    }
    public void SolveRiddlePuzzle() //Sibahle: The last bomb logic puzzle with riddle from office player
    {
        string[] correctOrder = { "Red Btn", "Blue Btn", "Green Btn" };
        
        DeactivationText.gameObject.SetActive(false);
        ErrorFlash.gameObject.SetActive(false);

        playerInput.Clear(); // Sibahle: refers to what button the player selects

        foreach (Button btn in RiddleButtons)
        {
            string btnName = btn.name;

            btn.onClick.RemoveAllListeners(); // Sibahle: prevents multiple listeners so that function can be attached onto a single button but reads all inputs
            btn.onClick.AddListener(() =>
            {
                playerInput.Add(btnName);
                
                for (int i = 0; i < playerInput.Count; i++) //Sibahle: If wrong button is selected, FlashandReset method is called
                {
                    if (playerInput[i] != correctOrder[i])
                    {
                        StartCoroutine(FlashandReset());
                        
                        return;
                    }
                }

            
                if (playerInput.Count == correctOrder.Length) //Sibahle: When player selects buttons in the correct order
                {
                    DeactivationText.gameObject.SetActive(true);
                    countdownTimer.PauseTimer(); //Sibahle: Calling method from Timer script to pause the countdown
                    Debug.Log("Timer reference: " + countdownTimer);
                    GameSessionManager.Instance.CmdRiddleSolved(); //Dumi : ensuring that the end of game panel shows up on both screen after bomb deactivates
                    Debug.Log("Yayyyy, the end game panel is showing on both screenns!!!");
                    
                    

                }

            });
        }
    }

    IEnumerator FlashandReset()
    {
        ErrorFlash.gameObject.SetActive(true); //Sibahle: Red flash image on screen displayed
        yield return new WaitForSeconds(1f);
        ErrorFlash.gameObject.SetActive(false);
        
    }
}
