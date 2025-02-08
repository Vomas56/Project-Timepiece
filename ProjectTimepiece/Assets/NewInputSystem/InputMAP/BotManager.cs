using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BotManager : MonoBehaviour
{
    BotInput botInput;
    BotLocomotion botLoco;

    private void Awake()
    {
        // Call everything so we can run it from here.
        botInput = GetComponent<BotInput>();
        botLoco = GetComponent<BotLocomotion>();
    }

    private void Update()
    {
        botInput.HandleAllInputs();
    }

    private void FixedUpdate()
    {
        botLoco.HandleAllMovement();
    }
}
