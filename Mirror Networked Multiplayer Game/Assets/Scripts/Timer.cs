using UnityEngine;
using TMPro;

public class Timer : MonoBehaviour
{
    public float remainingTime;
    public float currentTime = 600f; //Sibahle: setting countdown timer for 10 minutes
    public TMP_Text timerText;

    public bool isPaused = false; //Sibahle: this is for the end of the game

    void Start()
    {
        remainingTime = currentTime;
    }

    void Update()
    {
        if (isPaused) return;

        if (remainingTime > 0)
        {
            remainingTime -= Time.deltaTime;
        }
        else if (remainingTime < 0)
        {
            remainingTime = 0;
        }
        int minutes = Mathf.FloorToInt(remainingTime / 60);
        int seconds = Mathf.FloorToInt(remainingTime % 60);
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    public void PauseTimer()
    {
        isPaused = true;
        Debug.Log("Game is Complete!");
    }
}