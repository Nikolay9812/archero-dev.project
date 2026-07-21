using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    // Movement tining (editable in Inspector)
    public float speed = 5.0f;
    public float turnSpeed = 100f;
    // Input System action exposed in Inspector for binding (WASD/Arrow keys)
    public InputAction moveAction;
    // Current input value (x = left/right, y = forward/back), kept private for internal use
    private Vector2 moveInput;

    // Start is called once before the first execution of Update after the MonoBehaviour is created

    void Start()
    {
        // Enable the moveAcrtion so it starts reading input
        moveAction.Enable();
    }

    // Update is called once per frame
    void Update()
    {
        // Read the 2D vector from the moveAction (x: horizontal, y: vertical)
        moveInput = moveAction.ReadValue<Vector2>();

        // Move forward/back along local Z using y component
        transform.Translate(Vector3.forward * Time.deltaTime * speed * moveInput.y);

        // Rotate around local Y (yaw) using the x component
        transform.Translate(Vector3.right * Time.deltaTime * speed * moveInput.x);
    }
}