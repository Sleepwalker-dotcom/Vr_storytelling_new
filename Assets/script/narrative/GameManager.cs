using UnityEngine;
using UnityEngine.Video;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("主事件流")]
    [SerializeField] private NarrativeEvent[] events;

    [Header("片尾视频（可选）")]
    [SerializeField] private GameObject endingCanvas;
    [SerializeField] private VideoPlayer endingVideoPlayer;

    private int currentIndex = 0;
    private bool hasStarted = false;
    private bool hasEnded = false;
    private bool endingVideoFailed = false;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        Debug.Log("[GameManager] 等待片头视频结束后开始");
    }

    public void BeginGame()
    {
        if (hasStarted)
        {
            Debug.LogWarning("[GameManager] BeginGame 已调用过，忽略重复调用。");
            return;
        }

        hasStarted = true;
        currentIndex = 0;

        Debug.Log("[GameManager] BeginGame，开始播放第一个 Event");
        PlayCurrentEvent();
    }

    private void PlayCurrentEvent()
    {
        if (currentIndex >= events.Length)
        {
            Debug.Log("[GameManager] 所有 Event 播放完毕");
            PlayEndingVideo();
            return;
        }

        if (events[currentIndex] == null)
        {
            Debug.LogError($"[GameManager] events[{currentIndex}] 是空的，跳过。");
            currentIndex++;
            PlayCurrentEvent();
            return;
        }

        Debug.Log($"[GameManager] 播放第 {currentIndex} 个 Event：{events[currentIndex].name}");
        events[currentIndex].Begin(OnEventComplete);
    }

    private void OnEventComplete()
    {
        Debug.Log($"[GameManager] Event {currentIndex} 完成，切换下一个");
        currentIndex++;
        PlayCurrentEvent();
    }

    private void PlayEndingVideo()
    {
        if (hasEnded) return;
        hasEnded = true;
        Debug.Log("[GameManager] 开始播放片尾视频");
        StartCoroutine(PlayEndingVideoDelayed(0f));
    }

    private IEnumerator PlayEndingVideoDelayed(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (endingCanvas != null)
            endingCanvas.SetActive(true);

        if (endingVideoPlayer == null)
        {
            Debug.LogWarning("[GameManager] 未指定 endingVideoPlayer。");
            QuitApplication();
            yield break;
        }

        endingVideoFailed = false;
        endingVideoPlayer.isLooping = false;
        endingVideoPlayer.loopPointReached -= OnEndingVideoFinished;
        endingVideoPlayer.loopPointReached += OnEndingVideoFinished;
        endingVideoPlayer.errorReceived -= OnEndingVideoError;
        endingVideoPlayer.errorReceived += OnEndingVideoError;
        endingVideoPlayer.Prepare();

        yield return new WaitUntil(() => endingVideoPlayer.isPrepared || endingVideoFailed);

        if (!endingVideoFailed)
            endingVideoPlayer.Play();
    }

    private void OnEndingVideoError(VideoPlayer source, string message)
    {
        endingVideoFailed = true;
        QuitApplication();
        Debug.LogError($"[GameManager] 片尾视频准备失败：{message}");
    }

    private void OnEndingVideoFinished(VideoPlayer source)
    {
        QuitApplication();
    }

    private void QuitApplication()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void OnDestroy()
    {
        if (endingVideoPlayer != null)
        {
            endingVideoPlayer.errorReceived -= OnEndingVideoError;
            endingVideoPlayer.loopPointReached -= OnEndingVideoFinished;
        }
    }
}