using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class TutorialBottleGrab : XRGrabInteractable
{
    protected override void SetupRigidbodyDrop(Rigidbody body)
    {
        base.SetupRigidbodyDrop(body);
        body.isKinematic = false;
        body.useGravity = false;
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
    }

    protected override void Detach()
    {
        base.Detach();
        var body = GetComponent<Rigidbody>();
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
    }
}
