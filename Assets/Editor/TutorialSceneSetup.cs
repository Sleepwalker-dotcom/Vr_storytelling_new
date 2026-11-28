using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class TutorialSceneSetup
{
    static TutorialSceneSetup()
    {
        EditorApplication.delayCall += EnsureTutorial;
        EditorSceneManager.sceneOpened += OnSceneOpened;
    }

    private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
    {
        EnsureTutorial();
    }

    [MenuItem("Tools/Tutorial/Create Scene Tutorial")]
    private static void EnsureTutorial()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode ||
            SceneManager.GetActiveScene().path != "Assets/Scenes/The Frozen Hour.unity")
            return;

        var existing = Object.FindFirstObjectByType<IntroGrabTutorial>();
        if (existing != null)
        {
            existing.UpdateTutorialLayout();
            if (existing.successText == null)
            {
                existing.EnsureSuccessText();
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            }
            return;
        }

        var video = Object.FindFirstObjectByType<VideoToTimeline>();
        var camera = Object.FindFirstObjectByType<Unity.XR.CoreUtils.XROrigin>()?.Camera;
        if (video == null || camera == null)
            return;

        var controller = new GameObject("Tutorial Controller").AddComponent<IntroGrabTutorial>();
        controller.Build(camera);
        video.tutorial = controller;
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Selection.activeGameObject = controller.group;
    }
}
