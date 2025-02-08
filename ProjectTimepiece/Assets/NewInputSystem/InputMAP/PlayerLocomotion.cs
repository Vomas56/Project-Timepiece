using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class PlayerLocomotion : MonoBehaviour
{
    InputManager inputManager;
    public float m_velocity; // The magnitude of the player's velocity.
    // These variables allow the player to move.
    Vector3 moveDirection; // What direction is the player facing?
    Transform cameraObject;
    Rigidbody botRigid;
    // These constants dictate ground movement.
    private float movementSpeed = 7;
    private float rotationSpeed = 15;
    private float dashTimer = 0;
    private float dashCoolDown = 0;
    private float dashPower = 3;
    // Constants for jumping.
    public float AirTime = 0f;
    private float jumpTimer = 0f;
    private float rayCastHeightOffset = 0.5f;
    private float leapingVelocity = 3;
    private float fallSpeed = 33;
    private float jumpHeight = 3;
    private float gravityIntensity = -15;
    public LayerMask groundLayer;
    // Booleans that denote the state the player is in.
    private bool isInteracting = false;
    private bool isDashing = false;
    private bool isJumping = false;
    public bool isGrounded = false;

    private void Awake()
    {
        inputManager = GetComponent<InputManager>();
        botRigid = GetComponent<Rigidbody>();
        cameraObject = Camera.main.transform;
    }

    public void HandleAllMovement()
    {  // This function calls movement inputs frame by frame.
        HandleFallingAndLanding();
        HandleMovement();
        HandleRotation();
        HandleCoolDown();
    }

    private void HandleMovement()
    {
        if (botRigid.velocity.y != 0)
        {
            return;
        }
        else if (isDashing)
        {   // Dashing causes the player to move forward at a gradually decreasing speed.
            moveDirection = transform.forward;
            moveDirection *= movementSpeed * dashPower;
        }
        else
        {   // A vector is created based on keyboard inputs and the camera orientation.
            moveDirection = cameraObject.forward * inputManager.getVertical();
            moveDirection += cameraObject.right * inputManager.getHorizontal();
            moveDirection.Normalize();
            moveDirection.y = 0;
            moveDirection = moveDirection * movementSpeed;
        }
        // The velocity is translated to the rigid body, and its magnitude gets recorded.
        botRigid.velocity = moveDirection;
        m_velocity = moveDirection.x * moveDirection.x * +moveDirection.z * moveDirection.z;
    }

    private void HandleRotation()
    {
        if (botRigid.velocity.y != 0 || isDashing) return; // The player will not rotate when dashing.
        Vector3 targetDirection = Vector3.zero;
        // Creates a vector that rotates a character to match their movement direction.
        targetDirection = cameraObject.forward * inputManager.getVertical();
        targetDirection += cameraObject.right * inputManager.getHorizontal();
        targetDirection.Normalize();
        targetDirection.y = 0;
        // If the character isn't present, this if statement keeps their looking direction static.
        if (targetDirection == Vector3.zero)
        {
            targetDirection = transform.forward;
        }
        // Quaternions are used to make the rotation more gradual and less snappy.
        Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
        Quaternion playerRotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        // Finally, the transform is given the resulting vector.
        transform.rotation = playerRotation;
    }

    public void HandleFallingAndLanding()
    {
        if (isDashing) return; // The player's height does not change while dashing.
        // Creates a raycast to determine if the player is in contact with a surface.
        RaycastHit hit;
        Vector3 rayCastOrigin = transform.position;
        rayCastOrigin.y += rayCastHeightOffset;
        if (isJumping)
        {   // The power of the jump is increased by holding the button.
            if (inputManager.getJump() && jumpTimer < 0.25)
            {
                jumpTimer += Time.deltaTime;
                botRigid.AddForce(transform.forward * leapingVelocity / 100);
            }
            else if (botRigid.velocity.y <= 0)
            {
                isJumping = false;
            }
        }
        if (!isGrounded)
        {   // These functions continue to apply downward velocity on the player the longer they stay in the air.
            /* if (!isInteracting) 
            {
                // Maybe insert a falling animation if the character isn't already interacting?
            } */
            AirTime += Time.deltaTime;
            botRigid.AddForce(transform.forward * leapingVelocity);
            botRigid.AddForce(-Vector3.up * fallSpeed * AirTime); // This could see a maximum in the future.
        }
        if (Physics.SphereCast(rayCastOrigin, 0.2f, -Vector3.up, out hit, groundLayer))
        {   // The raycast should hit once the player lands, grounding them.
            AirTime = 0;
            jumpTimer = 0;
            isGrounded = true;
        }
        else
        {
            isGrounded = false;
        }
    }

    public bool HandleJump()
    {   // This function make the player jump up if they're on the ground and not doing anything.
        if (isGrounded && !isInteracting && dashTimer == 0)
        {
            float jumpVelocity = Mathf.Sqrt(-2 * gravityIntensity * jumpHeight);
            Vector3 playerVelocity = moveDirection;
            playerVelocity.y = jumpVelocity;
            botRigid.velocity = playerVelocity;
            isInteracting = true;
            isJumping = true;
            isGrounded = false;
            return true;
        }
        else
        {
            return false;
        }
    }

    public bool HandleDash()
    {
        if (dashCoolDown == 0)
        {   // Starts the character's dashing ability if it's not on cooldown.
            isInteracting = true;
            isDashing = true;
            dashTimer = 1;
            return true;
        }
        return false;
    }

    public void HandleCoolDown()
    {
        if (isDashing)
        {   // Gradually reduces both the amount of time spent dashing and the dashing speed.
            dashTimer -= Time.deltaTime;
            dashPower = Mathf.Max(dashPower - 2 * Time.deltaTime, 0.5f);
            if (dashTimer <= 0)
            {   // Starts a cooldown between dashes.
                dashTimer = 0;
                dashPower = 3;
                dashCoolDown = 3;
                isDashing = false;
            }
        }
        else if (dashCoolDown > 0)
        {   // Updates the dash cooldown to a minumum of 0.
            dashCoolDown = Mathf.Max(dashCoolDown - Time.deltaTime, 0);
        }
        isInteracting = !(!isDashing && !isJumping);
    }
}
