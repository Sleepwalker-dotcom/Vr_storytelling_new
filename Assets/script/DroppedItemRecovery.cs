using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class DroppedItemRecovery : MonoBehaviour
{
    private XRGrabInteractable grab;
    private bool slotted;

    public bool CanRecover => isActiveAndEnabled && !slotted && grab != null &&
        grab.isActiveAndEnabled && !grab.isSelected;

    private void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
    }

    public void MarkSlotted()
    {
        slotted = true;
    }
}
