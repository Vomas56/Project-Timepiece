using UnityEngine;

public class NewMonoBehaviourScript : MonoBehaviour
{
    // Adjustable speed parameter accessible in the Inspector
    public float moveSpeed = 5f;

    void Update()
    {
        // Gather input from Horizontal (A/D, Left/Right) and Vertical (W/S, Up/Down) axes
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        // Combine inputs into a movement direction vector
        Vector3 moveDirection = new Vector3(moveX, 0f, moveZ);

        // Move the object relative to world space over time
        transform.position += moveDirection * moveSpeed * Time.deltaTime;
    }
}
