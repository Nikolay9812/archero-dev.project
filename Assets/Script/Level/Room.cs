using UnityEngine;

// One room of the level. Its enemies sleep until the player walks in.
// When they are all dead the exit door opens, and the player walks on to the next room.
// Needs a Box Collider that covers the room's floor (it is turned into a trigger automatically).
[RequireComponent(typeof(BoxCollider))]
public class Room : MonoBehaviour
{
    public GameObject enemies; // parent object holding this room's enemies
    public Door exitDoor;      // opens when the room is cleared
    public Door entranceDoor;  // optional: closes behind the player when they walk in (tick "Starts Open" on it)

    public bool IsCleared { get; private set; }

    private BoxCollider area;
    private Enemy[] roomEnemies;
    private bool isActive;

    void Awake()
    {
        area = GetComponent<BoxCollider>();
        area.isTrigger = true;

        // Remember who lives here (true = also find inactive ones), then put them to sleep
        if (enemies != null)
        {
            roomEnemies = enemies.GetComponentsInChildren<Enemy>(true);
            enemies.SetActive(false);
        }
        else
        {
            roomEnemies = new Enemy[0];
        }
    }

    void Start()
    {
        // The player may already be standing in this room when the game starts (the first room)
        PlayerHealth player = PlayerHealth.Instance;
        if (player != null && IsInside(player.transform.position)) Activate();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<PlayerHealth>() != null) Activate();
    }

    void Update()
    {
        if (!isActive || IsCleared) return;

        // A destroyed enemy counts as null, so: any enemy not null = someone is still alive
        foreach (Enemy enemy in roomEnemies)
        {
            if (enemy != null) return;
        }

        IsCleared = true;
        if (exitDoor != null) exitDoor.Open();
    }

    void Activate()
    {
        if (isActive) return;
        isActive = true;

        if (enemies != null) enemies.SetActive(true);
        if (entranceDoor != null) entranceDoor.Close(); // no going back
    }

    // Only checks the floor area (x and z), so the player's height doesn't matter
    bool IsInside(Vector3 point)
    {
        Bounds bounds = area.bounds;
        point.y = bounds.center.y;
        return bounds.Contains(point);
    }

    // Shows the room area in the Scene view when selected
    void OnDrawGizmosSelected()
    {
        BoxCollider box = GetComponent<BoxCollider>();
        if (box == null) return;

        Gizmos.color = new Color(0.3f, 1f, 0.5f, 0.25f);
        Gizmos.DrawCube(box.bounds.center, box.bounds.size);
    }
}
