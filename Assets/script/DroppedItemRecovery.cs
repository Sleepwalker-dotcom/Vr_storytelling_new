using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class DroppedItemRecovery : MonoBehaviour
{
    public bool CanRecover => dropped && !slotted && !grab.isSelected;
    private XRGrabInteractable grab;
    private bool released;
    private bool dropped;
    private bool slotted;
    private float groundContactTime;
    private float floorHeight;

    private void Start()
    {
        var rig = FindFirstObjectByType<Unity.XR.CoreUtils.XROrigin>();
        floorHeight = rig != null ? rig.transform.position.y : transform.position.y;
    }

    private void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
        grab.selectEntered.AddListener(OnGrabbed);
        grab.selectExited.AddListener(OnReleased);
    }

    private void OnDestroy()
    {
        grab.selectEntered.RemoveListener(OnGrabbed);
        grab.selectExited.RemoveListener(OnReleased);
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        released = false;
        dropped = false;
        groundContactTime = 0f;
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        released = !slotted;
        groundContactTime = 0f;
    }

    private void FixedUpdate()
    {
        if (!released || slotted || grab.isSelected || dropped)
            return;

        Bounds bounds = new Bounds(transform.position, Vector3.zero);
        foreach (var collider in GetComponentsInChildren<Collider>())
        {
            if (collider.attachedRigidbody == grab.GetComponent<Rigidbody>() && !collider.isTrigger)
                bounds.Encapsulate(collider.bounds);
        }

        bool ground = false;
        foreach (var hit in Physics.RaycastAll(bounds.center, Vector3.down,
                     bounds.extents.y + 0.15f, ~0, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.attachedRigidbody == grab.GetComponent<Rigidbody>())
                continue;

            if (hit.normal.y > 0.7f && hit.point.y <= floorHeight + 0.15f)
                ground = true;

            for (Transform part = hit.collider.transform; part != null; part = part.parent)
            {
                if (part.name.IndexOf("floor", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    part.name.Equals("Ground", System.StringComparison.OrdinalIgnoreCase))
                    ground = true;
            }
        }

        groundContactTime = ground ? groundContactTime + Time.fixedDeltaTime : 0f;
        if (groundContactTime >= 0.5f)
            dropped = true;
    }
    public void MarkSlotted()
    {
        slotted = true;
        released = false;
        dropped = false;
    }
}
