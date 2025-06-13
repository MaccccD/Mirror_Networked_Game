using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using UnityEngine.InputSystem;
using System.Collections.Generic;

//Eden: Handles all the panels for the game flow and also handles role selection 
//and assigning each player to the correct screens.
//Dumi: I updated the ui manager to reference methods in the new story manager. The new methods include the stiry intergration UI ans well as the added  puzzle solving UI
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Startup UI")]
    public GameObject StartPanel;
    public Button StartButton;
    public GameObject WaitingPanel;

    [Header("Story UI")] //Dumi : added more Ui for the story , having taken out the narrative manager we had previously
    public GameObject StoryPanel; //Eden: Panel to explain our narrative once written
    public TMP_Text StoryText; //D: For displaying story dialogue
    public GameObject FlashbackPanel; //D: For flashback sequences
    public TMP_Text FlashbackText; //D: Text for flashback dialogue
    public GameObject ActDisplay; //D: Shows current act
    public TMP_Text ActText; //D: Text for current act

    [Header("Role Selection UI")]
    public GameObject RolePanel; //Eden: Panel for players to select either office player or bomb player 
    public Button OfficeButton;
    public Button BombButton;

    [Header("Final Game UI")]
    public GameObject OfficeFinalPanel; //Eden: The panel for the actual game play of the office player
    public GameObject BombFinalPanel; //Eden: The panel for the actual game play of the bomb player
    public GameObject endofGamePanel;
    public TMP_Text TimerText; // Timer display

    [Header("Puzzle 1 UI")]
    public GameObject puzzle1Container;
    public TMP_InputField[] letterFields;
    public Button puzzle1EnterButton;
    public GameObject riddleContainer;

    [Header("Bomb Puzzle UI")]
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
    public RectTransform uiRoot;       //Eden: for shaking UI directly
    public float shakeDuration = 0.5f;
    public float shakeMagnitude = 20f; //Eden: in UI units

    [Header("Error Flash UI")]
    public GameObject errorFlashPanel;
    //public TMP_Text errorFlashText;

    [Header("Story Integration UI")]//Dumi: I added the Ui for the stiry integration that contains story momenbts reveal at each puzzle solving moment
    public GameObject StoryMomentPanel; //D: For story revelations
    public TMP_Text StoryMomentText;
    public GameObject InstructionPanel; //D: For instruction text
    public TMP_Text InstructionText;
    public GameObject SuccessPanel; //D: Success messages
    public TMP_Text SuccessText;
    public GameObject FailurePanel; //D: Failure messages
    public GameObject failureImage;

    [Header("Light Switch Puzzle UI")] //Sibahle's Switch on the Light puzzel ui/ Can change based on how she's implementing the logic 
    public GameObject LightSwitchPanel;
    public GameObject SecurityFootagePanel; //Dumi: Sibahle you can change this name if its confusing. I just named it like this bc the pattern the other player will see mimicks a security footage esp bc the panel will be dark like you envisoned it. 
    public TMP_Text SecurityFootageText;
    public GameObject PatternDisplayPanel;
    public Button[] LightSwitchButtons; // Grid of buttons for bomb player
    public GameObject LightGridPanel;

    [Header("Anagram Puzzle UI")]//Dumi Anagram puzzle ui logic
    public GameObject AnagramPanel;
    public TMP_Text AnagramDisplayText;
    public TMP_InputField AnagramInputField;
    public Button AnagramSubmitButton;
    public GameObject StoryContextPanel;
    public TMP_Text StoryContextText;

    [Header("Periodic Table Puzzle UI")] //Dumi: Eden's Periodic table UI logic. Can change based on how she's implementing her logic.
    public GameObject PeriodicTablePanel;
    public TMP_Text ElementNumbersText;
    public GameObject PeriodicTableGrid; // Visual periodic table
    public TMP_InputField PeriodicSolutionInput;
    public Button PeriodicSubmitButton;

    [Header("Moral Choice UI")]//Dumi: The moral choie puzzle for btoh players at the end of the game
    public GameObject MoralChoicePanel;
    public Button[] MoralChoiceButtons;
    public TMP_Text[] MoralChoiceTexts;
    public GameObject ChoiceResultPanel;
    public TMP_Text ChoiceResultText;
    public GameObject ConflictPanel;
    public TMP_Text ConflictText;

    [Header("Ending UI")]
    public GameObject EndingPanel;
    public TMP_Text EndingText;
    public TMP_Text EndingTitle;

    bool puzzleRoleIsOffice;
    string selectedWire;

    [Header("Game session Mnanager Reference")]
    private GameSessionManager sessionManager;

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
        HideAllPanels();

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

        // Dumi : Initialize new puzzle button listeners
        if (AnagramSubmitButton != null)
            AnagramSubmitButton.onClick.AddListener(SubmitAnagram);

        if (PeriodicSubmitButton != null)
            PeriodicSubmitButton.onClick.AddListener(SubmitPeriodicSolution);

        //Eden: puzzle1EnterButton.onClick.AddListener(ValidatePuzzle1);
    }

    void HideAllPanels()
    {
        StartPanel.SetActive(false);
        WaitingPanel.SetActive(false);
        StoryPanel.SetActive(false);
        RolePanel.SetActive(false);
        OfficeFinalPanel.SetActive(false);
        BombFinalPanel.SetActive(false);
        ConfirmPanel.SetActive(false);
        WinPanel.SetActive(false);
        puzzle1Container.SetActive(false);
        errorFlashPanel.SetActive(false);

        //Dumi :  Hide all new panels when the game starts
        if (StoryMomentPanel != null) StoryMomentPanel.SetActive(false);
        if (InstructionPanel != null) InstructionPanel.SetActive(false);
        if (SuccessPanel != null) SuccessPanel.SetActive(false);
        if (FailurePanel != null) FailurePanel.SetActive(false);
        if (LightSwitchPanel != null) LightSwitchPanel.SetActive(false);
        if (SecurityFootagePanel != null) SecurityFootagePanel.SetActive(false);
        if (PatternDisplayPanel != null) PatternDisplayPanel.SetActive(false);
        if (AnagramPanel != null) AnagramPanel.SetActive(false);
        if (StoryContextPanel != null) StoryContextPanel.SetActive(false);
        if (PeriodicTablePanel != null) PeriodicTablePanel.SetActive(false);
        if (MoralChoicePanel != null) MoralChoicePanel.SetActive(false);
        if (ChoiceResultPanel != null) ChoiceResultPanel.SetActive(false);
        if (ConflictPanel != null) ConflictPanel.SetActive(false);
        if (EndingPanel != null) EndingPanel.SetActive(false);
        if (endofGamePanel != null) endofGamePanel.SetActive(false);
        if (FlashbackPanel != null) FlashbackPanel.SetActive(false);
    }

    #region Dumi: Newly Added Story Game Manager Methods

    public void DisplayStoryText(string dialogue, float duration)//Dumi:// this is responsible for displaying the actual text/story beats at each act during gameplay.
    {
        if (StoryText != null)
        {
            StoryPanel.SetActive(true);
            StoryText.text = dialogue;
            StartCoroutine(HideStoryTextAfterDelay(duration));
        }
    }

    IEnumerator HideStoryTextAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (StoryPanel != null) StoryPanel.SetActive(false);
    }

    public void StartFlashbackEffect() //Dumi: this is responsible for the flash back effect in Act 2 of the stiry and gameplay
    {
        if (FlashbackPanel != null)
        {
            FlashbackPanel.SetActive(true);
            Debug.Log("Flashback panel is active !");

        }
        else
        {
            return;
        }
    }


    public void ShowEnding(string endingMessage, string endingType) //Dumi// this is responsible for the dning after sibahle's puzzle and the krola choices have been selected.
    {
        if (EndingPanel != null && EndingText != null && EndingTitle != null)
        {
            EndingPanel.SetActive(true);
            EndingText.text = endingMessage;
            EndingTitle.text = endingType;
        }
    }

    public void UpdateActDisplay(StoryManager.GameAct newAct) //Dumi: this is responsible for showing the palyers which act of the story they are on as the game progresses kinda like how books signal to readers the chaperts they are on n the book
    {
        if (ActDisplay != null && ActText != null)
        {
            ActDisplay.SetActive(true);
            ActText.text = "ACT " + ((int)newAct + 1).ToString();
        }
    }

    public void UpdateStoryState(StoryManager.StoryState newState) //Dumi: this method updatres the state at which the story is in during gameplay just to keep trakc of the pacing
    {
        // Dumi : Update UI based on story state if needed
        Debug.Log($"Story state changed to: {newState}");
    }

    public void ShowGameOverScreen(bool success, float timeRemaining) //Dumi: Game ending stuff
    {
        if (endofGamePanel != null)
        {
           
            endofGamePanel.gameObject.SetActive(true);
            Debug.Log("Win Panel and end of game panel have shown");
            string message = success ? "Mission Accomplished!" : "Mission Failed!";
            message += $"\nTime Remaining: {timeRemaining:F1}s";
        }
    }

    #endregion

    #region Dumi :Newly added   Puzzle Manager Methods

    //D: Light Switch Puzzle Methods NB: Sibahle you can changed the logic here if that's not how it works according to how you envisioned it for the UI.
    public void ShowSecurityFootage(string footageText)
    {
        if (SecurityFootagePanel != null && SecurityFootageText != null)
        {
            SecurityFootagePanel.SetActive(true);
            SecurityFootageText.text = footageText;
        }
    }

    public void DisplayPattern(int[] pattern, float duration)
    {
        if (PatternDisplayPanel != null)
        {
            PatternDisplayPanel.SetActive(true);
            // Implement pattern display logic here
            StartCoroutine(HidePatternAfterDelay(duration));
        }
    }

    IEnumerator HidePatternAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        HidePattern();
    }

    public void HidePattern()
    {
        if (PatternDisplayPanel != null)
            PatternDisplayPanel.SetActive(false);
    }

    public void ShowLightSwitchGrid()
    {
        if (LightGridPanel != null)
        {
            LightGridPanel.SetActive(true);
        }
    }

    public void ShowInstructionText(string instruction)
    {
        if (InstructionPanel != null && InstructionText != null)
        {
            InstructionPanel.SetActive(true);
            InstructionText.text = instruction;
        }
    }

    // Dumi: Anagram Puzzle Methods
    public void ShowAnagramDisplay(string scrambledText)
    {
        if (AnagramPanel != null && AnagramDisplayText != null)
        {
            AnagramPanel.SetActive(true);
            AnagramDisplayText.text = scrambledText;
        }
    }

    public void ShowAnagramInput()
    {
        if (AnagramInputField != null)
        {
            AnagramInputField.gameObject.SetActive(true);
            AnagramInputField.text = "";
        }
    }

    public void ShowStoryContext(string context)
    {
        if (StoryContextPanel != null && StoryContextText != null)
        {
            StoryContextPanel.SetActive(true);
            StoryContextText.text = context;
        }
    }

  public  void SubmitAnagram()
    {
        if (AnagramInputField != null)
        {
            string answer = AnagramInputField.text.Trim();
            //Dumi:  Send to PuzzleManager
            FindObjectOfType<GameSessionManager>()?.CmdSubmitAnagram(answer);
        }
    }

    // Periodic Table Puzzle Methods NB: Eden you can change the code here if that's not how you envision the logic to work for the UI.
    public void ShowElementNumbers(int[] elements)
    {
        if (PeriodicTablePanel != null && ElementNumbersText != null)
        {
            PeriodicTablePanel.SetActive(true);
            string elementText = "Elements: " + string.Join(", ", elements);
            ElementNumbersText.text = elementText;
        }
    }

    public void ShowPeriodicTable()
    {
        if (PeriodicTableGrid != null)
        {
            PeriodicTableGrid.SetActive(true);
        }
    }

    void SubmitPeriodicSolution()
    {
        if (PeriodicSolutionInput != null)
        {
            string solution = PeriodicSolutionInput.text.Trim();
            // FindObjectOfType<PuzzleManager>()?.CmdSubmitPeriodicSolution(solution);
        }
    }

    public void ShowStoryReveal(string revealText)
    {
        ShowStoryMoment(revealText, 3f);
    }

    //Dumi :  Moral Choice Methods
    public void ShowMoralChoiceInterface()
    {
        if (MoralChoicePanel != null)
        {
            MoralChoicePanel.SetActive(true);
        }
    }

    public void ShowChoiceOptions(string[] options)
    {
        for (int i = 0; i < MoralChoiceButtons.Length && i < options.Length; i++)
        {
            if (MoralChoiceButtons[i] != null && MoralChoiceTexts[i] != null)
            {
                MoralChoiceButtons[i].gameObject.SetActive(true);
                MoralChoiceTexts[i].text = options[i];
                int choiceIndex = i;
                MoralChoiceButtons[i].onClick.RemoveAllListeners();
                MoralChoiceButtons[i].onClick.AddListener(() => MakeMoralChoice(choiceIndex));
            }
        }
    }

    void MakeMoralChoice(int choiceIndex)
    {
        FindObjectOfType<GameSessionManager>()?.CmdMakeMoralChoice(choiceIndex);
    }

    public void ShowChoiceResult(string result)//Dumi: Showing reuslts of the kroal choice acter p;ayers have made their choices
    {
        if (ChoiceResultPanel != null && ChoiceResultText != null)
        {
            ChoiceResultPanel.SetActive(true);
            ChoiceResultText.text = result;
        }
    }

    public void ShowConflictMessage(string conflictText)
    {
        if (ConflictPanel != null && ConflictText != null)
        {
            ConflictPanel.SetActive(true);
            ConflictText.text = conflictText;
        }
    }

    //Dumi:  General Feedback Methods
    public void ShowSuccess(string message)
    {
        if (SuccessPanel != null && SuccessText != null)
        {
            SuccessPanel.SetActive(true);
            SuccessText.text = message;
            StartCoroutine(HideSuccessAfterDelay(2f));
        }
    }

    IEnumerator HideSuccessAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (SuccessPanel != null) SuccessPanel.SetActive(false);
    }

    public void ShowFailure()
    {
        if (FailurePanel != null && failureImage != null)
        {
            FailurePanel.SetActive(true);
            failureImage.gameObject.SetActive(true);
            StartCoroutine(HideFailureAfterDelay(2f));
        }
    }

    IEnumerator HideFailureAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (FailurePanel != null) FailurePanel.SetActive(false);
    }

    public void ShowStoryMoment(string storyText, float duration)
    {
        if (StoryMomentPanel != null && StoryMomentText != null)
        {
            StoryMomentPanel.SetActive(true);
            StoryMomentText.text = storyText;
            StartCoroutine(HideStoryMomentAfterDelay(duration));
        }
    }

    IEnumerator HideStoryMomentAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (StoryMomentPanel != null) StoryMomentPanel.SetActive(false);
    }

    #endregion

    #region Original Methods (Preserved like we had)

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
        //Eden: Once both pressed, the players go to the story panel 
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

        //Eden: Determine this client's role by which button ended up disabled first
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
        foreach (Button riddleBtn in RiddleButtons)
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
    #endregion
}