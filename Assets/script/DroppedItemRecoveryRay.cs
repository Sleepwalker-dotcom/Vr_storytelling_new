using UnityEngine;
using UnityEngine.XR;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class DroppedItemRecoveryRay : MonoBehaviour
{
    public float downwardAngle = 60f;
    public float maxGrabDistance = 10f;

    private XRRayInteractor ray;
    private LineRenderer line;
    private XRGrabInteractable held;
    private bool previousGrip;
    private InputAction recoveryButton;
    private InputAction gripAction;

    private void OnEnable()
    {
        recoveryButton = new InputAction("Ray grab", InputActionType.Button,
            "<XRController>{LeftHand}/primaryButton");
        recoveryButton.Enable();
        gripAction = new InputAction("Ray grab grip", InputActionType.Value,
            "<XRController>{RightHand}/grip");
        gripAction.Enable();
    }

    private void OnDisable()
    {
        if (ray != null && ray.hasSelection)
            ray.EndManualInteraction();
        held = null;
        if (ray != null)
            ray.gameObject.SetActive(false);
        gripAction?.Dispose();
        gripAction = null;
        recoveryButton?.Dispose();
        recoveryButton = null;
    }

    private void Start()
    {
        foreach (var grab in FindObjectsByType<XRGrabInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            string item = grab.gameObject.name;
            if (item == "parcel" || item == "key" || item == "shoe" ||
                item == "aBottleOfBeer" || item == "phone" || item == "pickup_phone" ||
                System.Text.RegularExpressions.Regex.IsMatch(item, "^calendar_0[1-7]$"))
            {
                if (grab.GetComponent<DroppedItemRecovery>() == null)
                    grab.gameObject.AddComponent<DroppedItemRecovery>();
            }
        }

        var rayObject = new GameObject("Dropped Item Recovery Ray");
        rayObject.transform.SetParent(transform, false);
        ray = rayObject.AddComponent<XRRayInteractor>();
        ray.interactionManager = FindFirstObjectByType<XRInteractionManager>();
        ray.enableUIInteraction = false;
        ray.useForceGrab = true;
        line = rayObject.AddComponent<LineRenderer>();
        line.positionCount = 2;
        line.startWidth = 0.008f;
        line.endWidth = 0.003f;
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.enabled = false;
        rayObject.SetActive(false);
    }

    private void Update()
    {
        var left = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        var right = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        right.TryGetFeatureValue(UnityEngine.XR.CommonUsages.gripButton, out bool grip);
        grip |= gripAction != null && gripAction.ReadValue<float>() >= 0.7f;

        if (held != null)
        {
            var rig = GetComponent<Unity.XR.CoreUtils.XROrigin>();
            if (right.TryGetFeatureValue(UnityEngine.XR.CommonUsages.devicePosition, out Vector3 handPosition) &&
                right.TryGetFeatureValue(UnityEngine.XR.CommonUsages.deviceRotation, out Quaternion handRotation))
            {
                Transform space = rig.Camera.transform.parent;
                ray.transform.SetPositionAndRotation(space.TransformPoint(handPosition), space.rotation * handRotation * Quaternion.Euler(downwardAngle, 0f, 0f));
            }

            if (!grip)
            {
                ray.EndManualInteraction();
                held = null;
                ray.gameObject.SetActive(false);
            }
            previousGrip = grip;
            return;
        }

        bool requested = (recoveryButton != null && recoveryButton.IsPressed()) || (left.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primaryButton, out bool x) && x);
        if (!requested ||
            !right.TryGetFeatureValue(UnityEngine.XR.CommonUsages.devicePosition, out Vector3 position) ||
            !right.TryGetFeatureValue(UnityEngine.XR.CommonUsages.deviceRotation, out Quaternion rotation))
        {
            ray.gameObject.SetActive(false);
            previousGrip = grip;
            return;
        }

        var origin = GetComponent<Unity.XR.CoreUtils.XROrigin>();
        Transform trackingSpace = origin.Camera.transform.parent;
        ray.gameObject.SetActive(true);
        ray.transform.SetPositionAndRotation(trackingSpace.TransformPoint(position), trackingSpace.rotation * rotation * Quaternion.Euler(downwardAngle, 0f, 0f));
        Vector3 start = ray.transform.position;
        Vector3 end = start + ray.transform.forward * maxGrabDistance;
        XRGrabInteractable target = null;
        var hits = Physics.RaycastAll(start, ray.transform.forward, maxGrabDistance, ~0, QueryTriggerInteraction.Collide);
        System.Array.Sort(hits, (first, second) => first.distance.CompareTo(second.distance));
        foreach (var hit in hits)
        {
            var item = hit.collider.GetComponentInParent<DroppedItemRecovery>();
            if ((hit.collider.transform.IsChildOf(transform) || hit.collider.isTrigger) && item == null)
                continue;
            end = hit.point;
            if (item != null && item.CanRecover)
            {
                var candidate = item.GetComponent<XRGrabInteractable>();
                if (ray.interactionManager != null &&
                    ray.interactionManager.IsSelectPossible(ray, candidate))
                    target = candidate;
            }
            break;
        }
        line.enabled = true;
        line.SetPosition(0, start);
        line.SetPosition(1, end);
        Color rayColor = target != null
            ? new Color(0.65f, 1f, 0.65f, 1f)
            : new Color(1f, 0.95f, 0.6f, 1f);
        line.startColor = line.endColor = rayColor;
        if (target != null && grip && !previousGrip && ray.CanSelect((IXRSelectInteractable)target))
        {
            ray.StartManualInteraction((IXRSelectInteractable)target);
            Debug.Log("[RayGrab] Selection requested for " + target.name);
            if (ray.IsSelecting((IXRSelectInteractable)target))
            {
                held = target;
                line.enabled = false;
            }
        }
        previousGrip = grip;
    }

    private void OnDestroy()
    {
        if (line != null)
            Destroy(line.material);
    }
}
