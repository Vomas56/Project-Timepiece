using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BotLocomotion : MonoBehaviour
{
    BotInput botInput;
    // Variables needed for moving.
    Vector3 moveDirection;
    Transform cameraObject;
    Rigidbody botRigid;
    // Constants for ground movement.
    public float movementSpeed = 5;
    public float rotationSpeed = 10;

    private void Awake()
    {
        botInput = GetComponent<BotInput>();
        botRigid = GetComponent<Rigidbody>();
        cameraObject = Camera.main.transform;
    }

    public void HandleAllMovement()
    {
        // All movement inputs are handled from here.
        HandleMovement();
        HandleRotation();
    }

    private void HandleMovement()
    {
        // Creates a vector that moves the bot based on camera orientation.
        moveDirection = cameraObject.forward * botInput.verticalInput;
        moveDirection += cameraObject.right * botInput.horizontalInput;
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
        targetDirection = cameraObject.forward * botInput.verticalInput;
        targetDirection += cameraObject.right * botInput.horizontalInput;
        targetDirection.Normalize();
        targetDirection.y = 0;
        // Keeps rotation if the character isn't present.
        if (targetDirection == Vector3.zero)
            targetDirection = transform.forward;
        // Uses quaternions to make rotation more gradual and less snappy.
        Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
        Quaternion botRotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        transform.rotation = botRotation;
    }
}
