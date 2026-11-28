using UnityEngine;

public class TutorialShaderOutline : MonoBehaviour
{
    public bool visible;
    public bool preserveBottleColor;
    public float outlineWidth = 0.004f;
    private Material[] materials;
    private Transform[] materialTransforms;

    private void Start()
    {
        var renderers = GetComponentsInChildren<MeshRenderer>(true);
        var list = new System.Collections.Generic.List<Material>();
        var transforms = new System.Collections.Generic.List<Transform>();
        Material template = Resources.Load<Material>(
            preserveBottleColor ? "TutorialBottleToon" : "TutorialSlotToon");
        foreach (var renderer in renderers)
        {
            var source = renderer.sharedMaterials;
            var replacements = new Material[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                var material = new Material(template);
                if (preserveBottleColor && source[i] != null)
                {
                    Texture texture = source[i].HasProperty("_MainTex")
                        ? source[i].GetTexture("_MainTex") : source[i].mainTexture;
                    material.SetTexture("_MainTex", texture);
                    if (preserveBottleColor)
                    {
                        Color color = source[i].HasProperty("_Color")
                            ? source[i].GetColor("_Color") : Color.white;
                        color.a = 1f;
                        material.SetColor("_Color", color);
                    }
                }
                material.EnableKeyword("_USEOUTLINE_ON");
                material.SetFloat("_UseOutline", 1f);
                material.SetFloat("_AdaptiveThicnkess", 0f);
                material.SetFloat("_OutlineType", 0f);
                material.DisableKeyword("_OUTLINETYPE_POSITION");
                material.DisableKeyword("_OUTLINETYPE_UVBAKED");
                material.EnableKeyword("_OUTLINETYPE_NORMAL");
                material.SetColor("_OutlineColor", new Color(1f, 0.95f, 0.6f, 1f));
                replacements[i] = material;
                list.Add(material);
                transforms.Add(renderer.transform);
            }
            renderer.sharedMaterials = replacements;
        }
        materials = list.ToArray();
        materialTransforms = transforms.ToArray();
    }

    private void LateUpdate()
    {
        if (materials == null)
            return;
        float pulse = (Mathf.Sin(Time.unscaledTime * Mathf.PI * 3f) + 1f) * 0.5f;
        float worldWidth = visible ? outlineWidth * Mathf.Lerp(0.75f, 1.25f, pulse) : 0f;
        for (int i = 0; i < materials.Length; i++)
        {
            Vector3 scale = materialTransforms[i].lossyScale;
            float largestScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
            materials[i].SetFloat("_Thicnkess", worldWidth / Mathf.Max(largestScale, 0.0001f));
        }
    }
    private void OnDestroy()
    {
        if (materials == null)
            return;
        foreach (var material in materials)
            Destroy(material);
    }
}
