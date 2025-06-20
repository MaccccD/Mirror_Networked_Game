using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*Dumi: This script controls all the audio stuff that is triggered in the story manager in each act. It checks if the correct asio key pair is mapped to the cporrect audio sourcec
 so that when a specific key is called, t plays that specifc sound effect via its corresponding audio source*/


public class AudioManager : MonoBehaviour
{
    [System.Serializable]
    public class AudioKeyPair //D: each audio has an audioey and its corresponding audio source.
    {
        public string audioKey;
        public AudioSource audioSource;
    }

    [Header("Audio Key Mappings")]
    [SerializeField]
    private AudioKeyPair[] audioMappings = new AudioKeyPair[] // D: creating connection betwee an audio key and its corresponding audio source.
   {
        new AudioKeyPair { audioKey = "suspense_build", audioSource = null },
        new AudioKeyPair { audioKey = "revelation_theme", audioSource = null },
        new AudioKeyPair { audioKey = "climax_resolution", audioSource = null },
        new AudioKeyPair { audioKey = "fast_heartbeat", audioSource = null},
        new AudioKeyPair { audioKey = "success", audioSource = null}
   };

    //D: Dictionary for fast lookup during runtime
    private Dictionary<string, AudioSource> audioMap;

    void Awake()
    {
        InitializeAudioMap();
    }

    void InitializeAudioMap()
    {
        audioMap = new Dictionary<string, AudioSource>();

        //D: Build dictionary from the serialized array
        foreach (AudioKeyPair pair in audioMappings)
        {
            if (!string.IsNullOrEmpty(pair.audioKey))
            {
                audioMap[pair.audioKey] = pair.audioSource; //D: so i'm assigning each audio key to an audio source , as long as the audio source exists in the scene.
            }
        }

        Debug.Log($"Audio Manager has been  initialized with {audioMap.Count} audio mappings");
    }
    public void PlayStoryAudio(string audioKey)
    {
        if (audioMap.TryGetValue(audioKey, out AudioSource audioSource))
        {
            if (audioSource != null)
            {
                //D: Stop current audio if playing to avoid overlap
                if (audioSource.isPlaying)
                {
                    audioSource.Stop();
                }

                audioSource.Play();
                Debug.Log($"Playing audio: {audioKey} and it works");

                // D: Start coroutine to stop after 5 seconds
                StartCoroutine(StopPlayingAudio(audioSource, audioKey));
            }
            else
            {
                Debug.LogWarning($"AudioSource for key '{audioKey}' is not assigned!");
            }
        }
        else
        {
            Debug.LogWarning($"Audio key '{audioKey}' not found! Available keys: {string.Join(", ", audioMap.Keys)}");
        }
    }

    private IEnumerator StopPlayingAudio(AudioSource audioSource, string audioKey)
    {
        yield return new WaitForSeconds(5f);

        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
            Debug.Log($"Stopped audio: {audioKey} after 5 seconds");
        }
    }

}
