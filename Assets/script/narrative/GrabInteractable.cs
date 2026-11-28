using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class GrabInteractable : XRGrabInteractable
{
    public string interactableID;
    public bool releaseFromContainerOnGrab;
    public UnityEngine.Playables.PlayableDirector outlineUnlockDirector;
    public float outlineUnlockTime = -1f;
    private bool outlineUnlocked;
    private MaterialPropertyBlock outlineProperties;

    private void LateUpdate()
    {
        if (outlineUnlocked || outlineUnlockDirector == null ||
            outlineUnlockDirector.state != UnityEngine.Playables.PlayState.Playing)
            return;

        if (outlineUnlockTime >= 0f)
        {
            outlineUnlocked = outlineUnlockDirector.time >= outlineUnlockTime;
            return;
        }

        var targetRenderer = GetComponent<Renderer>();
        if (targetRenderer != null)
        {
            if (outlineProperties == null)
                outlineProperties = new MaterialPropertyBlock();
            targetRenderer.GetPropertyBlock(outlineProperties);
            var material = targetRenderer.material;
            outlineUnlocked = outlineProperties.GetFloat("_Thicnkess") > 0.00001f ||
                (material.HasProperty("_Thicnkess") && material.GetFloat("_Thicnkess") > 0.00001f);
        }
    }

    public override bool IsSelectableBy(UnityEngine.XR.Interaction.Toolkit.Interactors.IXRSelectInteractor interactor)
    {
        return (outlineUnlockDirector == null || outlineUnlocked) && base.IsSelectableBy(interactor);
    }

    void Start()
    {
        Debug.Log($"注册物体 ID: {interactableID}");
        InteractableRegistry.Register(interactableID, gameObject);
    }

    protected override void OnSelectEntered(SelectEnterEventArgs args)
    {
        if (releaseFromContainerOnGrab)
        {
            var parcel = GetComponentInParent<BoxFromNPC>();
            if (parcel != null)
                parcel.ReleaseInternalCollisions(transform);
            transform.SetParent(null, true);
        }

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