using UnityEngine;

[DefaultExecutionOrder(10000)]
public class LocomotionController : MonoBehaviour
{
    public Behaviour moveProvider;
    public Behaviour turnProvider;
    public Behaviour teleportProvider;

    private float movementHeight;

    private void Awake()
    {
        movementHeight = transform.position.y;
        var characterController = GetComponent<CharacterController>();
        if (characterController != null)
        {
            characterController.stepOffset = 0f;
            characterController.slopeLimit = 0f;
        }
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