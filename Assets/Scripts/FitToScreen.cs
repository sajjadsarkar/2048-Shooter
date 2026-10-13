using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class FitToScreen : MonoBehaviour
{
    public Vector2 designSize = new Vector2(1080f, 2337f);

    private void LateUpdate()
    {
        var rt = (RectTransform)transform;
        var area = ((RectTransform)rt.parent).rect;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = designSize;
        rt.anchoredPosition = Vector2.zero;
        float s = Mathf.Min(area.width / designSize.x, area.height / designSize.y);
        rt.localScale = new Vector3(s, s, 1f);
    }
}
