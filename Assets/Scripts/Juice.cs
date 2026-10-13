using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public static class Juice
{
    public static void Burst(Transform parent, Vector2 anchoredPos, Color color, int count, float distance)
    {
        for (int i = 0; i < count; i++)
        {
            var go = new GameObject("Particle", typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = Vector2.one * Random.Range(10f, 22f);
            rt.localRotation = Quaternion.Euler(0, 0, Random.Range(0f, 90f));

            var img = go.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = false;

            float angle = (i + Random.value) / count * Mathf.PI * 2f;
            Vector2 target = anchoredPos + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance * Random.Range(0.5f, 1.3f);
            float time = Random.Range(0.45f, 0.75f);

            rt.DOAnchorPos(target, time).SetEase(Ease.OutCubic);
            rt.DOScale(0f, time).SetEase(Ease.InQuad);
            rt.DORotate(new Vector3(0, 0, Random.Range(-180f, 180f)), time, RotateMode.FastBeyond360);
            img.DOFade(0f, time).OnComplete(() => Object.Destroy(go));
        }
    }

    public static void Flash(Transform any, Color color)
    {
        var root = any.GetComponentInParent<Canvas>().rootCanvas.transform;
        var go = new GameObject("Flash", typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(root, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var img = go.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        img.DOFade(0f, 0.3f).OnComplete(() => Object.Destroy(go));
    }

    public static void Shake(Transform target, float strength)
    {
        var rt = target as RectTransform;
        if (rt == null) return;
        rt.DOComplete();
        rt.DOShakeAnchorPos(0.3f, strength, 25, 90f, false, true);
    }
}
