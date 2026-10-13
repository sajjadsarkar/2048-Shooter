using UnityEngine;
using TMPro;
using DG.Tweening;

public class FloatingText : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private float moveDuration = 0.7f;
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private float moveDistance = 130f;

    public void Initialize(int score, Color color)
    {
        // Set the text with a "+" prefix to show it's an addition
        scoreText.text = "+" + score;
        scoreText.color = new Color(0.35f, 1f, 0.45f);
        scoreText.fontSize = Mathf.Clamp(50f + Mathf.Log(score, 2) * 3f, 50f, 90f);
        scoreText.outlineWidth = 0.4f;
        scoreText.outlineColor = Color.black;
        scoreText.fontStyle = FontStyles.Bold;
        scoreText.enableWordWrapping = false;
        transform.SetAsLastSibling();
        transform.rotation = Quaternion.Euler(0, 0, Random.Range(-6f, 6f));

        transform.localScale = Vector3.zero;
        transform.DOScale(1f, 0.3f).SetEase(Ease.OutElastic);
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
            gameObject.AddComponent<CanvasGroup>().DOFade(0, fadeDuration).SetEase(Ease.InQuad));

        // Destroy the game object when animation completes
        sequence.OnComplete(() => Destroy(gameObject));
    }
}
