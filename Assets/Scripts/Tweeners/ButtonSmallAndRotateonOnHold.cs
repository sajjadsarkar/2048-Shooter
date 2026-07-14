using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

public class ButtonSmallAndRotateonOnHold : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    private RectTransform targetRectTransform;
    private Vector3 originalScale;
    private Quaternion originalRotation;
    private Image targetImage;

    public float scaleDownFactor = 0.85f;
    public float rotationAngle = 20f;
    public float duration = 0.2f;

    void Start()
    {
        Button button = GetComponent<Button>();
        targetImage = button.targetGraphic as Image;
        targetRectTransform = targetImage.GetComponent<RectTransform>();

        if (targetRectTransform != null)
        {
            originalScale = targetRectTransform.localScale;
            originalRotation = targetRectTransform.localRotation;
        }
        else
        {
            Debug.LogError("Button targetGraphic is not an Image with a RectTransform.");
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (targetRectTransform != null)
        {
            targetRectTransform.DOScale(originalScale * scaleDownFactor, duration);
            targetRectTransform.DORotate(new Vector3(0, 0, rotationAngle), duration);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (targetRectTransform != null)
        { 
            targetRectTransform.DOScale(originalScale, duration);
            targetRectTransform.DORotateQuaternion(originalRotation, duration);
        }
    }
}
