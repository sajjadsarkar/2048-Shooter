using DG.Tweening;
using UnityEngine;

public class ObjectRotator : MonoBehaviour
{
    public Vector3 rotationAmount = new Vector3(0, 0, 90);
    public float duration = 1.0f;
    public RotateMode rotateMode = RotateMode.FastBeyond360; // Choose rotate mode according to your needs

    private Sequence rotationSequence;

    /// <summary>
    /// This function is called when the object becomes enabled and active.
    /// </summary>
    void OnEnable()
    {
        // Call the RotateObject function when the script starts
        RotateObject();
    }

    void OnDisable()
    {
        // Kill the sequence when the object is disabled
        if (rotationSequence != null)
        {
            rotationSequence.Kill();
        }
    }

    void RotateObject()
    {
        // Create a new sequence
        rotationSequence = DOTween.Sequence();

        // Add a rotation tween to the sequence
        rotationSequence.Append(
            transform.DORotate(rotationAmount, duration, rotateMode)
                .SetRelative(true) // Rotate relative to current rotation
                .SetEase(Ease.Linear) // Set ease type to linear for smooth rotation
        );

        // Set the sequence to loop indefinitely
        rotationSequence.SetLoops(-1, LoopType.Restart);
    }
}
