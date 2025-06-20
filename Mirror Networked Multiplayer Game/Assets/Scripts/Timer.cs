using UnityEngine;
using TMPro;
using Mirror;

public class Timer : NetworkBehaviour
{
    [SyncVar] private double startTime;   
    [SyncVar] private float duration;      
    public TMP_Text timerText;
    private bool isPaused;

    public void Initialize(double serverStartTime, float countdownLength)
    {
        startTime = serverStartTime;
        duration = countdownLength;
        isPaused = false;
    }

    void Update()
    {
        if (isPaused) return;
        if (startTime <= 0) return;

        float elapsed = (float)(NetworkTime.time - startTime);
        float remaining = Mathf.Clamp(duration - elapsed, 0, duration);

        int mins = Mathf.FloorToInt(remaining / 60f);
        int secs = Mathf.FloorToInt(remaining % 60f);
        timerText.text = $"{mins:00}:{secs:00}";
    }

    public void PauseTimer()
    {
        isPaused = true;
        Debug.Log("Timer paused");
    }
}
