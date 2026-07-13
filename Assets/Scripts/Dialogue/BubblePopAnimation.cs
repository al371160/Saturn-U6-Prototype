using System.Collections;
using UnityEngine;

public class BubblePopAnimation : MonoBehaviour
{
    [Header("X Axis Animation")]
    public float popDurationX = 0.2f;
    public AnimationCurve popCurveX = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Y Axis Animation")]
    public float popDurationY = 0.2f;
    public AnimationCurve popCurveY = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private Coroutine currentAnim;

    public void PlayPop()
    {
        if (currentAnim != null) StopCoroutine(currentAnim);
        currentAnim = StartCoroutine(DoPop());
    }

    IEnumerator DoPop()
    {
        transform.localScale = Vector3.zero;

        float t = 0f;
        while (t < Mathf.Max(popDurationX, popDurationY))
        {
            float scaleX = t < popDurationX ? popCurveX.Evaluate(t / popDurationX) : 1f;
            float scaleY = t < popDurationY ? popCurveY.Evaluate(t / popDurationY) : 1f;

            transform.localScale = new Vector3(scaleX, scaleY, 1f);

            t += Time.unscaledDeltaTime; // ✅ Use unscaled time here
            yield return null;
        }

        transform.localScale = Vector3.one;
    }

}
