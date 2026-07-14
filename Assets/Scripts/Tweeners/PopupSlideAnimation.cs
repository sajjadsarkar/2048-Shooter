using UnityEngine;
using DG.Tweening;

public class PopupSlideAnimations : MonoBehaviour
{
    public float slideDuration = 1f; // Duration of the slide animation
    public float slideDistance = 5f; // Distance to slide
    public float bounceHeight = 0.5f; // Height of the bounce
    public int bounceCount = 1; // Number of bounces
    public bool isRightToLeft = false; // Boolean for right-to-left animation
    public bool isUpToDown = true; // Boolean for up-to-down animation
    public bool isDownToUp = false; // Boolean for down-to-up animation
    public bool isLeftToRight = false; // Boolean for left-to-right animation
    private Vector3 initialPosition; // Initial position of the object
    private Sequence popUpSequence; // Sequence for animations
    private Vector3 slideDirection; // Direction vector for slide animation

    private void Awake()
    {
        // Store the initial position of the object at the start
        initialPosition = gameObject.transform.position;
    }

    private void OnEnable()
    {
        // Start the popup slide animation sequence
        PopupSlider();
    }

    private void OnDisable()
    {
        // Kill the sequence when the object is disabled
        if (popUpSequence != null)
        {
            popUpSequence.Kill();
        }
    }

    public void PopupSlider()
    { // Play panel swipe sound
        // if (Ludo.LudoAudioManager.instance != null)
        // {
        //     Ludo.LudoAudioManager.instance.AudioPlay(Ludo.LudoAudioManager.Clips.panel_swipe);
        // }
        // Determine the direction vector based on the booleans
        slideDirection = Vector3.zero;

        if (isRightToLeft)
        {
            slideDirection = Vector3.right * slideDistance;
        }
        else if (isUpToDown)
        {
            slideDirection = Vector3.up * slideDistance;
        }
        else if (isDownToUp)
        {
            slideDirection = Vector3.down * slideDistance;
        }
        else if (isLeftToRight)
        {
            slideDirection = Vector3.left * slideDistance;
        }

        // Move the object to the start position (above, below, to the right, or to the left of the screen)
        gameObject.transform.position = initialPosition + slideDirection;

        // Create a new sequence
        popUpSequence = DOTween.Sequence();

        // Slide to the initial position
        popUpSequence.Append(gameObject.transform.DOMove(initialPosition, slideDuration).SetEase(Ease.OutQuad));

        // Add bounces
        for (int i = 0; i < bounceCount; i++)
        {
            Vector3 bouncePosition = initialPosition;

            // Determine the bounce direction based on the booleans
            if (isRightToLeft || isLeftToRight)
            {
                bouncePosition += Vector3.right * bounceHeight;
            }
            else if (isUpToDown || isDownToUp)
            {
                bouncePosition += Vector3.up * bounceHeight;
            }

            popUpSequence.Append(gameObject.transform.DOMove(bouncePosition, slideDuration / (bounceCount * 2)).SetEase(Ease.OutQuad));
            popUpSequence.Append(gameObject.transform.DOMove(initialPosition, slideDuration / (bounceCount * 2)).SetEase(Ease.InQuad));
        }

        // Start the sequence
        popUpSequence.Play();
    }

    public void ClosePopup()
    {
        // Create a new sequence for the closing animation
        Sequence closeSequence = DOTween.Sequence();

        // Determine the direction vector based on the booleans for closing animation
        Vector3 closeDirection = Vector3.zero;

        if (isRightToLeft)
        {
            closeDirection = Vector3.left * slideDistance;
        }
        else if (isUpToDown)
        {
            closeDirection = Vector3.down * slideDistance;
        }
        else if (isDownToUp)
        {
            closeDirection = Vector3.up * slideDistance;
        }
        else if (isLeftToRight)
        {
            closeDirection = Vector3.right * slideDistance;
        }

        // Animate the object to slide out of view
        closeSequence.Append(gameObject.transform.DOMove(initialPosition + closeDirection, slideDuration).SetEase(Ease.InQuad));

        // Disable the GameObject after the animation completes and reset the position
        closeSequence.OnComplete(() =>
        {
            gameObject.SetActive(false);
            gameObject.transform.position = initialPosition; // Reset to initial position
        });

        // Start the close sequence
        closeSequence.Play();
    }
}
