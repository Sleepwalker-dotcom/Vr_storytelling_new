using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class GrabInteractable : XRGrabInteractable
{
    public string interactableID;
    public bool releaseFromContainerOnGrab;

    void Start()
    {
        Debug.Log($"注册物体 ID: {interactableID}");
        InteractableRegistry.Register(interactableID, gameObject);
    }

    protected override void OnSelectEntered(SelectEnterEventArgs args)
    {
        if (releaseFromContainerOnGrab)
            transform.SetParent(null, true);

        base.OnSelectEntered(args);
        Debug.Log($"物体被抓取，触发ID: {interactableID}");
        GameEvents.TriggerInteractionComplete(interactableID);
    }

    protected override void SetupRigidbodyDrop(Rigidbody body)
    {
        base.SetupRigidbodyDrop(body);
        if (releaseFromContainerOnGrab)
        {
            body.isKinematic = false;
            body.useGravity = true;
        }
    }
}