using UnityEngine;
using TMPro;
using DG.Tweening;
using System.Collections.Generic;

public static class AnimatedNumberText
{
    private static readonly Dictionary<TextMeshProUGUI, float> displayValues = new Dictionary<TextMeshProUGUI, float>();
    private static readonly Dictionary<TextMeshProUGUI, Tween> activeTweens = new Dictionary<TextMeshProUGUI, Tween>();

    /// <summary>
    /// Animates a number text from its current displayed value up/down to the target,
    /// formatting each frame with NumberFormatter so large numbers stay compact.
    /// </summary>
    public static void Animate(TextMeshProUGUI text, int target, float duration = 0.6f)
    {
        if (text == null) return;

        float start = displayValues.TryGetValue(text, out float d) ? d : 0f;

        if (activeTweens.TryGetValue(text, out Tween t)) t.Kill();

        Tween tween = DOTween.To(() => start, x =>
        {
            displayValues[text] = x;
            text.text = NumberFormatter.FormatNumber(Mathf.RoundToInt(x));
        }, target, duration)
        .SetEase(Ease.OutCubic)
        .OnComplete(() =>
        {
            displayValues[text] = target;
            text.text = NumberFormatter.FormatNumber(target);
        });

        activeTweens[text] = tween;
    }

    /// <summary>
    /// Sets the value instantly with no animation.
    /// </summary>
    public static void SetImmediate(TextMeshProUGUI text, int target)
    {
        if (text == null) return;

        if (activeTweens.TryGetValue(text, out Tween t)) t.Kill();
        displayValues[text] = target;
        text.text = NumberFormatter.FormatNumber(target);
    }
}
