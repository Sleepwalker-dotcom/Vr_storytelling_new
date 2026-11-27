using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class BirthdayCakeBlowInteraction : MonoBehaviour
{
    private const string EndingTriggerID = "cake_blown";
    private const float EndingDelay = 1f;
    [Header("Grab")]
    public XRGrabInteractable grabInteractable;

    [Header("Head Target")]
    public Transform playerHead;   // Main Camera

    [Header("Fire Object")]
    public GameObject fireObject;  // 你的 fire 模型
    public Renderer fireRenderer;  // fire 的 Renderer
    public Light candleLight;      // 新建的 CandleLight

    [Header("Emission")]
    public string emissionProperty = "_EmissionColor";
    public Color fireLitColor = new Color(2f, 1.2f, 0.4f);
    public Color fireOffColor = Color.black;

    [Header("Distance Check")]
    public float triggerDistance = 0.35f;

    [Header("Audio")]
    public AudioSource blowAudioSource; // 吹气音效
    public AudioClip blowClip;

    private bool hasBeenPickedUp = false;
    private bool candleLit = false;
    private bool endingTriggered = false;

    private Material fireMat;
    private bool isHeld = false;

    private void Awake()
    {
        if (grabInteractable == null)
            grabInteractable = GetComponent<XRGrabInteractable>();

        if (fireRenderer != null)
            fireMat = fireRenderer.material;

        SetFireState(false);
    }

    private void OnEnable()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.AddListener(OnGrabbed);
            grabInteractable.selectExited.AddListener(OnReleased);
        }
    }

    private void OnDisable()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(OnGrabbed);
            grabInteractable.selectExited.RemoveListener(OnReleased);
        }
    }

    private void Update()
    {
        if (!hasBeenPickedUp || !candleLit || endingTriggered || !isHeld || playerHead == null)
            return;

        float distance = Vector3.Distance(transform.position, playerHead.position);

        if (distance <= triggerDistance)
        {
            TriggerEndingSequence();
        }
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        isHeld = true;

        if (!hasBeenPickedUp)
        {
            hasBeenPickedUp = true;
            SetFireState(true);
        }
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        isHeld = false;
    }

    private void SetFireState(bool on)
    {
        candleLit = on;

        if (fireObject != null)
            fireObject.SetActive(true); // 如果你想始终看得到火焰模型，就保持 true

        if (fireMat != null && fireMat.HasProperty(emissionProperty))
        {
            fireMat.SetColor(emissionProperty, on ? fireLitColor : fireOffColor);
        }

        if (candleLight != null)
        {
            candleLight.intensity = on ? 3f : 0f;
        }

        // 如果想熄灭时直接隐藏火焰模型，可以取消下面注释
        // if (fireObject != null)
        //     fireObject.SetActive(on);
    }

    private void TriggerEndingSequence()
    {
        endingTriggered = true;
        StartCoroutine(EndingSequenceCoroutine());
    }

    private System.Collections.IEnumerator EndingSequenceCoroutine()
    {
        // 先播吹气
        if (blowAudioSource != null && blowClip != null)
        {
            blowAudioSource.PlayOneShot(blowClip);
        }

        yield return new WaitForSeconds(0.3f);

        // 再熄灭火焰
        SetFireState(false);

        yield return new WaitForSeconds(EndingDelay);

        GameEvents.TriggerInteractionComplete(EndingTriggerID);
    }
}
