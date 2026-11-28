using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class IntroGrabTutorial : MonoBehaviour
{
    public GameObject group;
    [SerializeField] private int layoutVersion;
    [SerializeField] private GameObject bottle;
    [SerializeField] private GameObject slot;
    [SerializeField] private GameObject slotVisual;
    [SerializeField] private TutorialBottleGrab grab;
    [SerializeField] private Rigidbody body;
    [SerializeField] private TextMeshPro grabText;
    [SerializeField] private TextMeshPro slotText;
    public TextMeshPro successText;
    [SerializeField] private TutorialShaderOutline slotOutline;
    [SerializeField] private TutorialShaderOutline bottleOutline;
    [SerializeField] private Material slotMaterial;
    [SerializeField] private Material outlineMaterial;


    public void Build(Camera camera)
    {
        Transform template = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(item => item.name == "Bottle new");
        if (template == null)
        {
            Debug.LogError("[Tutorial] Bottle new was not found.");
            return;
        }

        group = new GameObject("Tutorial Group");
        Vector3 forward = Vector3.ProjectOnPlane(camera.transform.forward, Vector3.up).normalized;
        Vector3 right = Vector3.Cross(Vector3.up, forward);
        Quaternion rotation = Quaternion.LookRotation(forward, Vector3.up);
        Vector3 center = camera.transform.position + forward * 0.8f - Vector3.up * 0.35f;

        bottle = new GameObject("TutorialBottle");
        bottle.transform.SetParent(group.transform);

        var visual = new GameObject("Bottle Visual");
        visual.transform.SetParent(bottle.transform, false);
        var renderers = template.GetComponentsInChildren<MeshRenderer>(true);

        Bounds bounds = new Bounds();
        bool first = true;
        for (int i = 0; i < renderers.Length; i++)
        {
            var filter = renderers[i].GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null)
                continue;
            // Mesh instantiation preserves imported vertex data without requiring Read/Write.
            var part = new GameObject("Bottle Mesh");
            part.transform.SetParent(visual.transform, false);
            part.transform.position = renderers[i].transform.position - template.position;
            part.transform.rotation = renderers[i].transform.rotation;
            part.transform.localScale = renderers[i].transform.lossyScale;
            part.AddComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
            part.AddComponent<MeshRenderer>().sharedMaterials = renderers[i].sharedMaterials;
            var renderer = part.GetComponent<MeshRenderer>();
            if (first) { bounds = renderer.bounds; first = false; }
            else bounds.Encapsulate(renderer.bounds);
        }
        if (first)
        {
            Debug.LogError("[Tutorial] Bottle new has no mesh renderers.");
            group.SetActive(false);
            return;
        }

        // Calculate geometry in an identity frame before positioning the tutorial.
        bottle.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        Bounds localBounds = visual.GetComponentInChildren<Renderer>().bounds;
        foreach (var renderer in visual.GetComponentsInChildren<Renderer>())
            localBounds.Encapsulate(renderer.bounds);
        float scale = 0.28f / Mathf.Max(localBounds.size.y, 0.001f);
        visual.transform.localScale *= scale;
        visual.transform.localPosition = -localBounds.center * scale;
        Vector3 size = localBounds.size * scale;

        bottle.transform.SetPositionAndRotation(center + right * 0.2f, rotation);
        var collider = bottle.AddComponent<BoxCollider>();
        collider.size = size;
        body = bottle.AddComponent<Rigidbody>();
        body.useGravity = false;
        body.isKinematic = true;
        grab = bottle.AddComponent<TutorialBottleGrab>();
        grab.throwOnDetach = false;
        bottle.AddComponent<DroppedItemRecovery>();
        bottleOutline = visual.AddComponent<TutorialShaderOutline>();
        bottleOutline.preserveBottleColor = true;
        bottleOutline.visible = true;
        slot = new GameObject("Tutorial Bottle Slot");
        slot.transform.SetParent(group.transform);
        slot.transform.SetPositionAndRotation(center - right * 0.2f, rotation);
        slotVisual = Instantiate(visual, slot.transform);
        slotMaterial = Resources.Load<Material>("TutorialSlotToon");
        foreach (var renderer in slotVisual.GetComponentsInChildren<Renderer>())
            renderer.sharedMaterials = Enumerable.Repeat(slotMaterial, renderer.sharedMaterials.Length).ToArray();

        grabText = CreateText("Press the button and grab it.",
            bottle.transform.position + Vector3.up * 0.28f, rotation);
        slotText = CreateText("Place the bottle in the highlighted slot.",
            slot.transform.position + Vector3.up * 0.28f, rotation);
        slotText.gameObject.SetActive(false);
        EnsureSuccessText();
        slotOutline = slotVisual.GetComponent<TutorialShaderOutline>();
        slotOutline.preserveBottleColor = false;
        slotOutline.visible = false;
        var lamp = new GameObject("Tutorial Light").AddComponent<Light>();
        lamp.transform.SetParent(group.transform);
        lamp.type = LightType.Spot;
        lamp.range = 3f;
        lamp.spotAngle = 80f;
        lamp.intensity = 1f;
        lamp.transform.position = center + Vector3.up * 0.8f;
        lamp.transform.rotation = Quaternion.LookRotation(Vector3.down, forward);
        layoutVersion = 1;

    }

    public IEnumerator Run(Camera camera)
    {
        if (group == null)
            Build(camera);
        if (group == null || grab == null)
            yield break;
        if (bottleOutline == null)
        {
            var oldHint = bottle.GetComponent<PickupOutline>();
            if (oldHint != null)
            {
                oldHint.enabled = false;
                Destroy(oldHint);
            }
            foreach (var oldLine in group.GetComponentsInChildren<LineRenderer>(true))
                Destroy(oldLine.gameObject);
            bottleOutline = bottle.transform.Find("Bottle Visual").gameObject.AddComponent<TutorialShaderOutline>();
            bottleOutline.preserveBottleColor = true;
            bottleOutline.visible = true;
            slotOutline = slotVisual.AddComponent<TutorialShaderOutline>();
        }
        EnsureSuccessText();
        successText.gameObject.SetActive(false);
        group.SetActive(true);
        UpdateTutorialLayout();
        bool pickedUp = false;
        while (true)
        {
            if (grab.isSelected)
                pickedUp = true;
            grabText.gameObject.SetActive(!pickedUp);
            slotText.gameObject.SetActive(pickedUp);
            bottleOutline.visible = !pickedUp;
            slotOutline.visible = pickedUp;
            if (pickedUp && !grab.isSelected &&
                Vector3.Distance(bottle.transform.position, slot.transform.position) < 0.16f)
                break;
            yield return null;
        }

        bottle.GetComponent<DroppedItemRecovery>().MarkSlotted();
        grab.enabled = false;
        body.isKinematic = true;
        bottle.transform.SetPositionAndRotation(slot.transform.position, slot.transform.rotation);
        slotVisual.SetActive(false);
        slotOutline.visible = false;
        slotText.gameObject.SetActive(false);
        successText.gameObject.SetActive(true);
        yield return new WaitForSecondsRealtime(4f);
        group.SetActive(false);
    }

    public void UpdateTutorialLayout()
    {
        if (bottle == null || slot == null || layoutVersion >= 1)
            return;

        Vector3 position = bottle.transform.position;
        bottle.transform.position = slot.transform.position;
        slot.transform.position = position;
        if (grabText != null && slotText != null)
        {
            position = grabText.transform.position;
            grabText.transform.position = slotText.transform.position;
            slotText.transform.position = position;
        }
        if (bottleOutline != null) bottleOutline.outlineWidth = 0.004f;
        if (slotOutline != null) slotOutline.outlineWidth = 0.004f;
        layoutVersion = 1;
    }
    public void EnsureSuccessText()
    {
        if (successText != null || group == null || slot == null || bottle == null)
            return;
        Vector3 position = (slot.transform.position + bottle.transform.position) * 0.5f + Vector3.up * 0.45f;
        successText = CreateText("Perfect! Now you can start.", position, slot.transform.rotation);
        successText.gameObject.name = "Success Instruction";
    }
    private TextMeshPro CreateText(string text, Vector3 position, Quaternion rotation)
    {
        var label = new GameObject("Tutorial Instruction").AddComponent<TextMeshPro>();
        label.transform.SetParent(group.transform);
        label.transform.SetPositionAndRotation(position, rotation);
        label.text = text;
        label.fontSize = 2f;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.alignment = TextAlignmentOptions.Center;
        label.rectTransform.sizeDelta = new Vector2(0.65f, 0.16f);
        return label;
    }

    private void OnDestroy()
    {
        if (Application.isPlaying && group != null) Destroy(group);
    }
}

