using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class ButtonPopAnimation : MonoBehaviour
{
    public float animationDuration = 0.5f;  // Duration of the scale animation
    public float fadeDuration = 0.3f;       // Duration of the fade animation
    public float initialDelay = 0.1f;       // Initial delay before starting the animation

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Sequence sequence;
    [SerializeField] Ease animationEase = Ease.OutBounce;
    void Awake()
    {
        // Get the RectTransform and CanvasGroup components
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = gameObject.AddComponent<CanvasGroup>();

        // Set initial states
        rectTransform.localScale = Vector3.zero;
        canvasGroup.alpha = 0f;

    }

    void OnEnable()
    {
        // Reset initial states
        rectTransform.localScale = Vector3.zero;
        canvasGroup.alpha = 0f;

        // Start the animation with the specified delay
        AnimateButton();
    }

    void OnDisable()
    {
        // Kill the sequence when the object is disabled to clean up resources
        if (sequence != null)
        {
            sequence.Kill();
        }
    }

    void AnimateButton()
    {
        // Create a new sequence to combine animations
        sequence = DOTween.Sequence();

        sequence.AppendInterval(initialDelay)
                .Append(rectTransform.DOScale(Vector3.one, animationDuration).SetEase(animationEase))
                .Join(canvasGroup.DOFade(1f, fadeDuration))
                .OnComplete(() => sequence.Kill()); // Kill the sequence at the end of the animation
    }
}
