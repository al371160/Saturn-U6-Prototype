using UnityEngine;

public class SineWaveMovement : MonoBehaviour
{
    [Header("Sine Wave Settings")]
    public float amplitude = 1f;       // Height of the wave
    public float frequency = 1f;       // Speed of the wave
    public float phaseOffset = 0f;     // Optional phase offset

    private Vector3 startPosition;

    void Start()
    {
        startPosition = transform.position;
    }

    void Update()
    {
        float newY = Mathf.Sin(Time.time * frequency + phaseOffset) * amplitude;
        transform.position = new Vector3(startPosition.x, startPosition.y + newY, startPosition.z);
    }
}
