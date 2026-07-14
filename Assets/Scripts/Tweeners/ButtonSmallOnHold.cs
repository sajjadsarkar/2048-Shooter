using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; // For IPointerDownHandler and IPointerUpHandler
using DG.Tweening; // DOTween namespace

public class ButtonSmallOnHold : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public float shrinkDuration = 0.1f; // Duration of the shrink effect
    public float shrinkScale = 0.9f; // Scale to shrink to

    private Vector3 originalScale; // To store the original scale of the button
    private Button button;
    private RectTransform rectTransform;
    private Tween currentTween; // Reference to the current tween

    private void Awake()
    {
        button = GetComponent<Button>();
        rectTransform = GetComponent<RectTransform>();
        originalScale = transform.localScale;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // Kill any existing tween before starting a new one
        if (currentTween != null && currentTween.IsActive())
        {
            currentTween.Kill();
        }

        // Set pivot to center for uniform scaling animation
        SetPivotWithoutMoving(rectTransform, new Vector2(0.5f, 0.5f));

        // Animate the button to shrink
        currentTween = transform.DOScale(originalScale * shrinkScale, shrinkDuration).SetEase(Ease.OutQuad);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // Kill any existing tween before starting a new one
        if (currentTween != null && currentTween.IsActive())
        {
            currentTween.Kill();
        }

        // Ensure pivot is centered for the restore animation as well
        SetPivotWithoutMoving(rectTransform, new Vector2(0.5f, 0.5f));

        // Animate back to original scale
        currentTween = transform.DOScale(originalScale, shrinkDuration).SetEase(Ease.OutQuad);
    }

    /// <summary>
    /// Changes the pivot of a RectTransform without moving its visual position
    /// </summary>
    private void SetPivotWithoutMoving(RectTransform rt, Vector2 newPivot)
    {
        if (rt == null) return;

        Vector2 currentPivot = rt.pivot;
        if (currentPivot == newPivot) return;

        Vector2 size = rt.rect.size;
        Vector2 deltaPivot = newPivot - currentPivot;
        Vector3 deltaPosition = new Vector3(deltaPivot.x * size.x * rt.localScale.x, deltaPivot.y * size.y * rt.localScale.y, 0);

        rt.pivot = newPivot;
        rt.localPosition += deltaPosition;
    }

    private void OnDisable()
    {
        // Check if the tween is active before killing it
        if (currentTween != null && currentTween.IsActive())
        {
            currentTween.Kill();
        }

        // Reset scale to original when disabled
        transform.localScale = originalScale;
    }
}
