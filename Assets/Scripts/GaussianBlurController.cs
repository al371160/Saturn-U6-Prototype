using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class GaussianBlurController : MonoBehaviour
{
    public Volume postProcessingVolume;
    public float blurSpeed = 20f;

    private DepthOfField dof;
    private Coroutine currentCoroutine;

    private void Start()
    {
        if (postProcessingVolume.profile.TryGet(out dof))
        {
            dof.active = true;
            dof.focusDistance.value = 20f; // Start far (no blur)
        }
        else
        {
            Debug.LogWarning("DepthOfField not found on Volume.");
        }
    }

    public void EnableBlur()
    {
        if (currentCoroutine != null) StopCoroutine(currentCoroutine);
        currentCoroutine = StartCoroutine(LerpFocusDistance(dof.focusDistance.value, 0.1f));
    }

    public void DisableBlur()
    {
        if (currentCoroutine != null) StopCoroutine(currentCoroutine);
        currentCoroutine = StartCoroutine(LerpFocusDistance(dof.focusDistance.value, 20f));
    }

    private IEnumerator LerpFocusDistance(float start, float end)
    {
        float t = 0f;

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime * blurSpeed; // Changed here
            dof.focusDistance.value = Mathf.Lerp(start, end, t);
            yield return null;
        }

        dof.focusDistance.value = end;
    }

}
