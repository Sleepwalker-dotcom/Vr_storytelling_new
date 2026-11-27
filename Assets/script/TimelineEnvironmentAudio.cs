using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

public class TimelineEnvironmentAudio : MonoBehaviour
{
    private PlayableDirector director;
    private TimelineClip environmentClip;
    private AudioSource source;
    public float fadeOutDuration = 2f;
    private bool started;
    private bool fading;
    private float fadeElapsed;
    private float fadeStartVolume;

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
                if (clip.displayName != "Environment sound new")
                    continue;
                environmentClip = clip;
                var audio = (AudioPlayableAsset)clip.asset;
                source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.clip = audio.clip;
                source.loop = true;
                source.volume = 1f;
                source.spatialBlend = 0f;
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
        if (source != null)
            source.Stop();
        fading = false;
        started = false;
    }

    private void OnStopped(PlayableDirector stoppedDirector)
    {
        BeginFadeOut();
    }

    private void BeginFadeOut()
    {
        if (source == null || fading || !started)
            return;

        fading = true;
        fadeElapsed = 0f;
        fadeStartVolume = source.volume;
        started = false;
    }

    private void Update()
    {
        if (source == null)
            return;
        if (fading)
        {
            fadeElapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(fadeElapsed / Mathf.Max(fadeOutDuration, 0.01f));
            source.volume = fadeStartVolume * (1f - progress);
            if (progress >= 1f)
            {
                source.Stop();
                fading = false;
            }
            return;
        }
        if (!started && director.state == PlayState.Playing &&
            director.time >= environmentClip.start && director.time < environmentClip.end)
        {
            source.time = (float)environmentClip.clipIn;
            source.volume = 1f;
            source.Play();
            started = true;
        }
        if (started && director.time >= environmentClip.end)
        {
            BeginFadeOut();
        }
    }
}
