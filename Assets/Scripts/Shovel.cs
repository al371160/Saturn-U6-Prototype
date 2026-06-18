using UnityEngine;
using System.Collections;

public class Shovel : MonoBehaviour
{
    [Header("References")]
    public Animator playerAnimator;
    public Transform shovelTip;
    public float digRange = 2f;
    public LayerMask diggableLayer;
    public string diggableTag = "DiggableChest";
    public PlayerController playerController;

    [Header("Animation")]
    public string digTrigger = "ShovelSwing";
    public float swingDuration = 1.0f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip shovelSound;

    [Header("Digging Effects")]
    public ParticleSystem digParticles; // Assign in Inspector
    public float digParticleDelay = 0.2f; // Customize the delay in seconds

    private bool isDigging = false;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R) && !isDigging)
        {
            StartCoroutine(PerformDig());
        }

        if (playerController != null)
        {
            playerController.UpdateFootstepParticles();
        }
    }

    IEnumerator PerformDig()
    {
        isDigging = true;

        if (playerAnimator != null)
            playerAnimator.SetTrigger(digTrigger);

        if (audioSource != null && shovelSound != null)
            audioSource.PlayOneShot(shovelSound);

        if (playerController != null)
        {
            playerController.canMove = false;
            playerController.canLook = false;
        }

        // Wait for custom delay before spawning particles
        yield return new WaitForSeconds(digParticleDelay);

        if (digParticles != null)
        {
            ParticleSystem particles = Instantiate(digParticles, shovelTip.position, shovelTip.rotation);
            particles.Play();
            Destroy(particles.gameObject, particles.main.duration + particles.main.startLifetime.constantMax);
        }

        // Wait for remaining swing time
        yield return new WaitForSeconds(swingDuration - digParticleDelay);

        if (Physics.Raycast(shovelTip.position, shovelTip.forward, out RaycastHit hit, digRange, diggableLayer))
        {
            if (hit.collider.CompareTag(diggableTag))
            {
                Chest chest = hit.collider.GetComponent<Chest>();
                if (chest != null)
                {
                    chest.SendMessage("OpenChest", SendMessageOptions.DontRequireReceiver);
                }
            }
        }

        if (playerController != null)
        {
            playerController.canMove = true;
            playerController.canLook = true;
        }

        isDigging = false;
    }
}
