using UnityEngine;
using Unity.Cinemachine;

public class CameraShakeTrigger : MonoBehaviour
{
    public CinemachineCamera cinemachineCam;
    public NoiseSettings newNoiseProfile;
    public float newAmplitude = 1f;
    public float newFrequency = 1f;

    private CinemachineBasicMultiChannelPerlin perlin;
    private NoiseSettings originalNoiseProfile;
    private float originalAmplitude;
    private float originalFrequency;

    void Start()
    {
        perlin = cinemachineCam.GetComponent<CinemachineBasicMultiChannelPerlin>();
        if (perlin == null)
        {
            Debug.LogError("CinemachineBasicMultiChannelPerlin component not found on the CinemachineCamera.");
            return;
        }

        // Save original settings to optionally restore later
        originalNoiseProfile = perlin.NoiseProfile;
        originalAmplitude = perlin.AmplitudeGain;
        originalFrequency = perlin.FrequencyGain;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) // Adjust this as needed
        {
            perlin.NoiseProfile = newNoiseProfile;
            perlin.AmplitudeGain = newAmplitude;
            perlin.FrequencyGain = newFrequency;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player")) // Optional: revert on exit
        {
            perlin.NoiseProfile = originalNoiseProfile;
            perlin.AmplitudeGain = originalAmplitude;
            perlin.FrequencyGain = originalFrequency;
        }
    }
}
