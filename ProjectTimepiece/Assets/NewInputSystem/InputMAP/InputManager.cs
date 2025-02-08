using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    public PlayerControls playerControls;
    public GameObject botSphere;
    PlayerLocomotion playerLocomotion;

    [SerializeField] private CinemachineFreeLook camCam;

    private Vector2 movementInput;
    private float verticalInput;
    private float horizontalInput;
    // Booleans that determine jump input.
    private bool jump_input;
    private bool jump_held;

    private bool dash_input;
    private bool swap_input;

    private void Awake()
    {
        playerLocomotion = GetComponent<PlayerLocomotion>();
    }

    public CinemachineFreeLook getCamCam()
    {
        return camCam;
    }

    public bool getJump()
    {
        return jump_held;
    }

    public float getVertical()
    {
        return verticalInput;
    }

    public float getHorizontal()
    {
        return horizontalInput;
    }

    private void OnEnable()
    {
        if (playerControls == null)
        {
            playerControls = new PlayerControls();
            // These activate the inputs :T
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
        if (jump_input)
        {
            jump_input = false;
            if (playerLocomotion.HandleJump())
            {
                jump_held = true;
            }
        }
        else
        {
            jump_held = false;
        }
    }
    private void HandleDashInput()
    {
        if (dash_input)
        {
            dash_input = false;
            playerLocomotion.HandleDash();
        }
    }
    private void HandleSwapInput()
    {
        if (swap_input)
        {
            swap_input = false;
            playerControls.Disable();
            var privateBot = Instantiate(botSphere, transform.position + Vector3.up * 2f, Quaternion.identity);
            // Changes camera to look back at the bot.
            camCam.Follow = privateBot.transform;
            camCam.LookAt = privateBot.transform;
        }
    }
}
