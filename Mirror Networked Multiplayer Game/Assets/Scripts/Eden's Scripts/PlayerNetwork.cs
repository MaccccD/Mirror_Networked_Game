using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Mirror;
using TMPro;

//Eden: This script ensures that the start screen automatically shown (even if other player has
//not joined yet. Locally shows UI

public class PlayerNetwork : NetworkBehaviour
{
    private bool isSpeaking = false;  // Flag to track if the player is speaking
    private string microphoneDevice;
    private AudioSource audioSource;
    private AudioClip microphoneClip;
    public UIManager youEye;

    // Unity's Start method to initialize things (if needed)
    private void Start()
    {
        if (isLocalPlayer)
        {
            // Initialize audio source for playback
            audioSource = GetComponent<AudioSource>();
            // Optionally: You can initialize UI or other logic for the local player here
            UIManager.Instance.ShowStartUI();
            OdinHandler.Instance.JoinRoom("TheLitZone");

            // Initialize microphone
            StartCoroutine(InitializeMicrophone());
        }
    }
    //Eden: This function called on local client when player object set up (by mirror)
    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        // Show the UI (UIManager should be handling this)
        UIManager.Instance.ShowStartUI();
        OdinHandler.Instance.JoinRoom("TheLitZone");

        // Now, let's start checking the player's microphone status.
        StartCoroutine(CheckIfPlayerIsSpeaking());
    }

    // Initialize microphone input
    private IEnumerator InitializeMicrophone()
    {
        // Try to get the microphone device name (first device if available)
        if (Microphone.devices.Length > 0)
        {
            microphoneDevice = Microphone.devices[0];
            microphoneClip = Microphone.Start(microphoneDevice, true, 10, 44100);
            while (Microphone.GetPosition(microphoneDevice) <= 0)  // Wait for the microphone to initialize
                yield return null;
        }
        else
        {
            Debug.LogError("No microphone devices found!");
        }
    }

    // Called periodically to check if the player is speaking
    private IEnumerator CheckIfPlayerIsSpeaking()
    {
        while (true)
        {
            if (Microphone.IsRecording(microphoneDevice))
            {
                if (!isSpeaking)
                {
                    isSpeaking = true;
                    Debug.Log("Player started speaking.");
                    // Optionally, you can add something to show on-screen, like UI or text
                }
            }
            else
            {
                if (isSpeaking)
                {
                    isSpeaking = false;
                    Debug.Log("Player stopped speaking.");
                    // Optionally, add UI or other actions when the player stops speaking
                }
            }

            yield return new WaitForSeconds(0.1f);  // Check every 0.1 seconds or adjust the time as needed
        }
    }

    // Called when the player is speaking (triggered somewhere in your voice chat system)
    public void OnPlayerSpeaking(string playerName)
    {
        // This is where you would call RpcOnVoiceDataReceived() to notify other players
        RpcOnVoiceDataReceived(playerName);
    }

    // RPC method to relay voice data to other players
    [ClientRpc]
    public void RpcOnVoiceDataReceived(string playerName)
    {
        if (isLocalPlayer)
        {
            Debug.Log($"{playerName} is speaking and you can hear them.");
         
            // Here you can play the voice clip or handle any logic for voice playback
        }
       
    }

    // A method to stop the microphone recording
    public void StopMicrophone()
    {
        if (Microphone.IsRecording(microphoneDevice))
        {
            Microphone.End(microphoneDevice);
        }
    }

    // A method to start the microphone recording (triggered if needed)
    public void StartMicrophone()
    {
        if (!Microphone.IsRecording(microphoneDevice))
        {
            microphoneClip = Microphone.Start(microphoneDevice, true, 10, 44100);
        }
    }

    // Update method (you could optionally use this to send/receive voice data continuously)
    private void Update()
    {
        // Play voice data or handle any logic for voice playback
        if (audioSource != null && microphoneClip != null)
        {
            // If the player is speaking, we can play back the microphone data.
            if (isSpeaking && !audioSource.isPlaying)
            {
                audioSource.clip = microphoneClip;
                audioSource.Play();
            }
        }
    }
}
