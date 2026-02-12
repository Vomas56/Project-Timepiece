using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class BattleTimer : MonoBehaviour
{
    public float timeRemaining = 60f;
    public bool timerRunning = false;

    public TextMeshProUGUI timerText;

    void Start()
    {
        timerRunning = true;
        UpdateTimerDisplay();
    }

    void Update()
    {
        if (!timerRunning) return;

        if (timeRemaining > 0)
        {
            timeRemaining -= Time.deltaTime;
            UpdateTimerDisplay();
        }
        else
        {
            timeRemaining = 0;
            timerRunning = false;
            TimerEnded();
        }
    }

    void UpdateTimerDisplay()
    {
        int minutes = Mathf.FloorToInt(timeRemaining / 60);
        int seconds = Mathf.FloorToInt(timeRemaining % 60);

        timerText.text = $"{minutes:00}:{seconds:00}";
    }

    void TimerEnded()
    {
        Debug.Log("Timer finished!");
        // Trigger win/lose condition here
    }
}
