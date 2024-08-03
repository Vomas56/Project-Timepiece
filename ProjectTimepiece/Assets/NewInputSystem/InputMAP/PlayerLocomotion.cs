 using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerLocomotion : MonoBehaviour
{
    InputManager inputManager;
    // Variables needed for moving.
    Vector3 moveDirection;
    Transform cameraObject;
    Rigidbody botRigid;
    // Constants for ground movement.
    public float movementSpeed = 7;
    public float rotationSpeed = 15;
    public float dashTimer = 0;
    public float dashCoolDown = 0;
    public float dashPower = 3;
    // Constants for jumping.
    public float AirTime;
    public float jumpTimer = 0.5f;
    public float rayCastHeightOffset = 0.5f;
    public float leapingVelocity = 3;
    public float fallingVelocity = 33;
    public float jumpHeight = 3;
    public float gravityIntensity = -15;
    public LayerMask groundLayer;
    // Booleans that denote the state the player is in.
    public bool isInteracting;
    public bool isDashing;
    public bool isJumping;
    public bool isGrounded = true;


    private void Awake()
    {
        inputManager = GetComponent<InputManager>();
        botRigid = GetComponent<Rigidbody>();
        cameraObject = Camera.main.transform;
    }

    public void HandleAllMovement()
    {
        HandleCoolDown();
        // As dashing leaves you in midair we handle that first.
        if (isDashing)
        {
            HandleDash();
            return;
        }
        HandleFallingAndLanding();
        if (isInteracting)
        {
            return;
        }
        HandleMovement();
        HandleRotation();
    }

    private void HandleMovement()
    {
        // Creates a vector that moves the player based on camera orientation.
        moveDirection = cameraObject.forward * inputManager.verticalInput;
        moveDirection += cameraObject.right * inputManager.horizontalInput;
        moveDirection.Normalize();
        moveDirection.y = 0;
        moveDirection = moveDirection * movementSpeed;
        // Gives Rigid body velocity based on previous calculations.
        Vector3 movementVelocity = moveDirection;
        botRigid.velocity = movementVelocity;
    }

    private void HandleRotation()
    {
        Vector3 targetDirection = Vector3.zero;
        // Creates a vector that rotates a character to match their movement direction.
        targetDirection = cameraObject.forward * inputManager.verticalInput;
        targetDirection += cameraObject.right * inputManager.horizontalInput;
        targetDirection.Normalize();
        targetDirection.y = 0;
        // Keeps rotation if the character isn't present.
        if (targetDirection == Vector3.zero)
            targetDirection = transform.forward;
        // Uses quaternions to make rotation more gradual and less snappy.
        Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
        Quaternion playerRotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

        transform.rotation = playerRotation;
    }

    public void HandleFallingAndLanding()
    {
        // A raycast is used to determine if 
        RaycastHit hit;
        Vector3 rayCastOrigin = transform.position;
        rayCastOrigin.y += rayCastHeightOffset;

        if (isJumping)
        {
            if (inputManager.jump_held && jumpTimer < 0.25)
            {
                // Slightly increase the force of the jump.
                jumpTimer += Time.deltaTime;
                botRigid.AddForce(transform.forward * leapingVelocity / 100);
            }
            else if (botRigid.velocity.y <= 0)
            {
                isJumping = false;
            }
            return;
        }
        if (!isGrounded)
        {
            AirTime += Time.deltaTime;
            botRigid.AddForce(transform.forward * leapingVelocity);
            botRigid.AddForce(-Vector3.up * fallingVelocity * AirTime);
        }
        if (Physics.SphereCast(rayCastOrigin, 0.5f, -Vector3.up, out hit, groundLayer))
        {
            AirTime = 0;
            jumpTimer = 0;
            isGrounded = true;
            isInteracting = false;
        }
        else
        {
            isGrounded = false;
        }
    }

    public void HandleJump()
    {
        if (isGrounded)
        {
            float jumpVelocity = Mathf.Sqrt(-2 * gravityIntensity * jumpHeight);
            Vector3 playerVelocity = moveDirection;
            playerVelocity.y = jumpVelocity;
            botRigid.velocity = playerVelocity;
            isJumping = true;
            isGrounded = false;
        }
    }

    public void HandleDash()
    {
        // Creates a vector that moves the player forward at a straight speed. Gradually slows down.
        Vector3 dashDirection = transform.forward;
        dashDirection *= movementSpeed * dashPower;
        botRigid.velocity = dashDirection;
        // Update timers, and stop dashing when the dashTimer runs out.
        dashTimer -= Time.deltaTime;
        dashPower = Mathf.Max(dashPower - 2 * Time.deltaTime, 0.5f);
        if (dashTimer <= 0)
        {
            dashTimer = 0;
            dashPower = 3;
            dashCoolDown = 3;
            isDashing = false;
        }
    }

    public void HandleCoolDown()
    {
        if (dashCoolDown > 0)
        {
            dashCoolDown = Mathf.Max(dashCoolDown - Time.deltaTime, 0);
        }
    }
}
