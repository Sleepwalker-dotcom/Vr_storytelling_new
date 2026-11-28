using UnityEngine;
using UnityEngine.Video;
using System.Collections;
using UnityEngine.XR;

public class VideoToTimeline : MonoBehaviour
{
    [Header("片头视频播放器")]
    public VideoPlayer videoPlayer;

    [Header("片头视频结束后隐藏的 Canvas（可选）")]
    public GameObject videoCanvas;

    [Header("片头期间锁定移动（可选）")]
    public LocomotionController locomotionController;

    public bool skipTutorialForTesting;
    public IntroGrabTutorial tutorial;
    private bool titleStarted;
    private bool videoFailed;
    private bool firstFrameReady;
    private bool hasTriggered = false;

    private void OnEnable()
    {
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached += OnVideoFinished;
            videoPlayer.errorReceived += OnVideoError;
            videoPlayer.frameReady += OnFrameReady;
        }
    }

    private void OnDisable()
    {
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnVideoFinished;
            videoPlayer.errorReceived -= OnVideoError;
            videoPlayer.frameReady -= OnFrameReady;
        }
    }

    private IEnumerator Start()
    {
        if (locomotionController != null)
            locomotionController.DisableLocomotion();
        if (videoPlayer != null)
        {
            videoPlayer.playOnAwake = false;
            videoPlayer.Stop();
            videoPlayer.waitForFirstFrame = true;
            videoPlayer.Prepare();
        }
        if (videoCanvas != null)
            videoCanvas.SetActive(false);

        if (tutorial == null)
            tutorial = FindFirstObjectByType<IntroGrabTutorial>();
        if (skipTutorialForTesting && tutorial != null && tutorial.group != null)
            tutorial.group.SetActive(false);

        if (!skipTutorialForTesting)
        {
            while (XRSettings.isDeviceActive &&
                   !(InputDevices.GetDeviceAtXRNode(XRNode.Head).TryGetFeatureValue(
                       CommonUsages.isTracked, out bool tracked) && tracked))
                yield return null;
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();
            Camera camera = locomotionController != null
                ? locomotionController.GetComponent<Unity.XR.CoreUtils.XROrigin>().Camera
                : Camera.main;
            if (camera != null)
            {
                if (tutorial == null)
                    tutorial = FindFirstObjectByType<IntroGrabTutorial>();
                if (tutorial == null)
                    tutorial = gameObject.AddComponent<IntroGrabTutorial>();
                yield return tutorial.Run(camera);
            }
        }

        titleStarted = true;
        if (videoPlayer == null)
        {
            OnVideoFinished(null);
            yield break;
        }

        float deadline = Time.realtimeSinceStartup + 15f;
        while (!videoPlayer.isPrepared && !videoFailed && Time.realtimeSinceStartup < deadline)
            yield return null;
        if (!videoPlayer.isPrepared || videoFailed)
        {
            Debug.LogWarning("[VideoToTimeline] Title video could not be prepared.");
            OnVideoFinished(videoPlayer);
            yield break;
        }

        firstFrameReady = false;
        videoPlayer.sendFrameReadyEvents = true;
        videoPlayer.Play();
        deadline = Time.realtimeSinceStartup + 15f;
        while (!firstFrameReady && !videoFailed && Time.realtimeSinceStartup < deadline)
            yield return null;
        videoPlayer.sendFrameReadyEvents = false;
        if (firstFrameReady && !hasTriggered)
        {
            if (videoCanvas != null)
                videoCanvas.SetActive(true);
        }
        else if (!hasTriggered)
        {
            videoPlayer.Stop();
            OnVideoFinished(videoPlayer);
        }
    }

    private void OnFrameReady(VideoPlayer source, long frame)
    {
        firstFrameReady = true;
    }

    private void OnVideoError(VideoPlayer source, string message)
    {
        videoFailed = true;
        Debug.LogWarning("[VideoToTimeline] " + message);
    }
    private void OnVideoFinished(VideoPlayer vp)
    {
        if (!titleStarted || hasTriggered) return;
        hasTriggered = true;

        // 隐藏片头 Canvas
        if (videoCanvas != null)
            videoCanvas.SetActive(false);

        // ✅ 片头结束：恢复移动
        if (locomotionController != null)
            locomotionController.EnableLocomotion();

        // 开始主流程
        if (GameManager.Instance != null)
            GameManager.Instance.BeginGame();
        else
            Debug.LogWarning("[VideoToTimeline] GameManager.Instance is null.");
    }
}