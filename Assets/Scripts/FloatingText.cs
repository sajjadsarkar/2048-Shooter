using UnityEngine;
using TMPro;
using DG.Tweening;

public class FloatingText : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private float moveDuration = 0.7f;
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private float moveDistance = 100f;

    public void Initialize(int score, Color color)
    {
        // Set the text with a "+" prefix to show it's an addition
        scoreText.text = "+" + score.ToString();

        // Always set text color to white for better visibility
        scoreText.color = Color.black;

        // Start the animation
        AnimateText();
    }

    private void AnimateText()
    {
        // Store the original position
        Vector3 startPos = transform.position;

        // Create a sequence of animations
        Sequence sequence = DOTween.Sequence();

        // Move downward (negative Y direction)
        sequence.Append(transform.DOMoveY(startPos.y - moveDistance, moveDuration)
            .SetEase(Ease.OutCubic));

        // Fade out near the end of the movement
        sequence.Insert(moveDuration - fadeDuration,
            scoreText.DOFade(0, fadeDuration).SetEase(Ease.InQuad));

        // Destroy the game object when animation completes
        sequence.OnComplete(() => Destroy(gameObject));
    }
}
