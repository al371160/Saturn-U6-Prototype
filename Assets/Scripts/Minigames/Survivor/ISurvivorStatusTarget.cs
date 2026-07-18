using UnityEngine;

public interface ISurvivorStatusTarget
{
    void ApplyKnockback(Vector3 direction, float force);

    /// <summary>1 = normal speed, 0 = fully stopped. Effects combine multiplicatively.</summary>
    void SetSlowMultiplier(float multiplier);
}
