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
        if (!EnsureDof())
            return;

        dof.active = false; // Only pay for the DoF pass while the inventory is actually open
        dof.focusDistance.value = 20f; // Start far (no blur)
    }

    public void EnableBlur()
    {
        if (!EnsureDof())
            return;

        if (currentCoroutine != null) StopCoroutine(currentCoroutine);
        dof.active = true;
        currentCoroutine = StartCoroutine(LerpFocusDistance(dof.focusDistance.value, 0.1f, deactivateAfter: false));
    }

    public void DisableBlur()
    {
        if (!EnsureDof())
            return;

        if (currentCoroutine != null) StopCoroutine(currentCoroutine);
        currentCoroutine = StartCoroutine(LerpFocusDistance(dof.focusDistance.value, 20f, deactivateAfter: true));
    }

    private bool EnsureDof()
    {
        if (dof != null)
            return true;

        if (postProcessingVolume == null || postProcessingVolume.profile == null)
            return false;

        if (!postProcessingVolume.profile.TryGet(out dof) || dof == null)
        {
            Debug.LogWarning("DepthOfField not found on Volume.", this);
            return false;
        }

        return true;
    }

    private IEnumerator LerpFocusDistance(float start, float end, bool deactivateAfter)
    {
        float t = 0f;

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime * blurSpeed;
            if (dof != null)
                dof.focusDistance.value = Mathf.Lerp(start, end, t);
            yield return null;
        }

        if (dof != null)
        {
            dof.focusDistance.value = end;
            if (deactivateAfter) dof.active = false;
        }
    }
}
