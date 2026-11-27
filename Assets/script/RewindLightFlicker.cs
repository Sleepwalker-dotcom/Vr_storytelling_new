using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[DefaultExecutionOrder(11000)]
public class RewindLightFlicker : MonoBehaviour
{
    public Light targetLight;
    public float peakIntensity = 2f;
    public float frequency = 6f;
    public float recoveryDelay = 3f;
    public float recoveryDuration = 7f;
    private bool recovering;
    private float recoveryElapsed;
    private PlayableDirector director;
    private TimelineClip rewindClip;
    private bool flickering;
    private float originalIntensity;
    private bool originalEnabled;

    private void Awake()
    {
        director = GetComponent<PlayableDirector>();
        var timeline = director.playableAsset as TimelineAsset;
        if (timeline == null)
            return;

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
        Restore();
    }

    private void OnStopped(PlayableDirector source)
    {
        BeginRecovery();
    }

    private void LateUpdate()
    {
        if (targetLight == null || rewindClip == null)
            return;

        bool active = director.state == PlayState.Playing &&
            director.time >= rewindClip.start && director.time < rewindClip.end;
        if (!active)
        {
            if (flickering)
            {
                BeginRecovery();
                return;
            }
            if (recovering)
            {
                recoveryElapsed += Time.deltaTime;
                float progress = Mathf.Clamp01((recoveryElapsed - recoveryDelay) / Mathf.Max(recoveryDuration, 0.01f));
                float targetIntensity = originalIntensity > 0f ? originalIntensity : peakIntensity;
                targetLight.enabled = true;
                targetLight.intensity = targetIntensity * progress;
                if (progress >= 1f)
                    recovering = false;
            }
            return;
        }

        if (!flickering)
        {
            if (!recovering)
            {
                originalIntensity = targetLight.intensity;
                originalEnabled = targetLight.enabled;
            }
            recovering = false;
            flickering = true;
        }

        float phase = (float)(director.time - rewindClip.start) * frequency;
        targetLight.enabled = true;
        targetLight.intensity = peakIntensity * (Mathf.Repeat(phase, 1f) < 0.5f ? 1f : 0.1f);
    }

    private void BeginRecovery()
    {
        if (!flickering || targetLight == null)
            return;

        flickering = false;
        recovering = true;
        recoveryElapsed = 0f;
        targetLight.intensity = 0f;
    }

    private void Restore()
    {
        if ((!flickering && !recovering) || targetLight == null)
            return;

        targetLight.intensity = originalIntensity;
        targetLight.enabled = originalEnabled;
        recovering = false;
        flickering = false;
    }
}
