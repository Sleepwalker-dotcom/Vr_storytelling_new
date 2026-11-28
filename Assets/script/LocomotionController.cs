using UnityEngine;
using UnityEngine.XR;
using Unity.XR.CoreUtils;
using System.Collections;

[DefaultExecutionOrder(10000)]
public class LocomotionController : MonoBehaviour
{
    public Behaviour moveProvider;
    public Behaviour turnProvider;
    public Behaviour teleportProvider;

    private float movementHeight;
    private Vector3 spawnPosition;
    private Vector3 spawnForward;

    private void Awake()
    {
        movementHeight = transform.position.y;
        spawnPosition = transform.position;
        spawnForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        gameObject.AddComponent<DroppedItemRecoveryRay>();
        var characterController = GetComponent<CharacterController>();
        if (characterController != null)
        {
            characterController.stepOffset = 0f;
            characterController.slopeLimit = 0f;
        }
    }

    private IEnumerator Start()
    {
        var origin = GetComponent<XROrigin>();
        if (origin == null || origin.Camera == null)
            yield break;

        bool tracked = false;
        while (!tracked)
        {
            var head = InputDevices.GetDeviceAtXRNode(XRNode.Head);
            tracked = head.TryGetFeatureValue(CommonUsages.isTracked, out bool isTracked) && isTracked;
            yield return null;
        }

        yield return new WaitForEndOfFrame();
        origin.MatchOriginUpCameraForward(Vector3.up, spawnForward);
        Vector3 cameraPosition = origin.Camera.transform.position;
        cameraPosition.x = spawnPosition.x;
        cameraPosition.z = spawnPosition.z;
        origin.MoveCameraToWorldLocation(cameraPosition);
        Debug.Log("[Locomotion] Aligned tracked head to the starting position and heading.");
    }
    private void LateUpdate()
    {
        Vector3 position = transform.position;
        position.y = movementHeight;
        transform.position = position;
    }

    public void DisableLocomotion()
    {
        if (moveProvider) moveProvider.enabled = false;
        if (turnProvider) turnProvider.enabled = false;
        if (teleportProvider) teleportProvider.enabled = false;

        Debug.Log("[Locomotion] 已关闭");
    }

    public void EnableLocomotion()
    {
        if (moveProvider) moveProvider.enabled = true;
        if (turnProvider) turnProvider.enabled = true;
        if (teleportProvider) teleportProvider.enabled = true;

        Debug.Log("[Locomotion] 已开启");
    }
}