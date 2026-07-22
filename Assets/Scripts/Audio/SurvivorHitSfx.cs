using UnityEngine;

/// <summary>
/// Per-target hit audio: material impact (bullet/hit) plus an optional victim react (groan / creak).
/// Destroy is played by the damageable when it dies / shatters.
/// </summary>
[System.Serializable]
public class SurvivorHitSfx
{
    public AudioClip impact;
    public AudioClip react;
    public AudioClip destroy;

    [Range(0f, 1f)] public float impactVolume = 0.5f;
    [Range(0f, 1f)] public float reactVolume = 0.55f;
    [Range(0f, 1f)] public float destroyVolume = 0.85f;

    public bool HasAny => impact != null || react != null || destroy != null;
}
