using UnityEngine;
using TMPro;
using DG.Tweening;

public class CoinPopup : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI coinText;
    [SerializeField] private float moveDuration = 0.7f;
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private float moveDistance = 50f;

    public void Initialize(int coins)
    {
        // Set the text with a "+" prefix to show it's an addition
        coinText.text = "+" + coins.ToString();

        // Set text color to coin gold color
        coinText.color = new Color(1f, 0.84f, 0.0f); // Gold color

        // Start the animation
        AnimateText();
    }

    private void AnimateText()
    {
        // Store the original position
        Vector3 startPos = transform.position;

        // Create a sequence of animations
        Sequence sequence = DOTween.Sequence();

        // Scale up animation
        sequence.Append(transform.DOScale(1.2f, 0.1f).SetEase(Ease.OutQuad));

        // Move upward
        sequence.Append(transform.DOMoveY(startPos.y + moveDistance, moveDuration)
            .SetEase(Ease.OutCubic));

        // Scale back to normal
        sequence.Insert(0.1f, transform.DOScale(1.0f, 0.2f).SetEase(Ease.InOutQuad));

        // Fade out near the end of the movement
        sequence.Insert(moveDuration - fadeDuration,
            coinText.DOFade(0, fadeDuration).SetEase(Ease.InQuad));

        // Destroy the game object when animation completes
        sequence.OnComplete(() => Destroy(gameObject));
    }
}
