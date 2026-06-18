using UnityEngine;
using System.Collections;

public class Axe : MonoBehaviour
{
    [Header("References")]
    public Animator playerAnimator;
    public Transform axeTip;
    public LayerMask treeLayer;
    public PlayerController playerController;

    [Header("Tool Settings")]
    public float swingRange = 1.0f;

    [Header("Timing")]
    public float swingDuration = 1.0f;
    public float hitDetectionDelay = 0.1f;
    public float particleDelay = 0.2f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip axeSwingSound;

    [Header("Effects")]
    public ParticleSystem chopParticles;

    private bool isSwinging = false;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R) && !isSwinging)
        {
            StartCoroutine(PerformSwing());
        }

        if (playerController != null)
        {
            playerController.UpdateFootstepParticles();
        }
    }

    IEnumerator PerformSwing()
    {
        isSwinging = true;

        // Trigger animation
        playerAnimator?.SetTrigger("AxeSwing");

        // Lock movement
        if (playerController != null)
        {
            playerController.canMove = false;
            playerController.canLook = false;
        }

        // Play sound
        if (audioSource && axeSwingSound)
            audioSource.PlayOneShot(axeSwingSound);

        // Delay before hitting the tree
        yield return new WaitForSeconds(hitDetectionDelay);

        // Hit detection (OverlapSphere)
        Collider[] hits = Physics.OverlapSphere(axeTip.position, swingRange, treeLayer);
        foreach (Collider hit in hits)
        {
            Tree tree = hit.GetComponent<Tree>();
            if (tree != null)
            {
                tree.TakeDamage(1);
                break;
            }
        }

        // Delay before playing particles (independent)
        float remainingToParticles = particleDelay - hitDetectionDelay;
        if (remainingToParticles > 0)
            yield return new WaitForSeconds(remainingToParticles);

        // Play particles
        if (chopParticles != null)
        {
            ParticleSystem particles = Instantiate(chopParticles, axeTip.position, axeTip.rotation);
            particles.Play();
            Destroy(particles.gameObject, particles.main.duration + particles.main.startLifetime.constantMax);
        }

        // Wait out the rest of the swing
        float remainingToEnd = swingDuration - Mathf.Max(hitDetectionDelay, particleDelay);
        if (remainingToEnd > 0)
            yield return new WaitForSeconds(remainingToEnd);

        // Unlock movement
        if (playerController != null)
        {
            playerController.canMove = true;
            playerController.canLook = true;
        }

        isSwinging = false;
    }

    // Visualize hit area in editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(axeTip.position, swingRange);
    }
}
