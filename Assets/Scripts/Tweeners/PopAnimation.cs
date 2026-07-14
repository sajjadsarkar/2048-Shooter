using UnityEngine;
using DG.Tweening;

public class PopAnimation : MonoBehaviour
{
    [Header("Enable Animation Settings")]
    public float enableDuration = 0.2f;
    public float enableScaleUpSize = 1.05f;
    public float enableStartScaleSize = 0.8f;
    public Ease enableEase = Ease.OutQuad;
    public float enableFadeDuration = 0.3f;

    [Header("Disable Animation Settings")]
    public float disableDuration = 0.2f;
    public float disableScaleUpSize = 1.1f;
    public float disableScaleDownSize = 0.8f;
    public Ease disableEase = Ease.InQuad;
    public float disableFadeDuration = 0.6f;

    private Vector3 originalScale;
    private CanvasGroup canvasGroup;

    private void Awake()
    {
        originalScale = transform.localScale;
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            // Add a CanvasGroup if one doesn't exist
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    private void OnEnable()
    {
        PopEnable();
    }

    public void PopEnable()
    {
        // // Play panel swipe sound
        // if (Ludo.LudoAudioManager.instance != null)
        // {
        //     Ludo.LudoAudioManager.instance.AudioPlay(Ludo.LudoAudioManager.Clips.panel_swipe);
        // }

        // Reset scale and alpha
        transform.localScale = originalScale * enableStartScaleSize;
        canvasGroup.alpha = 0f;

        // Create sequences for the scale animation and fade in
        Sequence scaleSequence = DOTween.Sequence();
        Sequence fadeSequence = DOTween.Sequence();

        // Fade in
        fadeSequence.Append(canvasGroup.DOFade(1f, enableFadeDuration).SetEase(enableEase));

        // Scale up and down
        scaleSequence.Append(transform.DOScale(originalScale * enableScaleUpSize, enableDuration / 2).SetEase(enableEase));
        scaleSequence.Append(transform.DOScale(originalScale, enableDuration / 2).SetEase(enableEase));

        // Start both sequences independently
        fadeSequence.Play();
        scaleSequence.Play();
    }

    public void PopDisable()
    {
        // // Play panel swipe sound
        // if (Ludo.LudoAudioManager.instance != null)
        // {
        //     Ludo.LudoAudioManager.instance.AudioPlay(Ludo.LudoAudioManager.Clips.panel_swipe);
        // }

        // Create sequences for the scale animation and fade out
        Sequence scaleSequence = DOTween.Sequence();
        Sequence fadeSequence = DOTween.Sequence();

        // Fade out
        fadeSequence.Append(canvasGroup.DOFade(0f, disableFadeDuration).SetEase(disableEase));

        // Scale up and down
        scaleSequence.Append(transform.DOScale(originalScale * disableScaleUpSize, disableDuration / 3).SetEase(disableEase));
        scaleSequence.Append(transform.DOScale(originalScale, disableDuration / 3).SetEase(disableEase));
        scaleSequence.Append(transform.DOScale(originalScale * disableScaleDownSize, disableDuration / 3).SetEase(disableEase))
                     .OnComplete(() => gameObject.SetActive(false));

        // Start both sequences independently
        fadeSequence.Play();
        scaleSequence.Play();
    }
}
