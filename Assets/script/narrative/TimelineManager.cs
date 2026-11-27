using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

public class TimelineManager : MonoBehaviour
{
    public static TimelineManager Instance;

    void Awake()
    {
        Instance = this;
    }

    public void Play(TimelineAsset timeline, Action onFinish)
    {
        PlayableDirector target = null;
        var allDirectors = FindObjectsByType<PlayableDirector>(FindObjectsSortMode.None);

        Debug.Log($"[TimelineManager] 场景里共有 {allDirectors.Length} 个Director");
        foreach (var d in allDirectors)
        {
            Debug.Log($"[TimelineManager] 找到Director: {d.gameObject.name}, Asset: {d.playableAsset?.name}");
            if (d != null && d.playableAsset == timeline)
            {
                target = d;
                break;
            }
        }

        if (target == null)
        {
            Debug.LogError($"[TimelineManager] 找不到对应的Director: {timeline.name}");
            onFinish?.Invoke();
            return;
        }

        Debug.Log($"[TimelineManager] 开始播放: {timeline.name}, duration: {target.duration}");

        var trigger = target.GetComponent<TimelineTrigger>();
        if (trigger != null)
            trigger.ResetTriggers();

        target.Stop();
        target.time = 0;
        target.Play();

        StartCoroutine(WaitForEnd(target, onFinish));
    }

    IEnumerator WaitForEnd(PlayableDirector director, Action onFinish)
    {
        float duration = Mathf.Max((float)director.duration, 0.1f);
        Debug.Log($"[TimelineManager] 等待 {duration} 秒");
        while (director != null && director.time < director.duration - 0.0001 &&
               (director.state == PlayState.Playing || director.time > 0))
            yield return null;
        if (director == null)
            yield break;
        Debug.Log("[TimelineManager] 播放结束");
        director.Stop();
        onFinish?.Invoke();
    }
}