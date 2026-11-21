using TMPro;
using UnityEngine;

public class LapManager : MonoBehaviour
{
    public static LapManager Instance;

    public int totalLaps = 20;

    public TMP_Text lapText;

    private int currentCheckpoint;
    private int currentLap = 1;

    private void Awake()
    {
        Instance = this;
    }

    public void CheckpointPassed(int index)
    {
        if (index != currentCheckpoint)
            return;

        currentCheckpoint++;

        if (currentCheckpoint >= 4)
        {
            currentCheckpoint = 0;
            currentLap++;

            if (currentLap > totalLaps)
            {
                Debug.Log("Race Finished");
            }

            UpdateUI();
        }
    }

    private void UpdateUI()
    {
        if (lapText != null)
        {
            lapText.text =
                $"Lap {currentLap}/{totalLaps}";
        }
    }
}