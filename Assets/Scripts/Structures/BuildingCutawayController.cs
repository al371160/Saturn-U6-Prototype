using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Fades roof / exterior walls for the local player only via MaterialPropertyBlock.
/// Multiplayer-safe: never syncs fade state — each client runs this from their own trigger.
/// </summary>
public class BuildingCutawayController : MonoBehaviour
{
    public static readonly int FadeId = Shader.PropertyToID("_Fade");

    [Tooltip("Renderers that dissolve when the local player is inside (roof + fadeable walls).")]
    public List<Renderer> fadeRenderers = new List<Renderer>();

    public float fadeSpeed = 4f;
    [Tooltip("1 = fully faded to shader Inside Alpha (semi-transparent). Keep at 1 for see-through roofs.")]
    public float insideFade = 1f;
    public float outsideFade = 0f;

    private float targetFade;
    private float currentFade;
    private MaterialPropertyBlock propertyBlock;
    private int occupants;

    private void Awake()
    {
        propertyBlock = new MaterialPropertyBlock();

        if (fadeRenderers == null || fadeRenderers.Count == 0)
            AutoCollectFadeRenderers();

        targetFade = outsideFade;
        currentFade = outsideFade;
        ApplyFade(currentFade);
    }

    private void Update()
    {
        if (Mathf.Abs(currentFade - targetFade) < 0.001f)
            return;

        currentFade = Mathf.MoveTowards(currentFade, targetFade, fadeSpeed * Time.deltaTime);
        ApplyFade(currentFade);
    }

    public void NotifyLocalPlayerEntered()
    {
        occupants++;
        targetFade = insideFade;
    }

    public void NotifyLocalPlayerExited()
    {
        occupants = Mathf.Max(0, occupants - 1);
        if (occupants == 0)
            targetFade = outsideFade;
    }

    public void AutoCollectFadeRenderers()
    {
        fadeRenderers.Clear();
        CollectUnder("Roof_Fadeable");
        CollectUnder("Walls_Fadeable");
    }

    private void CollectUnder(string childName)
    {
        Transform root = transform.Find(childName);
        if (root == null)
            root = FindDeepChild(transform, childName);
        if (root == null)
            return;

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (!fadeRenderers.Contains(renderers[i]))
                fadeRenderers.Add(renderers[i]);
        }
    }

    private static Transform FindDeepChild(Transform parent, string name)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == name)
                return child;
            Transform nested = FindDeepChild(child, name);
            if (nested != null)
                return nested;
        }

        return null;
    }

    private void ApplyFade(float fade)
    {
        if (propertyBlock == null)
            propertyBlock = new MaterialPropertyBlock();

        for (int i = 0; i < fadeRenderers.Count; i++)
        {
            Renderer renderer = fadeRenderers[i];
            if (renderer == null)
                continue;

            renderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetFloat(FadeId, fade);
            renderer.SetPropertyBlock(propertyBlock);
        }
    }
}
