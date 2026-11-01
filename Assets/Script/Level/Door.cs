using UnityEngine;

// A door in a gap of a wall. It sinks into the floor when it opens and rises back up when it closes.
// - Exit door: closed at the start, Room calls Open() when all its enemies are dead.
// - Entrance door: tick "Starts Open", Room calls Close() behind the player when they walk in.
// Use a Cube scaled to fill the gap in the wall, and put it under the "Walls" object so arrows stop at it.
public class Door : MonoBehaviour
{
    public float moveSpeed = 3f;     // meters per second
    public bool startsOpen = false;

    public bool IsOpen { get; private set; }

    private float closedY; // where it stands when closed (where you placed it in the editor)
    private float openY;   // fully under the floor

    void Awake()
    {
        closedY = transform.position.y;

        // Sink by the door's own height, so it ends up fully under the floor
        Renderer doorRenderer = GetComponentInChildren<Renderer>();
        float height = doorRenderer != null ? doorRenderer.bounds.size.y : 3f;
        openY = closedY - height - 0.1f;

        if (startsOpen)
        {
            IsOpen = true;
            SetBlocking(false);
            SetY(openY);
            gameObject.SetActive(false);
        }
    }

    public void Open()
    {
        if (IsOpen) return;
        IsOpen = true;

        // Stop blocking right away, so the player can walk through while it is still sinking
        SetBlocking(false);
    }

    public void Close()
    {
        if (!IsOpen) return;
        IsOpen = false;

        // Block right away, so nobody slips through while it is still rising
        gameObject.SetActive(true);
        SetBlocking(true);
    }

    void Update()
    {
        float targetY = IsOpen ? openY : closedY;
        SetY(Mathf.MoveTowards(transform.position.y, targetY, moveSpeed * Time.deltaTime));

        // Fully down: hide it (it can't be seen under the floor anyway)
        if (IsOpen && transform.position.y <= openY) gameObject.SetActive(false);
    }

    void SetBlocking(bool blocking)
    {
        foreach (Collider col in GetComponentsInChildren<Collider>()) col.enabled = blocking;
    }

    void SetY(float y)
    {
        Vector3 position = transform.position;
        position.y = y;
        transform.position = position;
    }
}
