using UnityEngine;
using TMPro;
using Mirror;

public class SyncedTimer : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnTimeChanged))]
    public float currentTime = 60f;

    public TMP_Text timerText;

    public bool isCountingDown = true;

    void Update()
    {
        if (!isServer) return;

        if (isCountingDown && currentTime > 0f)
        {
            currentTime -= Time.deltaTime;

            if (currentTime <= 0f)
            {
                currentTime = 0f;
                isCountingDown = false;
                Debug.Log("Timer ended!");
                // You could trigger an event here or winning / losing screen
            }
        }
    }

    // This hook runs on all clients whenever the SyncVar changes
    void OnTimeChanged(float oldTime, float newTime)
    {
        UpdateTimerUI(newTime);
    }

    void UpdateTimerUI(float timeToShow)
    {
        if (timerText != null)
        {
            timerText.text = timeToShow.ToString("F0"); // this makes or converts the timer so that it does not show decimals.
        }
    }

    // Optional: manually sync timer UI on client start
    public override void OnStartClient()
    {
        base.OnStartClient();
        UpdateTimerUI(currentTime);
    }
} 