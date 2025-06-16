using UnityEngine;
using Mirror;
using System.Collections;
using System.Collections.Generic;
//Dumi: This script is the story manager that coordinates the flow of the story using the 4-Act Structure.
//It breaks down each act using a coroutine and contains the neccessary story beats to show for each player based on their role in the game.
public class StoryManager : NetworkBehaviour

{
    [Header("Game State")]
    [SyncVar(hook = nameof(OnActChanged))] public GameAct currentAct = GameAct.Act1_Setup;
    [SyncVar(hook = nameof(OnStoryStateChanged))] public StoryState storyState = StoryState.IntroDialogue;
    [SyncVar] public float bombTimer = 600f; // can be changed  10 minutes
    [SyncVar] public int storyPoints = 0; // Dumi: Tracks moral choices wich dtermine the ending like I explained

    [Header("Story Progress")]
    [SyncVar] public bool flashbackRevealed = false;
    [SyncVar] public bool ziphoMotivationKnown = false;
    [SyncVar] public bool finalChoiceUnlocked = false;
    [SyncVar] public bool isStoryStarted = false;


    public FlashbackManager flashbackManager;

  //  [Header("Audio Management")]
  //  public AudioSource storyAudioSource;
   // public AudioClip[] actTransitionSounds;
 

    public enum GameAct { Act1_Setup, Act2_Reaction, Act3_Action, Act4_Resolution }//Dumi: tracks the current act the game is on and show relevant feedback
    public enum StoryState { IntroDialogue, PuzzleSolving, StoryReveal, ActTransition, GameEnd }//Dumi: tracking the current state of the game for pacing purposes.
   
    // Component References
    private GameSessionManager sessionManager;
    private UIManager uiManager;
    private AudioManager audioManager;

    void Start()
    {
        sessionManager = GameSessionManager.Instance;
        uiManager = FindObjectOfType<UIManager>();
        audioManager = GetComponent<AudioManager>();

 
    }

    void Update()
    {
        if (isServer && isStoryStarted && bombTimer > 0)
        {
            bombTimer -= Time.deltaTime;
            uiManager.TimerText.text = bombTimer.ToString("F0"); //Dumi: Show the actual timer for both players to see
            if (bombTimer <= 0)
            {
                GameOver(false);
                return;
            }
        }
    }

    #region Game Flow Management


    [Server]
    public void BeginStoryFlow()
    {
        if (!isStoryStarted)
        {
            isStoryStarted = true;
            StartCoroutine(GameFlow());
            Debug.Log("[Server] Story flow officially started!");
        }
        
    }

    IEnumerator GameFlow() //Dumi: Divided the acts of the story into co-routines that will execute all the story context and the puzzle solving in each act.
    {
        yield return StartCoroutine(ExecuteAct1());
        yield return StartCoroutine(ExecuteAct2());
        yield return StartCoroutine(ExecuteAct3());
        yield return StartCoroutine(ExecuteAct4());
    }

    IEnumerator ExecuteAct1()
    {
        currentAct = GameAct.Act1_Setup;

        //Dumi:  Story Introduction like we discussed
        storyState = StoryState.IntroDialogue;

         RpcShowStoryBeat("A bomb has been planted at St Francis College. Both you and the bomb player are detectives that have been put onto this case to solve the bomb mystery, while uncovering what has happened.You must work together to defuse the bomb before the timer runs out. before time runs out.", PlayerRole.OfficePlayer, 15f);
         RpcShowStoryBeat("A bomb has been planted at St Francis College. Both you and the bomb player are detectives that have been put onto this case to solve the bomb mystery, while uncovering what has happened.You must work together to defuse the bomb before the timer runs out. before time runs out.", PlayerRole.BombPlayer, 15f);
         yield return new WaitForSeconds(4f);

        // Dumi : Sibahle's Light Switch Memory Puzzle with Story Context
        storyState = StoryState.PuzzleSolving;
        RpcShowStoryBeat("The security system  of the school has been tampered with. Restore power to see the bomb clearly.", PlayerRole.OfficePlayer, 2f);

        // Dumi : Start Sibahle's  Light Switch Puzzle
        sessionManager.StartLightSwitchPuzzle();
        yield return new WaitUntil(() => sessionManager.IsLightSwitchComplete());

        //Dumi :  Story Reveal: First glimpse of Zipho's motivation
        storyState = StoryState.StoryReveal;
        RpcShowStoryBeat("Security footage shows a figure entering the building... someone familiar with the layout.", PlayerRole.OfficePlayer, 4f);

        //Dumi:  Anagram Puzzle Introduction
        RpcShowStoryBeat("There's a message on the bomb... 'HATEBYFUELED'. What could this mean ???", PlayerRole.BombPlayer, 3f);
        sessionManager.StartAnagramPuzzle("HAETBEYFUDEL", "FUELEDBYHATE");//Dumi: the scambled verison >>> the corrrect answer to be typed in
        yield return new WaitUntil(() => sessionManager.IsAnagramComplete());

        //Dumi :  Act 1 Story Conclusion
        ziphoMotivationKnown = true;
        RpcShowStoryBeat("FUELED BY HATE... This isn't random. Someone has a personal vendetta.", PlayerRole.OfficePlayer, 4f);
        RpcTriggerAudioCue("suspense_build");
    }

    IEnumerator ExecuteAct2()
    {
        currentAct = GameAct.Act2_Reaction;

        //Dumi :  10-Year Flashback Setup
        storyState = StoryState.StoryReveal;
        RpcShowFlashback();//Dumi: showing the flashback
        yield return new WaitForSeconds(8f);

        //Dumi : Eden's Periodic Table Puzzle with Story Integration
        storyState = StoryState.PuzzleSolving;
        RpcShowStoryBeat("The bomb is connected to Mr. Du Plessis's classroom computer. You need his access code. Is there anything you see, eisther numbers of anyting that could imply an acccess code of some sort?", PlayerRole.OfficePlayer, 3f);
        RpcShowStoryBeat("I see numbers on the bomb display: 32, 28, 92, 16", PlayerRole.BombPlayer, 2f);

        sessionManager.StartPeriodicTablePuzzle(new int[] { 32, 28, 92, 16 }, "GeNiUS");
        yield return new WaitUntil(() => sessionManager.IsPeriodicTableComplete());

        //D: Story Revelation
        flashbackRevealed = true;
        RpcShowStoryBeat("GeNiUS... That's the same word that destroyed a young student's confidence years ago.", PlayerRole.OfficePlayer, 4f);
        RpcShowStoryBeat("This is about Zipho... Mr. Du Plessis's former student. He is on a mission to avenge himself to Mr Du Plessi's for the used he used to 'curse' him.", PlayerRole.OfficePlayer, 3f);

        //D: Increase tension
        RpcTriggerAudioCue("revelation_theme");
        ModifyBombTimer(-30f); // Lose 30 seconds for dramatic effect
    }

    IEnumerator ExecuteAct3()
    {
        currentAct = GameAct.Act3_Action;

        //wait untill the correct wire is cut
        yield return new WaitUntil(() => GameSessionManager.Instance.puzzle2Solved);
        //to include the wire cutting puzzle validation

        // Peak Tension - Zipho's Plan Unfolds
        storyState = StoryState.StoryReveal;
        RpcShowStoryBeat("The bomb is more complex than I  initially thought. Zipho planned this meticulously.", PlayerRole.OfficePlayer, 3f);
        RpcShowStoryBeat("There are multiple wire sequences... each one seems to represent something personal. Maybe we can try figure out what each wire represents as a starting point", PlayerRole.BombPlayer, 3f);

        // Transition to Resolution
        finalChoiceUnlocked = true;
        RpcTriggerAudioCue("climax_resolution");
    }

    IEnumerator ExecuteAct4()
    {
        currentAct = GameAct.Act4_Resolution;

        //Dumi :  Moral Choice Setup
        storyState = StoryState.StoryReveal;
        RpcShowStoryBeat("The bomb has been successfully defused... but Zipho left one final choice.", PlayerRole.OfficePlayer, 4f);
        RpcShowStoryBeat("You can either:", PlayerRole.BombPlayer, 2f);
        RpcShowStoryBeat("1. Completely neutralize everything, or", PlayerRole.OfficePlayer, 2f);
        RpcShowStoryBeat("2. Leave a harmless but symbolic message for Mr. Du Plessis", PlayerRole.BombPlayer, 3f);

        // Dumi : Final Moral Choice Puzzle
        storyState = StoryState.PuzzleSolving;
        sessionManager.StartMoralChoicePuzzle();
        yield return new WaitUntil(() => sessionManager.IsMoralChoiceComplete());

        // Game Resolution Based on Choices
        storyState = StoryState.GameEnd;
        if (storyPoints >= 5) //D Compassionate choices throughout
        {
            RpcShowEnding("redemption");
        }
        else if (storyPoints <= -5) // Harsh choices
        {
            RpcShowEnding("revenge");
        }
        else
        {
            RpcShowEnding("neutral");
        }

        GameOver(true);
    }

    #endregion

    #region Network RPCs

    [ClientRpc]
    void RpcShowStoryBeat(string dialogue, PlayerRole targetPlayer, float duration) // Dumi: Each story beat is role specific and thus I need to obtain the player's role first before the relevant story beat shows
    {
        PlayerRole clientRole = GetClientRole();
        Debug.Log($"Client role: {clientRole}, Target: {targetPlayer}, Should show: {targetPlayer == clientRole || targetPlayer == PlayerRole.None}");

        if (targetPlayer == clientRole || targetPlayer == PlayerRole.None)
        {
            uiManager.DisplayStoryText(dialogue, duration); 
            uiManager.UpdateActDisplay(currentAct);
        }
    }

    [ClientRpc]
    void RpcShowFlashback()
    {
        StartCoroutine(PlayFlashbackSequence());
    }

    IEnumerator PlayFlashbackSequence()
    {
        flashbackManager.StartFlashbackEffect();

        //Dumi:  Initialize the flashback with the intro text
        yield return StartCoroutine(flashbackManager.StartFlashbackDialogue("10 years ago... An exchange between a student and a teacher led to the threat that looms over St Francis college", 5f));


        //Dumi: Then  add each dialogue line progressively
        yield return StartCoroutine(flashbackManager.AddFlashbackDialogue("Zipho: \"Sir, I couldn't do your assignment because we didn't have electricity at home. I promise I'll get it done by tomorrow\"", 4f));

        yield return StartCoroutine(flashbackManager.AddFlashbackDialogue("Mr Du Plessis: \"Zipho, it's always the same excuses with you. This is the 5th time I'm reminding you about this assignment.\"", 4f));

        yield return StartCoroutine(flashbackManager.AddFlashbackDialogue("Zipho: \"Sir, you know me. I'll do it\"", 3f));

        yield return StartCoroutine(flashbackManager.AddFlashbackDialogue("Mr Du Plessis: \"You just don't have it in you Zipho. You'll never be SMART enough in your life with this kind of attitude. You'll never amount to anything.\"", 5f));


        flashbackManager.EndFlashbackEffect();
    }

    [ClientRpc]
    void RpcShowEnding(string endingType)
    {
        switch (endingType)
        {
            case "redemption":
                uiManager.ShowEnding("Zipho's message was delivered, but no one was hurt. Sometimes understanding is enough.", "REDEMPTION");
                break;
            case "revenge":
                uiManager.ShowEnding("The symbolic message was left, but the cycle of hurt continues...", "REVENGE");
                break;
            case "neutral":
                uiManager.ShowEnding("The bomb was defused. The past remains in the past.", "RESOLUTION");
                break;
        }
    }

    [ClientRpc]
    void RpcTriggerAudioCue(string audioKey)
    {
       // audioManager.PlayStoryAudio(audioKey);
    }

    #endregion

    #region SyncVar Hooks

    void OnActChanged(GameAct oldAct, GameAct newAct)
    {
        uiManager.UpdateActDisplay(newAct);
       // audioManager.TransitionToActMusic(newAct);
    }

    void OnStoryStateChanged(StoryState oldState, StoryState newState)
    {
        uiManager.UpdateStoryState(newState);
    }

    #endregion

    #region Utility Methods

    PlayerRole GetClientRole()
    {
        //Dumi: Here I'm tracking the role that each player has chosen using a bool that perfoms a conditional rendering check.By default, the role first picked is the office player
        bool isOffice = !uiManager.OfficeButton.interactable && uiManager.BombButton.interactable;
        return isOffice ? PlayerRole.OfficePlayer : PlayerRole.BombPlayer;
    }

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

    [Server]
    void GameOver(bool success)
    {
        RpcGameOver(success);
    }

    [ClientRpc]
    void RpcGameOver(bool success)
    {
        uiManager.ShowGameOverScreen(success, bombTimer);
    }
    #endregion

}
