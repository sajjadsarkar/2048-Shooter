using UnityEngine;
using UnityEngine.EventSystems;

public class PreviewTileDragger : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    private ShootingManager2048 shootingManager;
    private RectTransform rectTransform;
    private Vector2 originalPosition;
    private bool isDragging = false;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        shootingManager = FindObjectOfType<ShootingManager2048>();

        if (shootingManager == null)
        {
            Debug.LogError("ShootingManager2048 not found!");
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isDragging = true;
        originalPosition = rectTransform.anchoredPosition;
        shootingManager.OnPreviewTileDragStart();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;

        // Convert screen point to local point in parent's rect
        Vector2 localPoint;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform.parent as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out localPoint))
        {
            rectTransform.anchoredPosition = localPoint;
            shootingManager.OnPreviewTileDragged(eventData);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!isDragging) return;

        isDragging = false;
        shootingManager.OnPreviewTileDragEnd(eventData);

        // Reset to original position if needed - ShootingManager will handle this
        rectTransform.anchoredPosition = originalPosition;
    }
}
