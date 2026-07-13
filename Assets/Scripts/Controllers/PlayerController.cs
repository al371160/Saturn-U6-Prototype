using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("References")]
    public Animator playerAnim;
    public CharacterController controller;
    public Transform groundCheck;
    public LayerMask groundMask;
    public PlayerSwimming swimming;

    [Header("Effects")]
    public ParticleSystem footstepParticles;
    public TrailRenderer leftGlideTrail;
    public TrailRenderer rightGlideTrail;
    public ParticleSystem jumpParticles;
    public ParticleSystem swimParticles;

    [Header("Audio")]
    public AudioSource runAudioSource;
    public AudioClip runClip;

    public AudioSource jumpAudioSource;
    public AudioClip jumpClip;

    public AudioSource glideAudioSource;
    public AudioClip glideClip;

    public AudioSource swimAudioSource;
    public AudioClip swimClip;

    [Header("Run Audio Settings")]
    public float walkAudioPitch = 1.0f;
    public float sprintAudioPitch = 1.2f;


    [Header("Audio Settings")]
    public bool randomizePitch = true;
    public float minPitch = 0.9f;
    public float maxPitch = 1.1f;

    [Header("Camera")]
    public float mouseSensitivity = 100f;

    [Header("Movement")]
    public float walkSpeed = 6f;
    public float sprintSpeed = 10f;
    private Vector3 inputDir; // Accessible across methods

    [Header("Jump/Gravity")]
    public float gravity = -9.81f;
    public float jumpHeight = 1.5f;
    public Vector3 velocity;
    public bool isGrounded;

    [Header("Dash")]
    public float dashDistance = 10f;
    public float dashCooldown = 1f;
    private bool canDash = true;
    private bool isDashing = false;
    private float dashTime = 0.2f;
    private float dashTimer;

    [Header("Glide")]
    public float glideMultiplier = -1.2f; // Adjusted from -2f
    public float glideForwardSpeed = 3f;  // New

    private bool isGliding = false;

    [Header("Stamina")]
    public float maxStamina = 100f;
    public float currentStamina = 100f;
    public float staminaRegenRate = 15f;
    public float jumpStaminaCost = 20f;
    public float dashStaminaCost = 25f;
    public float glideStaminaPerSecond = 10f;
    public float sprintStaminaPerSecond = 20f;
    public float climbStaminaPerSecond = 10f; // New variable for climbing stamina cost
    [Tooltip("Delay before stamina starts to regenerate after reaching 0.")]
    public float staminaRechargeDelay = 2f; // seconds
    private float staminaRechargeTimer = 0f;
    public bool suppressStaminaRegen = false;
    private bool inRegenZone = false;
    public bool areWeSwimming = false;

    //rotation b4 victory
    public float rotationSpeed = 360f;
    private bool isFrozen = false;

    //zoom b4 vic
    public HikeCamZoom hikeCamZoom;


    [Header("Ground Check")]
    public float groundDistance = 0.3f;

    [Header("Control Flags")]
    public bool canMove = true;

    private bool isSprinting = false;

    private Vector3 hitPoint;

    [Header("Climbing")]
    public LayerMask climbableMask;
    public float climbSpeed = 3f;
    public float climbCheckDistance = 1f;
    private bool isClimbing = false;
    private Vector3 climbNormal;
    private float climbCooldownTimer = 0f;
    public float climbCooldownDuration = 0.5f;
    public float footOrigin = 1f;

    private bool isExhaustedFall = false;

    /*[Header("Exhausted Jump")]
    public float exhaustedPushOffForce = 8f;
    
    [Header("Exhausted Fall")]
    public float exhaustedBackForce = 2f;
    public float exhaustedUpForce = 4f;
    public float exhaustedImmunityTime = 0.6f;

    [Header("Exhausted Push")]
    public float exhaustedPushDuration = 0.25f;
    private float exhaustedPushTimer = 0f; */

    [Header("Ragdoll")]
    public Rigidbody[] ragdollBodies;
    public Collider[] ragdollColliders;

    public float recoverVelocityThreshold = 1.2f;
    public float minRagdollTime = 0.6f;
    public float ragdollRecoveryLift = 1.5f;

    public bool isRagdoll = false;
    private float ragdollTimer = 0f;
    //public bool ragdollQueued = false;

    private bool exhaustedWaitingForJump = false;
    private Transform ragdollHips;



    //freeToMove
    private bool freeToMove = true; 
    //this bool is true when the player is able to be caught by the climb stamina freeze function in climbing,
    //meaning it will set can move and canlook as false.



    [Header("Mouse Rotation")]
    public LayerMask mouseAimMask = ~0; // Add this to specify what the raycast should hit
    public float rotateMultiplier = 10f;
    public Collider raycastTargetCollider; // Collider to exclude from ragdoll collisions
    

    void Start()
    {
        //Cursor.lockState = CursorLockMode.Locked;
        //Cursor.visible = false;
        swimming = GetComponent<PlayerSwimming>();
        if (leftGlideTrail != null) leftGlideTrail.emitting = false;
        if (rightGlideTrail != null) rightGlideTrail.emitting = false;

        ragdollHips = playerAnim.GetBoneTransform(HumanBodyBones.Hips);
        if (ragdollHips == null) Debug.LogError("Hips not found! Ragdoll will fail.");

        ragdollBodies = GetComponentsInChildren<Rigidbody>();
        ragdollBodies = System.Array.FindAll(ragdollBodies, rb => rb.gameObject != gameObject);

        ragdollColliders = GetComponentsInChildren<Collider>();

        SetRagdoll(false);

    }

    void Update()
    {
        // 1. ROTATION: Always try to rotate first unless physically a ragdoll
        // This fixes the issue where you couldn't look around at the start.
        if (canLook)
        {
            RotateTowardsMousePoint();
            Debug.Log("canLook: " + canLook);
        }

        // 2. RAGDOLL STATE: If ragdolling, skip everything else
        if (isRagdoll)
        {
            ragdollTimer += Time.deltaTime;
            // Keep root position updated to hips so the camera follows
            transform.position = ragdollHips.position;

            if (ragdollTimer < minRagdollTime) return;

            float maxVel = 0f;
            foreach (Rigidbody rb in ragdollBodies)
                maxVel = Mathf.Max(maxVel, rb.linearVelocity.magnitude);

            if (maxVel < recoverVelocityThreshold) RecoverFromRagdoll();
            return;
        }

        // 3. EXHAUSTED FALL: Skip movement logic but keep gravity
        if (isExhaustedFall)
        {
            ApplyGravity();
            if (exhaustedWaitingForJump && Input.GetButtonDown("Jump"))
            {
                exhaustedWaitingForJump = false;
                StartRagdoll();
            }
            return; 
        }

        // 4. GENERAL MOVEMENT BLOCK (Frozen/Cinematic)
        if (!canMove)
        {
            ApplyGravity();
            UpdateFootstepParticles();
            return;
        }

        UpdateFootstepParticles();

        //Debug.Log(controller.velocity.magnitude);
        areWeSwimming = swimming.isSwimming;
        //Debug.Log("areweswimming in update of playercontroller: " +areWeSwimming);
        //swimming stamina regen. for some reason anything below doesn't work
        if (areWeSwimming)
        {
            //Debug.Log("passed else statement! areweswimming: " + areWeSwimming);
            float regenMultiplier = 1f;
            if (areWeSwimming)
            {
                regenMultiplier *= 2f;
            }

            currentStamina += staminaRegenRate * regenMultiplier * Time.deltaTime;
            currentStamina = Mathf.Min(currentStamina, maxStamina);
            playerAnim.SetBool("exhausted", false);
        }


        //Debug.Log(currentStamina);
        // Ground Check
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }
        Debug.Log("canMove: " + canMove);
        Debug.Log("isExhaustedFall: " + isExhaustedFall);
        Debug.Log("freeToMove: " + freeToMove);


        /* if (isExhaustedFall && !isRagdoll)
        {
            StartCoroutine(DelayedRagdoll());
            return;
        } */

        // Let swimming override movement
        if (swimming != null && swimming.IsSwimming())
        {
            return;
        }

        // Input
        //float horizontal = 0f;
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        inputDir = new Vector3(horizontal, 0f, vertical).normalized;
        float targetSpeed = walkSpeed;

        // Sprint
        isSprinting = Input.GetKey(KeyCode.LeftShift) && inputDir.magnitude > 0.1f && currentStamina > 0 && isGrounded;
        if (isSprinting)
        {
            targetSpeed = sprintSpeed;
            UseStamina(sprintStaminaPerSecond * Time.deltaTime);
            playerAnim.SetBool("isSprinting", true);
        } else{
            playerAnim.SetBool("isSprinting", false);
        }

        // Movement
        RaycastHit hit;
        Vector3 moveDir = transform.forward * vertical + transform.right * horizontal;
        if (Physics.Raycast(transform.position, moveDir.normalized, out hit, 1f, groundMask))
        {
            float angle = Vector3.Angle(hit.normal, Vector3.up);
            if (angle > controller.slopeLimit)
            {
                // Prevent walking up too-steep slope
                moveDir = Vector3.zero;
            }
        }
 
        controller.Move(moveDir.normalized * targetSpeed * Time.deltaTime);
        

        playerAnim.SetBool("isRunning", inputDir.magnitude >= 0.1f && isGrounded);

        
        // Jump
        if (Input.GetButtonDown("Jump") && currentStamina >= jumpStaminaCost)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            UseStamina(jumpStaminaCost);
            playerAnim.SetTrigger("isJumping");

            if (jumpParticles != null)
                jumpParticles.Play(); // <- Jump effect here

            if (jumpAudioSource != null && jumpClip != null)
            {
                jumpAudioSource.clip = jumpClip;

                if (randomizePitch)
                    jumpAudioSource.pitch = Random.Range(minPitch, maxPitch);
                else
                    jumpAudioSource.pitch = 1f;

                jumpAudioSource.Play();
            }
        }



        // Glide
        isGliding = !isGrounded && Input.GetButton("Jump") && velocity.y < 0 /*&& currentStamina > 0 */;
        if (isGliding)
        {
            UseStamina(glideStaminaPerSecond * Time.deltaTime);
            playerAnim.SetBool("isGliding", true);

            if (glideAudioSource != null && glideClip != null && !glideAudioSource.isPlaying)
            {
                glideAudioSource.clip = glideClip;
                glideAudioSource.loop = true;

                if (randomizePitch)
                    glideAudioSource.pitch = Random.Range(minPitch, maxPitch);
                else
                    glideAudioSource.pitch = 1f;

                glideAudioSource.Play();
            }


            // Optional forward momentum
            Vector3 forwardBoost = transform.forward * glideForwardSpeed;
            controller.Move(forwardBoost * Time.deltaTime);
        } else
        {
            playerAnim.SetBool("isGliding", false);
            if (glideAudioSource != null && glideAudioSource.isPlaying)
            {
                glideAudioSource.Stop();
            }

        }


        // Gravity
        if (isGliding)
        {
            velocity.y = glideMultiplier; // Or whatever constant fall speed you want while gliding
        }
        else
        {
            velocity.y += gravity * Time.deltaTime;
        }


        if (!isDashing)
        {
            controller.Move(velocity * Time.deltaTime);
        }

        // Dash
        if (Input.GetKeyDown(KeyCode.C) && canDash && !isDashing && inputDir != Vector3.zero && currentStamina >= dashStaminaCost)
        {
            UseStamina(dashStaminaCost);
            StartCoroutine(Dash(inputDir));
            playerAnim.SetTrigger("isDashing");
        }

        if (!isSprinting && !isGliding && !isClimbing && currentStamina < maxStamina && isGrounded)
        {
            //Debug.Log("passed check! isswimming: " + swimming.IsSwimming());
            //Debug.Log("passed check! areweswimming: " + areWeSwimming);
            if (staminaRechargeTimer > 0)
            {
                staminaRechargeTimer -= Time.deltaTime;
            }
            else
            {
                float regenMultiplier = 1f;

                if (suppressStaminaRegen) 
                {
                    regenMultiplier *= 0f; // Regenerates 75% slower, or you can set this to 0f to completely block it
                }
                currentStamina += staminaRegenRate * regenMultiplier * Time.deltaTime;
                currentStamina = Mathf.Min(currentStamina, maxStamina);
            }
        } 

        // Interaction Animation
        if (Input.GetKeyDown(KeyCode.E) && isGrounded)
        {
            playerAnim.SetTrigger("pressedE");
        }


        if (climbCooldownTimer > 0)
        {
            climbCooldownTimer -= Time.deltaTime;
            return;
        }

        //Climbing
        CheckForClimb();

    if (isClimbing)
    {
        // Get vertical and lateral input
        float verticalInput = Input.GetAxisRaw("Vertical");
        float horizontalInput = Input.GetAxisRaw("Horizontal");

        // Calculate climbing surface direction
        Vector3 up = Vector3.up;
        Vector3 right = Vector3.Cross(climbNormal, up).normalized;
        Vector3 forward = Vector3.Cross(right, climbNormal).normalized;

        // Final move direction
        Vector3 climbDirection = forward * verticalInput + right * horizontalInput;

        // Drain stamina
        UseStamina(climbStaminaPerSecond * Time.deltaTime);

        controller.Move(climbDirection * climbSpeed * Time.deltaTime);
        playerAnim.SetBool("isClimbing", climbDirection.magnitude > 0.01f);
        isClimbing = playerAnim.GetBool("isClimbing");

        return; // Skip other movement
    }


    if (!isClimbing)
    {
        playerAnim.SetBool("isClimbing", false);
    }

    UpdateFootstepParticles();




    // GLIDING TRAILS
    if (leftGlideTrail != null && rightGlideTrail != null)
    {
        if (isGliding)
        {
            if (!leftGlideTrail.emitting) leftGlideTrail.emitting = true;
            if (!rightGlideTrail.emitting) rightGlideTrail.emitting = true;
        }
        else
        {
            if (leftGlideTrail.emitting) leftGlideTrail.emitting = false;
            if (rightGlideTrail.emitting) rightGlideTrail.emitting = false;
        }
    }


    // SWIMMING PARTICLES
    if (swimParticles != null)
    {
        bool swimmingNow = swimming != null && swimming.IsSwimming();

        if (swimmingNow && !swimParticles.isPlaying)
            swimParticles.Play();
        else if (!swimmingNow && swimParticles.isPlaying)
            swimParticles.Stop();
    }


}



    public void PlayImportantItemAnimation()
    {
        if (playerAnim != null)
        {
            playerAnim.SetTrigger("FoundImportantItem");
        }

        StartCoroutine(RotateTowardsCamera());
        StartCoroutine(FreezePlayer(3.5f));
        isFrozen = true;

        Debug.Log("playing important item animation");

        if (hikeCamZoom != null)
        {
            hikeCamZoom.ZoomForCelebration(); 
            Debug.Log("playing important item animation!!");
        }
    }

    public void UpdateFootstepParticles()
    {
        bool isMovingOnGround = !isClimbing && isGrounded && inputDir.magnitude > 0.1f && !isClimbing && !isFrozen;

        if (footstepParticles != null)
        {
            if (isMovingOnGround && !footstepParticles.isPlaying)
                footstepParticles.Play();
            else if (!isMovingOnGround && footstepParticles.isPlaying)
                footstepParticles.Stop();
        }

        if (isMovingOnGround)
        {
            if (!runAudioSource.isPlaying && runClip != null)
            {
                runAudioSource.clip = runClip;
                runAudioSource.loop = true;

                if (isSprinting)
                    runAudioSource.pitch = sprintAudioPitch;
                else
                    runAudioSource.pitch = walkAudioPitch;

                runAudioSource.Play();
            }
            else if (runAudioSource.isPlaying)
            {
                if (isSprinting)
                    runAudioSource.pitch = sprintAudioPitch;
                else
                    runAudioSource.pitch = walkAudioPitch;
            }
        }
        else
        {
            if (runAudioSource.isPlaying)
                runAudioSource.Stop();
        }
    }


    private IEnumerator RotateTowardsCamera()
    {
        if (!canLook || isExhaustedFall)
        yield break;

        Vector3 cameraPosition = Camera.main.transform.position;
        Vector3 direction = cameraPosition - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.01f)
            yield break;

        Quaternion targetRotation = Quaternion.LookRotation(direction);

        while (Quaternion.Angle(transform.rotation, targetRotation) > 0.1f)
        {
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            yield return null;

            // Update target in case camera moves during rotation
            cameraPosition = Camera.main.transform.position;
            direction = cameraPosition - transform.position;
            direction.y = 0f;
            targetRotation = Quaternion.LookRotation(direction);
        }

        transform.rotation = targetRotation;
    }


    private IEnumerator FreezePlayer(float duration)
    {
        canMove = false;
        canLook = false;
        isFrozen = true;

        yield return new WaitForSeconds(duration);

        canMove = true;
        canLook = true;
        isFrozen = false;
    }

    /*private void ApplyExhaustedPush()
    {
        if (exhaustedPushTimer > 0f)
        {
            exhaustedPushTimer -= Time.deltaTime;

            Vector3 pushDir = -transform.forward;
            controller.Move(pushDir * exhaustedBackForce * Time.deltaTime);
        }
    } 


    private void ExhaustedPushOff()
    {
        exhaustedPushTimer = exhaustedPushDuration;

        // Vertical impulse only
        velocity.y = exhaustedUpForce;

        playerAnim.SetBool("exhausted", false);
        playerAnim.SetBool("isClimbing", false);
        playerAnim.SetTrigger("isJumping");
        jumpAudioSource.Play();

        StartCoroutine(playerMovementImmunity(exhaustedImmunityTime));
    } */


    public void SetInRegenZone(bool value)
    {
        inRegenZone = value;
    }


    private void ApplyGravity()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        if (!isGrounded)
        {
            velocity.y += gravity * Time.deltaTime;
            controller.Move(Vector3.up * velocity.y * Time.deltaTime);
        }
        else if (velocity.y < 0)
        {
            velocity.y = -2f;
        }
    }


    private void UseStamina(float amount)
    {
        currentStamina -= amount;

        if (currentStamina <= 0)
        {
            currentStamina = 0;
            staminaRechargeTimer = staminaRechargeDelay;

            if (isClimbing && !isExhaustedFall)
            {
                EnterExhaustedFall();
            }
        }
    }

    private void EnterExhaustedFall()
    {
        isExhaustedFall = true;
        exhaustedWaitingForJump = true;

        canMove = false;
        canLook = false;
        freeToMove = false;

        velocity = Vector3.zero;

        playerAnim.SetBool("exhausted", true);
        playerAnim.SetBool("isClimbing", false);
    }



    private IEnumerator Dash(Vector3 inputDir)
    {
        canDash = false;
        isDashing = true;
        dashTimer = dashTime;

        Vector3 dashDirection = transform.forward * inputDir.z + transform.right * inputDir.x;

        while (dashTimer > 0)
        {
            controller.Move(dashDirection.normalized * dashDistance * Time.deltaTime / dashTime);
            dashTimer -= Time.deltaTime;
            yield return null;
        }

        isDashing = false;
        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }


    public bool canLook = true; // Add this to your player class if not already present

    private void RotateTowardsMousePoint()
    {
        Debug.Log("rotating...");
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Debug.Log("ray exists");

        if (Physics.Raycast(ray, out RaycastHit hit, 100f, mouseAimMask)) // Limit to 100 units and filtered mask
        {
            Debug.Log("ray hit");
            Vector3 direction = hit.point - transform.position;
            direction.y = 0f; // Keep rotation level

            if (direction.sqrMagnitude > 0.01f)
            {
                if (isGliding)
                {
                    rotateMultiplier = 3f;
                }
                else if (swimming != null && swimming.IsSwimming())
                {
                    rotateMultiplier = 5f; // slower turn in water
                }
                else
                {
                    rotateMultiplier = 10f;
                }

                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotateMultiplier);
            }

            hitPoint = hit.point; // Save for visual debugging or other uses
        }
    }



    private void CheckForClimb()
    {
        RaycastHit hit;
        Vector3 origin = transform.position + Vector3.up * footOrigin;
        Vector3 direction = transform.forward;

        if (Physics.Raycast(origin, direction, out hit, climbCheckDistance, climbableMask))
        {
            if (Input.GetKey(KeyCode.W) && currentStamina > 0)
            {
                isClimbing = true;
                climbNormal = hit.normal;
                velocity.y = 0f; // cancel falling
            }
        }
        else
        {
            isClimbing = false;
        }
}


    public void ChangeMaxStamina(int input)
    {
        maxStamina = maxStamina + input;
    }

    private void StartRagdoll()
    {
        isRagdoll = true;
        ragdollTimer = 0f;

        controller.enabled = false;
        playerAnim.enabled = false;

        // Position root BEFORE activating physics so bones don't spawn inside terrain
        Vector3 safePos = ragdollHips.position;
        if (isClimbing)
            safePos += climbNormal * 0.5f;  // pull away from wall
        else
            safePos += Vector3.up * 0.1f;
        transform.position = safePos;

        SetRagdoll(true);

        // Push ragdoll bodies away from the wall so they don't immediately collide
        if (isClimbing)
        {
            Vector3 pushVel = climbNormal * 3f + Vector3.up * 2f;
            foreach (Rigidbody rb in ragdollBodies)
                rb.linearVelocity = pushVel;
        }
    }



    private void SetRagdoll(bool enabled)
    {
        isRagdoll = enabled;

        foreach (Rigidbody rb in ragdollBodies)
        {
            rb.isKinematic = !enabled;
            rb.useGravity = enabled; // Ensure gravity is on for ragdolls
            
            if (enabled)
            {
                // Help prevent clipping through ground on the first frame
                rb.interpolation = RigidbodyInterpolation.Interpolate;
            }
        }

        Collider[] exceptions = { raycastTargetCollider }; // add more if needed
        foreach (Collider col in ragdollColliders)
        {
            if (System.Array.Exists(exceptions, e => e == col)) continue;
            col.enabled = enabled;
        }


        // The main controller must be off for ragdoll to work
        controller.enabled = !enabled;
        playerAnim.enabled = !enabled;

    }

    private void RecoverFromRagdoll()
    {
        isRagdoll = false;
        ragdollTimer = 0f;

        // STEP 1: Find a safe position
        Vector3 targetPos = ragdollHips.position;
        RaycastHit hit;
        if (Physics.Raycast(targetPos, Vector3.down, out hit, 2f, groundMask))
        {
            targetPos.y = hit.point.y + controller.height / 2f + ragdollRecoveryLift;
        }
        else
        {
            targetPos.y += ragdollRecoveryLift;
        }
        transform.position = targetPos;

        // STEP 2: Rotation
        transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);

        // STEP 3: Disable ragdoll physics
        SetRagdoll(false);

        // STEP 4: Re-enable controller and animator
        controller.enabled = true;
        playerAnim.enabled = true;
        playerAnim.Rebind();
        playerAnim.SetBool("exhausted", false);
        playerAnim.SetBool("isClimbing", false);

        // STEP 5: Reset flags
        canMove = true;
        canLook = true;
        freeToMove = true;
        isExhaustedFall = false;

        // STEP 6: Reset velocity
        velocity = Vector3.zero;
    }




}
