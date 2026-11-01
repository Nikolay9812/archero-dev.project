using UnityEngine;

// A door that blocks the way out of a room. Room calls Open() when all its enemies are dead:
// the door stops blocking and sinks into the floor.
// Use a Cube scaled to fill the gap in the wall, and put it under the "Walls" object so arrows stop at it.
public class Door : MonoBehaviour
{
    public float sinkSpeed = 3f; // meters per second

    public bool IsOpen { get; private set; }

    private float targetY;

    public void Open()
    {
        if (IsOpen) return;
        IsOpen = true;

        // Stop blocking right away, so the player can walk through while it is still sinking
        foreach (Collider col in GetComponentsInChildren<Collider>()) col.enabled = false;

        // Sink by the door's own height, so it ends up fully under the floor
        Renderer doorRenderer = GetComponentInChildren<Renderer>();
        float height = doorRenderer != null ? doorRenderer.bounds.size.y : 3f;
        targetY = transform.position.y - height - 0.1f;
    }

    void Update()
    {
        if (!IsOpen) return;

        Vector3 position = transform.position;
        position.y = Mathf.MoveTowards(position.y, targetY, sinkSpeed * Time.deltaTime);
        transform.position = position;

        // Fully down: hide it (it can't be seen under the floor anyway)
        if (position.y <= targetY) gameObject.SetActive(false);
    }
}
