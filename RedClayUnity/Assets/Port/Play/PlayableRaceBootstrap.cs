// Starts the playable check in any scene, in Play mode, without any scene setup.
using UnityEngine;

public static class PlayableRaceBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Spawn()
    {
        if (Object.FindFirstObjectByType<PlayableRace>() != null) return;
        new GameObject("PlayableRace").AddComponent<PlayableRace>();
    }
}
