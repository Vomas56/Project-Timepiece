using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    public PlayerControls playerControls;
    public GameObject botSphere;
    PlayerLocomotion playerLocomotion;

    public Vector2 movementInput;
    public float verticalInput;
    public float horizontalInput;
    // Booleans that determine jump input.
    public bool jump_input;
    public bool jump_held;
    
    public bool dash_input;
    public bool swap_input;

    private void Awake()
    {
        playerLocomotion = GetComponent<PlayerLocomotion>();
    }

    private void OnEnable()
    {
        if (playerControls == null)
        {
            playerControls = new PlayerControls();

            playerControls.PlayerMovement.Move.performed += i => movementInput = i.ReadValue<Vector2>();
            playerControls.PlayerMovement.Jump.performed += i => jump_input = true;
            playerControls.PlayerMovement.Dash.performed += i => dash_input = true;
            playerControls.PlayerMovement.Swap.performed += i => swap_input = true;
        }

        playerControls.Enable();
    }

    private void OnDisable()
    {
        playerControls.Disable();
    }

    public void HandleAllInputs()
    {
        HandleMovementInput();
        HandleJumpInput();
        HandleDashInput();
        HandleSwapInput();
    }
    
    private void HandleMovementInput()
    {
        verticalInput = movementInput.y;
        horizontalInput = movementInput.x;
    }

    private void HandleJumpInput()
    {
        if (!jump_input)
        {
            jump_held = false;
        }

        if (jump_input)
        {
            jump_input = false;
            if (!playerLocomotion.isInteracting && playerLocomotion.dashTimer == 0)
            {
                jump_held = true;
                playerLocomotion.isInteracting = true;
                playerLocomotion.HandleJump();
            }
        }
    }
    private void HandleDashInput()
    {
        if (dash_input)
        {
            dash_input = false;
            if (playerLocomotion.dashCoolDown == 0)
            {
                playerLocomotion.dashTimer = 1;
                playerLocomotion.isDashing = true;
            }
        }
    }
    private void HandleSwapInput()
    {
        if (swap_input)
        {
            swap_input = false;
            playerControls.Disable();
            var privateBot = Instantiate(botSphere, transform.position + Vector3.up * 2f, Quaternion.identity);
            /* If only there was a way to get the camera to follow the bot...
            GameObject.FindWithTag("CameraTwo").GetComponent<CinemachineFreeLook>().Follow = privateBot;
            */
        }
    }
}
