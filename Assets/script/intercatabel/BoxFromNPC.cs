using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class BoxFromNPC : XRGrabInteractable
{
    [Header("叙事设置")]
    public string interactableID;

    [Header("NPC设置")]
    public Transform npcHand;

    [Header("初始状态")]
    public bool canGrab = false;

    private bool taken = false;
    public bool HasBeenTaken => taken;
    private RigidbodyConstraints originalConstraints;

    protected override void Awake()
    {
        base.Awake();
        originalConstraints = GetComponent<Rigidbody>().constraints;
        if (npcHand != null)
            transform.SetParent(npcHand);
    }

    void Start()
    {
        InteractableRegistry.Register(interactableID, gameObject);
        Debug.Log($"[BoxFromNPC] 注册ID: {interactableID}");
    }

    // ← 关键：在这里控制是否允许抓取
    public override bool IsSelectableBy(IXRSelectInteractor interactor)
    {
        if (!canGrab) return false;
        return base.IsSelectableBy(interactor);
    }

    public void EnableGrab()
    {
        canGrab = true;
        Debug.Log("[BoxFromNPC] 可以抓取了");
    }

    public void DisableGrab()
    {
        canGrab = false;
        Debug.Log("[BoxFromNPC] 禁止抓取");
    }

    protected override void OnSelectEntered(SelectEnterEventArgs args)
    {
        GetComponent<Rigidbody>().constraints = originalConstraints;
        base.OnSelectEntered(args);
        if (taken)
        {
            GameEvents.TriggerInteractionComplete(interactableID);
            return;
        }
        taken = true;
        ReleaseTimelineControl();

        transform.SetParent(null);

        var rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = false;
        }

        Debug.Log($"[BoxFromNPC] 箱子被接过，触发ID: {interactableID}");
        GameEvents.TriggerInteractionComplete(interactableID);
    }

    protected override void OnSelectExited(SelectExitEventArgs args)
    {
        base.OnSelectExited(args);

        var rb = GetComponent<Rigidbody>();
        if (rb != null)
            rb.useGravity = true;

        Debug.Log("[BoxFromNPC] 箱子被放下");
    }

    void Update()
    {
        if (taken && transform.parent != null)
        {
            transform.SetParent(null);

            var rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
            }
        }
    }

    protected override void SetupRigidbodyDrop(Rigidbody body)
    {
        base.SetupRigidbodyDrop(body);
        body.isKinematic = false;
        body.useGravity = true;
        body.angularVelocity = Vector3.zero;
        body.constraints = originalConstraints;
    }

    protected override void Detach()
    {
        base.Detach();
        var body = GetComponent<Rigidbody>();
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
    }

    private void ReleaseTimelineControl()
    {
        var animator = GetComponent<Animator>();
        if (animator == null)
            return;

        foreach (var director in FindObjectsByType<PlayableDirector>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (director.playableAsset == null)
                continue;

            foreach (var output in director.playableAsset.outputs)
            {
                if (director.GetGenericBinding(output.sourceObject) == animator)
                    director.ClearGenericBinding(output.sourceObject);
            }
        }

        animator.enabled = false;
    }
}