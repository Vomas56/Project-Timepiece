using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;

public class BotInput : MonoBehaviour
{
    PlayerControls playerControls;
    PlayerControls botControls;
    BotLocomotion botLocomotion;
    // The bot only has 4 needed inputs.

    [SerializeField] private CinemachineFreeLook camCam;

    private Vector2 movementInput;
    public float verticalInput;
    public float horizontalInput;
    private bool swap_input;

    private void Awake()
    {
        botLocomotion = GetComponent<BotLocomotion>();
        playerControls = GameObject.FindWithTag("Player").GetComponent<InputManager>().playerControls;
        camCam = GameObject.FindWithTag("Player").GetComponent<InputManager>().getCamCam();
    }

    private void OnEnable()
    {

        if (botControls == null)
        {
            botControls = new PlayerControls();

            botControls.BotMovement.Move.performed += i => movementInput = i.ReadValue<Vector2>();
            botControls.BotMovement.Swap.performed += i => swap_input = true;
        }

        botControls.Enable();
    }

    private void OnDisable()
    {
        botControls.Disable();
    }

    public void HandleAllInputs()
    {
        HandleMovementInput();
        HandleSwapInput();
    }

    private void HandleMovementInput()
    {
        verticalInput = movementInput.y;
        horizontalInput = movementInput.x;
    }

    private void HandleSwapInput()
    {
        if (swap_input)
        {
            botControls.Disable();
            playerControls.Enable();
            camCam.Follow = GameObject.FindWithTag("Player").transform;
            camCam.LookAt = GameObject.FindWithTag("Player").transform;
            Destroy(gameObject);
            return;
        }
    }
}
