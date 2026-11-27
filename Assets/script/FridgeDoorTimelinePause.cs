using UnityEngine;
using UnityEngine.Playables;

public class FridgeDoorTimelinePause : MonoBehaviour
{
    public FridgeDoor door;
    private PlayableDirector director;
    private bool waiting;

    private void Awake()
    {
        director = GetComponent<PlayableDirector>();
    }

    public void PauseForDoor()
    {
        if (director.time < 50 || door == null || door.OpenAngle >= 15f)
            return;

        waiting = true;
        director.Pause();
        Debug.Log("[FridgeDoorTimelinePause] MainScene paused, waiting for door to reach 15 degrees.");
    }

    private void Update()
    {
        if (waiting && door.OpenAngle >= 15f)
        {
            waiting = false;
            director.Resume();
        }
    }
}
