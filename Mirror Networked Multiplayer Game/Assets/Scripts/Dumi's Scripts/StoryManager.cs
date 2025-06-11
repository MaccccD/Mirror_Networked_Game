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

    [Header("Audio Management")]
    public AudioSource storyAudioSource;
    public AudioClip[] actTransitionSounds;
    public AudioClip[] characterVoiceLines;

    public enum GameAct { Act1_Setup, Act2_Reaction, Act3_Action, Act4_Resolution }//Dumi: tracks the current act the game is on and show relevant feedback
    public enum StoryState { IntroDialogue, PuzzleSolving, StoryReveal, ActTransition, GameEnd }//Dumi: tracking the current state of the game for pacing purposes.
   

    [System.Serializable]
    public class StoryBeat
    {
        public string dialogue;
        public PlayerRole targetPlayer;
        public float displayDuration;
        public bool requiresBothPlayers;
    }

    [Header("Story Content")]//Dumi: each act has relevant story beats that will show for it which will be  stored inside here.
    public StoryBeat[] act1Beats;
    public StoryBeat[] act2Beats;
    public StoryBeat[] act3Beats;
    public StoryBeat[] act4Beats;

    // Component References
    private GameSessionManager sessionManager;
    private UIManager uiManager;
    private AudioManager audioManager;

    void Start()
    {
        sessionManager = GameSessionManager.Instance;
        uiManager = FindObjectOfType<UIManager>();
        audioManager = GetComponent<AudioManager>();

        if (isServer)
        {
            StartCoroutine(GameFlow());
        }
    }

    void Update()
    {
        if (isServer && bombTimer > 0)
        {
            bombTimer -= Time.deltaTime;
            if (bombTimer <= 0)
            {
                GameOver(false);
                return;
            }
        }
    }

    #region Game Flow Management

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

        //Dumi:  Story Introduction
        storyState = StoryState.IntroDialogue;
        RpcShowStoryBeat("A bomb has been planted at St Francis College...", PlayerRole.OfficePlayer, 3f);
        RpcShowStoryBeat("You must work together to defuse it before time runs out.", PlayerRole.BombPlayer, 3f);
        yield return new WaitForSeconds(4f);

        // Dumi : Sibahle's Light Switch Memory Puzzle with Story Context
        storyState = StoryState.PuzzleSolving;
        RpcShowStoryBeat("The security system has been tampered with. Restore power to see the bomb clearly.", PlayerRole.OfficePlayer, 2f);

        // Dumi : Start Sibahle's  Light Switch Puzzle
        sessionManager.StartLightSwitchPuzzle();
        yield return new WaitUntil(() => sessionManager.IsLightSwitchComplete());

        //Dumi :  Story Reveal: First glimpse of Zipho's motivation
        storyState = StoryState.StoryReveal;
        RpcShowStoryBeat("Security footage shows a figure entering the building... someone familiar with the layout.", PlayerRole.OfficePlayer, 4f);

        //Dumi:  Anagram Puzzle Introduction
        RpcShowStoryBeat("There's a message on the bomb... 'HATEBYFUELED'", PlayerRole.BombPlayer, 3f);
        sessionManager.StartAnagramPuzzle("HAETBEYFUDEL", "FUELEDBYHATE");//Dumi: the scamb;ed verison >>> the corrrect answer to be typed in
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
        RpcShowStoryBeat("The bomb is connected to Mr. Du Plessis's classroom computer. You need his access code.", PlayerRole.OfficePlayer, 3f);
        RpcShowStoryBeat("I see numbers on the bomb display: 32, 28, 92, 16", PlayerRole.BombPlayer, 2f);

        sessionManager.StartPeriodicTablePuzzle(new int[] { 32, 28, 92, 16 }, "GeNiUS");
        yield return new WaitUntil(() => sessionManager.IsPeriodicTableComplete());

        //D: Story Revelation
        flashbackRevealed = true;
        RpcShowStoryBeat("GeNiUS... The same word that destroyed a young student's confidence years ago.", PlayerRole.OfficePlayer, 4f);
        RpcShowStoryBeat("This is about Zipho... Mr. Du Plessis's former student.", PlayerRole.OfficePlayer, 3f);

        //D: Increase tension
        RpcTriggerAudioCue("revelation_theme");
        ModifyBombTimer(-30f); // Lose 30 seconds for dramatic effect
    }

    IEnumerator ExecuteAct3()
    {
        currentAct = GameAct.Act3_Action;

        //wait untill the correct wire is cut
        yield return new WaitUntil(() => GameSessionManager.Instance.puzzle2Solved);
        //to iunclude the wire cutting puzzle validation

        // Peak Tension - Zipho's Plan Unfolds
        storyState = StoryState.StoryReveal;
        RpcShowStoryBeat("The bomb is more complex than initially thought. Zipho planned this meticulously.", PlayerRole.OfficePlayer, 3f);
        RpcShowStoryBeat("There are multiple wire sequences... each one seems to represent something personal.", PlayerRole.BombPlayer, 3f);

        // Transition to Resolution
        finalChoiceUnlocked = true;
        RpcTriggerAudioCue("climax_resolution");
    }

    IEnumerator ExecuteAct4()
    {
        currentAct = GameAct.Act4_Resolution;

        //Dumi :  Moral Choice Setup
        storyState = StoryState.StoryReveal;
        RpcShowStoryBeat("The bomb is defused... but Zipho left one final choice.", PlayerRole.OfficePlayer, 4f);
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
    void RpcShowStoryBeat(string dialogue, PlayerRole targetPlayer, float duration)
    {
        // Check if this client should receive this story beat
        PlayerRole clientRole = GetClientRole();
        if (targetPlayer == clientRole || targetPlayer == PlayerRole.None)
        {
            uiManager.DisplayStoryText(dialogue, duration);
        }
    }

    [ClientRpc]
    void RpcShowFlashback()
    {
        StartCoroutine(PlayFlashbackSequence());
    }

    IEnumerator PlayFlashbackSequence()
    {
        uiManager.StartFlashbackEffect();

        // 10 years ago flashback dialogue
        yield return StartCoroutine(uiManager.ShowFlashbackDialogue("10 years ago...", 2f));
        yield return StartCoroutine(uiManager.ShowFlashbackDialogue("'You'll never amount to anything, Zipho. You're just not smart enough.'", 4f));
        yield return StartCoroutine(uiManager.ShowFlashbackDialogue("- Mr. Du Plessis", 2f));

        uiManager.EndFlashbackEffect();
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
        // Determine if this client is Office or Bomb player
        // This would be set during connection/room joining
        return NetworkClient.localPlayer.GetComponent<PlayerRole>();
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
