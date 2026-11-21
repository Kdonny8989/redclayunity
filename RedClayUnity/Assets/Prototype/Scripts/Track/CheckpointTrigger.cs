using UnityEngine;

public class CheckpointTrigger : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        TrackCheckpoint cp =
            GetComponent<TrackCheckpoint>();

        LapManager.Instance.CheckpointPassed(
            cp.checkpointIndex);
    }
}