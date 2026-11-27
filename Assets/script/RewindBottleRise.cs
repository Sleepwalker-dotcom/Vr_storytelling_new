using UnityEngine;
using System.Linq;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[DefaultExecutionOrder(11000)]
public class RewindBottleRise : MonoBehaviour
{
    public Transform bottleGroup;
    public Transform singlePhoto;
    public float targetHeight = 2.5f;
    public float turns = 2f;

    private PlayableDirector director;
    private TimelineClip rewindClip;
    private Transform[] bottles;
    private Vector3[] positions;
    private Quaternion[] rotations;
    private Vector3[] rotationAxes;
    private Rigidbody[] bodies;
    private bool[] kinematic;
    private XRBaseInteractable[] interactables;
    private bool[] interactionEnabled;
    private bool running;

    private void Awake()
    {
        director = GetComponent<PlayableDirector>();
        var timeline = director.playableAsset as TimelineAsset;
        foreach (var track in timeline.GetOutputTracks())
        {
            if (!(track is AudioTrack))
                continue;
            foreach (var clip in track.GetClips())
            {
                var audio = clip.asset as AudioPlayableAsset;
                if (audio != null && audio.clip != null && audio.clip.name == "Rewind")
                    rewindClip = clip;
            }
        }
    }

    private void OnEnable()
    {
        GetComponent<PlayableDirector>().stopped += OnStopped;
    }

    private void OnDisable()
    {
        GetComponent<PlayableDirector>().stopped -= OnStopped;
        Finish();
    }

    private void OnStopped(PlayableDirector source)
    {
        Finish();
    }

    private void LateUpdate()
    {
        if (rewindClip == null || bottleGroup == null)
            return;
        bool inside = director.time >= rewindClip.start && director.time < rewindClip.end;
        if (!inside)
        {
            Finish();
            if (director.state == PlayState.Playing && director.time >= rewindClip.end)
            {
                bottleGroup.gameObject.SetActive(false);
                if (singlePhoto != null)
                    singlePhoto.gameObject.SetActive(false);
            }
            return;
        }
        if (!running)
            Begin();

        float progress = Mathf.SmoothStep(0f, 1f,
            (float)((director.time - rewindClip.start) / rewindClip.duration));
        for (int i = 0; i < bottles.Length; i++)
        {
            Vector3 position = positions[i];
            position.y = Mathf.Lerp(position.y, targetHeight, progress);
            bottles[i].SetPositionAndRotation(position,
                Quaternion.AngleAxis(360f * turns * progress, rotationAxes[i]) * rotations[i]);
        }
    }

    private void Begin()
    {
        bottles = new Transform[bottleGroup.childCount + (singlePhoto != null ? 1 : 0)];
        positions = new Vector3[bottles.Length];
        rotations = new Quaternion[bottles.Length];
        rotationAxes = new Vector3[bottles.Length];
        for (int i = 0; i < bottles.Length; i++)
        {
            bottles[i] = i < bottleGroup.childCount ? bottleGroup.GetChild(i) : singlePhoto;
            rotationAxes[i] = Random.onUnitSphere;
            positions[i] = bottles[i].position;
            rotations[i] = bottles[i].rotation;
        }

        interactables = bottleGroup.GetComponentsInChildren<XRBaseInteractable>(true);
        if (singlePhoto != null)
            interactables = interactables.Concat(singlePhoto.GetComponentsInChildren<XRBaseInteractable>(true)).ToArray();
        interactionEnabled = new bool[interactables.Length];
        for (int i = 0; i < interactables.Length; i++)
        {
            interactionEnabled[i] = interactables[i].enabled;
            interactables[i].enabled = false;
        }
        bodies = bottleGroup.GetComponentsInChildren<Rigidbody>(true);
        if (singlePhoto != null)
            bodies = bodies.Concat(singlePhoto.GetComponentsInChildren<Rigidbody>(true)).ToArray();
        kinematic = new bool[bodies.Length];
        for (int i = 0; i < bodies.Length; i++)
        {
            kinematic[i] = bodies[i].isKinematic;
            bodies[i].isKinematic = true;
        }
        bottleGroup.gameObject.SetActive(true);
        running = true;
    }

    private void Finish()
    {
        if (!running)
            return;
        running = false;
        bottleGroup.gameObject.SetActive(false);
        if (singlePhoto != null)
            singlePhoto.gameObject.SetActive(false);
        for (int i = 0; i < bottles.Length; i++)
            bottles[i].SetPositionAndRotation(positions[i], rotations[i]);
        for (int i = 0; i < bodies.Length; i++)
            bodies[i].isKinematic = kinematic[i];
        for (int i = 0; i < interactables.Length; i++)
            interactables[i].enabled = interactionEnabled[i];
    }
}
