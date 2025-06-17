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

    [Header("Animations")]
    public Animator pageFlip;

  //  [Header("Audio Management")]
  //  public AudioSource storyAudioSource;
   // public AudioClip[] actTransitionSounds;
 

    public enum GameAct { Act1_Setup, Act2_Reaction, Act3_Action, Act4_Resolution }//Dumi: tracks the current act the game is on and show relevant feedback
    public enum StoryState { IntroDialogue, PuzzleSolving, StoryReveal, ActTransition, GameEnd }//Dumi: tracking the current state of the game for pacing purposes.
   
    // Component References
    private GameSessionManager sessionManager;
    private UIManager uiManager;
    public AudioManager audioManager;

    void Start()
    {
        sessionManager = GameSessionManager.Instance;
        uiManager = FindObjectOfType<UIManager>();
        audioManager = FindObjectOfType<AudioManager>();
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
     
        RpcShowStoryBeat("A bomb has been planted at St Francis College. Both you and the bomb player are detectives that have been put onto this case to solve the bomb mystery, while uncovering what has happened.You guys are both on different rooms but you will be able to see each other's thoughts as you work via the text chat together to defuse the bomb before the timer runs out.", PlayerRole.OfficePlayer, 10f);
        RpcShowStoryBeat("A bomb has been planted at St Francis College. Both you and the office player are detectives that have been put onto this case to solve the bomb mystery, while uncovering what has happened.You guys are both on different rooms but you will be able to see each other's thoughts as you work via the text chat together to defuse the bomb before the timer runs out.", PlayerRole.BombPlayer, 10f);
        yield return new WaitForSeconds(10f);
       
         RpcShowThoughtBubble("The security system  of the school has been tampered with. Restore the power back by turning on the light switch to see the bomb set up clearly.", PlayerRole.OfficePlayer, 10f);
         RpcShowThoughtBubble("The security system  of the school has been tampered with. Restore the power back by turning on the light switch to see the bomb set up clearly.", PlayerRole.BombPlayer, 10f);
         yield return new WaitForSeconds(10f);
         Debug.Log("I am also showing hence you can see the debug here");


        // Dumi : Start Sibahle's  Light Switch Puzzle
        storyState = StoryState.PuzzleSolving;
        sessionManager.StartLightSwitchPuzzle();
        yield return new WaitUntil(() => sessionManager.IsLightSwitchComplete());

        //Dumi :  Story Reveal: First glimpse of Zipho's motivation
        storyState = StoryState.StoryReveal;
    
        RpcShowThoughtBubble("The lights are now on and the bomb player can now see the bomb display. By tampering with the security system to cause darkness, someone was trying to plant a bomb in the dark. But why ?", PlayerRole.OfficePlayer, 6f);
        RpcShowThoughtBubble("The lights are now on and I can see the bomb display. By tampering with the security system to cause darkness, someone was trying to plant a bomb in the dark. But why ?", PlayerRole.BombPlayer, 6f);

        //Dumi: Periodic Table Puzzle Introdcution.
        RpcShowThoughtBubble("To find out the reason why a bomb has been planted, we need to access any computer files that might gives us information.", PlayerRole.BombPlayer, 6f);
        RpcShowThoughtBubble("To find out the reason why a bomb has been planted, we need to access any computer files that might gives us information.", PlayerRole.OfficePlayer, 6f);
       
        //Dumi: Now start periodic table puzzle
        storyState = StoryState.PuzzleSolving;
        RpcShowThoughtBubble("Let me ask my partner what they see on their screen via the text chat", PlayerRole.OfficePlayer, 3f);
        RpcShowThoughtBubble("Let me ask my partner what they see on their screen via the text chat", PlayerRole.BombPlayer, 3f);

        sessionManager.StartPeriodicTablePuzzle(new int[] { 32, 28, 92, 16 }, "GeNiUS");
        yield return new WaitUntil(() => sessionManager.IsPeriodicTableComplete());

        //Dumi :  Act 1 Story Conclusion
        ziphoMotivationKnown = true;
        
        RpcShowThoughtBubble("The computer belongs to Mr. Du Plessis, Head of IT at St Francis College. But why would someone target him specifically?", PlayerRole.OfficePlayer, 4f);
        RpcShowThoughtBubble("The computer belongs to Mr. Du Plessis, Head of IT at St Francis College. But why would someone target him specifically?", PlayerRole.BombPlayer, 4f);
        RpcTriggerAudioCue("suspense_build");
        Debug.Log("Suspense build audio is playing that's why you see me");

    }

    IEnumerator ExecuteAct2()
    {
        currentAct = GameAct.Act2_Reaction;

        //Dumi :  10-Year Flashback Setup
        storyState = StoryState.StoryReveal;
        RpcShowFlashback();//Dumi: showing the flashback
        flashbackRevealed = true;
        yield return new WaitForSeconds(10f);

        //Dumi: @sibahle you can trigger the page flip anim here :
        RpcTriggerPageAnimation();//Sibahle: page flip animation
        RpcShowStoryBeat("Mr. Du Plessis is a Computer Science teacher... that explains the technical sophistication of this bomb. Zipho must have spent years mastering the very skills Du Plessis said he'd never have.", PlayerRole.OfficePlayer, 8f);
        RpcShowStoryBeat("Mr. Du Plessis is a Computer Science teacher... that explains the technical sophistication of this bomb. Zipho must have spent years mastering the very skills Du Plessis said he'd never have.", PlayerRole.BombPlayer, 8f);

        storyState = StoryState.PuzzleSolving;
        sessionManager.StartAnagramPuzzle("hguonetramston", "notsmartenough");
        yield return new WaitUntil(() => sessionManager.IsAnagramComplete());

        //Dumi: @sibahle you can trigger the page flip anim here :
        RpcTriggerPageAnimation();//Sibahle: page flip animation
        RpcShowStoryBeat("Zipho's personal vendetta against Mr Du Plessi's was not random. It was caused by the teacher's words to him", PlayerRole.OfficePlayer, 4f);
        RpcShowStoryBeat("Mr Du Plessis said those words to Zipho to motivate him to take his work seriously.It was meant to build him, not destroy him confidence.", PlayerRole.BombPlayer, 3f);

        //D: Increase tension
        RpcTriggerAudioCue("revelation_theme");
        Debug.Log("revelation theme build audio is playing that's why you see me");
        ModifyBombTimer(-30f); // Lose 30 seconds for dramatic effect
    }

    IEnumerator ExecuteAct3()
    {
        currentAct = GameAct.Act3_Action;

        //Dumi Peak Tension - Zipho's Plan Unfolds
        storyState = StoryState.StoryReveal;
        //Dumi: @sibahle you can trigger the page flip anim here :
        RpcTriggerPageAnimation();//Sibahle: page flip animation
        RpcShowStoryBeat("The bomb is more complex than I  initially thought. Zipho planned this strategically.", PlayerRole.OfficePlayer, 5f);
        RpcShowStoryBeat("There are multiple wire sequences... each one seems to represent something personal about  Zipho's motives.Each wire color represents a different emotion from that day. Find out what each wire represents as a starting point", PlayerRole.BombPlayer, 8f);


        //Dumi: @sibahle you can trigger the page flip anim here :
        RpcTriggerPageAnimation();//Sibahle: page flip animation
        sessionManager.StartWireCutRepresentation("red");
        sessionManager.StartWireCutRepresentation("blue");
        sessionManager.StartWireCutRepresentation("yellow");
        sessionManager.StartWireCutRepresentation("green");



        //Dumi" start the wire cut puzzle:
        // Dumi : Sibahle's Light Switch Memory Puzzle with Story Context
        storyState = StoryState.PuzzleSolving;
        sessionManager.StartWireCutPuzzle();
        yield return new WaitUntil(() => sessionManager.puzzle2Solved);

        //Dumi: @sibahle you can trigger the page flip anim here :
        RpcTriggerPageAnimation();//Sibahle: page flip animation
        RpcShowStoryBeat("The wires have been cut but the timer on the bomb has not stopped. Zipho also used AI to further encyrpt this bomb", PlayerRole.OfficePlayer, 7f);
        RpcShowStoryBeat("If AI was used to further encrypt the bomb, then Zipho must have made a mistake somewhere. Look for something digital that Zipho may have used to get the AI encryption system", PlayerRole.BombPlayer, 7f);

        // Dumi : Sibahle's Light Switch Memory Puzzle with Story Context
        storyState = StoryState.PuzzleSolving;
        sessionManager.StartChalkPuzzle();
        yield return new WaitUntil(() => sessionManager.puzzle1Solved);

        //Dumi: @sibahle you can trigger the page flip anim here :
        RpcTriggerPageAnimation();//Sibahle: page flip animation
        RpcShowStoryBeat("This level of AI integration... Zipho didn't just learn programming, he became an expert. After 10 years of proving himself, something must have triggered this revenge plot.", PlayerRole.BombPlayer, 10f);
        RpcShowStoryBeat("This level of AI integration... Zipho didn't just learn programming, he became an expert. After 10 years of proving himself, something must have triggered this revenge plot.", PlayerRole.OfficePlayer, 10f);
        RpcShowStoryBeat("The password to get extra information from the AI assitant that Zipho  used to further encrypt the bomb has been retrieved. However, the bomb timer is still on. There is something else he used to set up the bomb detonation.", PlayerRole.OfficePlayer, 9f); 
        RpcShowStoryBeat("Some bombs have  button(s) that need to be pressed in a specific order to switch it off. Zipho had this in mind while planting this bomb threat. Find the buttons that need to be disabled in the bomb. ", PlayerRole.OfficePlayer, 9f);
      

        RpcTriggerAudioCue("climax_resolution");
        Debug.Log("climax build audio is playing that's why you see me");

    }

    IEnumerator ExecuteAct4()
    {
        currentAct = GameAct.Act4_Resolution;

       
        storyState = StoryState.PuzzleSolving;
        sessionManager.StartBombDisablePuzzle();
        yield return new WaitUntil(() => sessionManager.bombdifuseComplete);

        // //Dumi: @sibahle you can trigger the page flip anim here :
        RpcTriggerPageAnimation();//Sibahle: page flip animation
        RpcShowStoryBeat("Du Plessis probably doesn't even remember that day 10 years ago. But Zipho never forgot. The question is - does destroying a man's life over forgotten words solve anything?", PlayerRole.OfficePlayer, 8f);
        RpcShowStoryBeat("Du Plessis probably doesn't even remember that day 10 years ago. But Zipho never forgot. The question is - does destroying a man's life over forgotten words solve anything?", PlayerRole.BombPlayer, 9f);

        //Dumi :  Moral Choice Setup
        storyState = StoryState.StoryReveal;
        //Dumi: @sibahle you can trigger the page flip anim here :
        RpcTriggerPageAnimation();//Sibahle: page flip animation
        RpcShowStoryBeat("The bomb has been successfully defused... but Zipho left one final choice.", PlayerRole.OfficePlayer, 4f);
        RpcShowStoryBeat("You can either:", PlayerRole.BombPlayer, 2f);
        RpcShowStoryBeat("1. Completely neutralize everything. Meaning ; erase everything, let  Mr Du Plessis never know how much damage his words caused", PlayerRole.OfficePlayer, 2f);
        RpcShowStoryBeat("2. Leave a message. Meaning; a clear reminder that words have consequences", PlayerRole.BombPlayer, 3f);

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
            uiManager.UpdateActDisplay(currentAct);
            uiManager.DisplayStoryText(dialogue, duration); 
        }
    }

    [ClientRpc]
    void RpcShowThoughtBubble(string dialogue, PlayerRole targetPlayer, float duration) // Dumi: Each story beat is role specific and thus I need to obtain the player's role first before the relevant story beat shows
    {
        PlayerRole clientRole = GetClientRole();
        Debug.Log($"Client role: {clientRole}, Target: {targetPlayer}, Should show: {targetPlayer == clientRole || targetPlayer == PlayerRole.None}");

        if (targetPlayer == clientRole || targetPlayer == PlayerRole.None)
        {
            uiManager.DisplaySpeechBubble(dialogue, duration);
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
       audioManager.PlayStoryAudio(audioKey);
    }

    [ClientRpc]
    void RpcTriggerPageAnimation()
    {
        if (pageFlip != null)
        {
            pageFlip.SetTrigger("PageFlip");
        }
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
