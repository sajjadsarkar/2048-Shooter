using UnityEngine;
using UnityEngine.EventSystems;

public class ColumnTouchHandler : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    private int columnIndex;
    private ShootingManager2048 shootingManager;

    public void Initialize(int column)
    {
        columnIndex = column;
        shootingManager = FindObjectOfType<ShootingManager2048>();

        if (shootingManager == null)
        {
            Debug.LogWarning("ShootingManager2048 not found!");
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (shootingManager != null)
        {
            // When this area is clicked, tell the shooting manager to shoot at this column
            shootingManager.ShootAtColumn(columnIndex);
        }
    }

    // Implement the missing OnPointerDown method
    public void OnPointerDown(PointerEventData eventData)
    {
        if (shootingManager != null)
        {
            shootingManager.OnPointerDown(eventData);
        }
    }

    // Implement the missing OnPointerUp method
    public void OnPointerUp(PointerEventData eventData)
    {
        if (shootingManager != null)
        {
            shootingManager.OnPointerUp(eventData);
        }
    }

    // Make sure drag is passed to the ShootingManager
    public void OnDrag(PointerEventData eventData)
    {
        if (shootingManager != null)
        {
            shootingManager.OnDrag(eventData);
        }
    }
}
