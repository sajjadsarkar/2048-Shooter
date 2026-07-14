using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class ObjectScaler : MonoBehaviour
{
    public Vector3 scaleUpAmount = new Vector3(1.1f, 1.1f, 1.1f); // Slightly larger scale for a subtle effect
    public float scaleUpDuration = 0.2f;  // Faster scale up
    public float scaleDownDuration = 0.15f; // Faster scale down

    private bool isScaling = false;
    private Sequence scalingSequence;

    /// <summary>
    /// This function is called when the object becomes enabled and active.
    /// </summary>
    void OnEnable()
    {
        // Call the ScaleObject function when the script starts
        ScaleObject();
    }

    /// <summary>
    /// This function is called when the behaviour becomes disabled or inactive.
    /// </summary>
    void OnDisable()
    {
        if (isScaling)
            scalingSequence.Kill();
    }

    void ScaleObject()
    {
        isScaling = true;

        // Create a new sequence for scaling
        scalingSequence = DOTween.Sequence();

        // Scale up
        scalingSequence.Append(transform.DOScale(scaleUpAmount, scaleUpDuration).SetEase(Ease.OutBack));

        // Scale down
        scalingSequence.Append(transform.DOScale(Vector3.one, scaleDownDuration).SetEase(Ease.InBack));

        // Loop the sequence infinitely
        scalingSequence.SetLoops(-1, LoopType.Yoyo);

        // Start the sequence
        scalingSequence.Play();
    }
}
