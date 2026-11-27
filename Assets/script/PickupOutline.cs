using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class PickupOutline : MonoBehaviour
{
    public Renderer targetRenderer;
    public float outlineWidth = 0.008f;
    public float outlinePulseSpeed = 1.5f;
    private XRGrabInteractable grab;
    private bool pickedUp;

    private void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
        if (targetRenderer == null)
            targetRenderer = GetComponent<Renderer>();
        grab.selectEntered.AddListener(OnGrabbed);
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        pickedUp = true;
        if (outline != null)
            outline.gameObject.SetActive(false);
    }
    private LineRenderer outline;
    private Material borderMaterial;

    private void Start()
    {
        if (targetRenderer == null)
            return;

        var filter = targetRenderer.GetComponent<MeshFilter>();
        if (filter == null || filter.sharedMesh == null)
            return;

        Bounds bounds = filter.sharedMesh.bounds;
        Vector3 size = bounds.size;
        int thinAxis = size.x <= size.y && size.x <= size.z ? 0 : (size.y <= size.z ? 1 : 2);
        int firstAxis = (thinAxis + 1) % 3;
        int secondAxis = (thinAxis + 2) % 3;
        var points = new Vector3[4];
        for (int i = 0; i < 4; i++)
        {
            Vector3 point = bounds.center;
            point[firstAxis] += bounds.extents[firstAxis] * (i < 2 ? -1f : 1f);
            point[secondAxis] += bounds.extents[secondAxis] * (i == 0 || i == 3 ? -1f : 1f);
            points[i] = point;
        }

        var border = new GameObject("Pickup Outline");
        border.transform.SetParent(targetRenderer.transform, false);
        outline = border.AddComponent<LineRenderer>();
        outline.useWorldSpace = false;
        outline.loop = true;
        outline.positionCount = 4;
        outline.SetPositions(points);
        outline.startWidth = outline.endWidth = outlineWidth;
        outline.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        outline.receiveShadows = false;
        borderMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        borderMaterial.SetColor("_BaseColor", new Color(1f, 0.95f, 0.6f, 1f));
        outline.sharedMaterial = borderMaterial;
        outline.gameObject.SetActive(!pickedUp);
    }

    private void Update()
    {
        if (outline == null || !outline.gameObject.activeSelf)
            return;

        float pulse = (Mathf.Sin(Time.unscaledTime * outlinePulseSpeed * Mathf.PI * 2f) + 1f) * 0.5f;
        outline.startWidth = outline.endWidth = outlineWidth * Mathf.Lerp(0.75f, 1.25f, pulse);

    }
    private void OnDestroy()
    {
        if (borderMaterial != null)
            Destroy(borderMaterial);
        grab.selectEntered.RemoveListener(OnGrabbed);
    }
}
