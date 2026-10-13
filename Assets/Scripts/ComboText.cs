using UnityEngine;
using TMPro;
using DG.Tweening;

public class ComboText : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI comboText;
    [SerializeField] private float moveDuration = 1.1f;
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private float moveDistance = 140f;

    public void Initialize(int comboCount)
    {
        // Set the text to display the combo count
        comboText.text = "Combo x" + comboCount;
        comboText.color = new Color(1f, 0.55f, 0.1f);
        comboText.fontSize = 80f;
        comboText.outlineWidth = 0.4f;
        comboText.outlineColor = Color.black;
        comboText.fontStyle = FontStyles.Bold;
        comboText.enableWordWrapping = false;
        transform.SetAsLastSibling();
        transform.rotation = Quaternion.Euler(0, 0, Random.Range(-5f, 5f));

        transform.localScale = Vector3.zero;
        transform.DOScale(1f + Mathf.Min(comboCount, 8) * 0.12f, 0.4f).SetEase(Ease.OutElastic);
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
