using Unity.AI.Navigation;
using UnityEngine;

// One room of a chapter, saved as a prefab. ChapterManager spawns it, puts the player on
// "playerStart" and calls Activate() once the screen has faded in.
// When all its enemies are dead the exit door opens, and the player walks into the RoomExit behind it.
//
// The room bakes its own NavMesh when it is spawned (so you never have to press "Bake"),
// which is why the room object also needs a NavMeshSurface component.
[RequireComponent(typeof(NavMeshSurface))]
public class Room : MonoBehaviour
{
    public Door exitDoor;         // opens when the room is cleared
    public Transform playerStart; // where the player appears (an empty object near the bottom wall)

    public bool IsCleared { get; private set; }

    private Enemy[] roomEnemies;
    private bool isActive;

    void Awake()
    {
        // Every enemy anywhere inside the room belongs to it (true = also find inactive ones).
        // Put them all to sleep until the player can see the room.
        roomEnemies = GetComponentsInChildren<Enemy>(true);
        foreach (Enemy enemy in roomEnemies)
        {
            enemy.gameObject.SetActive(false);
            WakeUpParents(enemy.transform);
        }

        BuildNavMesh();
    }

    // Called by ChapterManager when the player is in place and can see the room
    public void Activate()
    {
        if (isActive) return;
        isActive = true;

        foreach (Enemy enemy in roomEnemies)
        {
            enemy.gameObject.SetActive(true);
        }
    }

    void Update()
    {
        if (!isActive || IsCleared) return;

        // A destroyed enemy counts as null, so: any enemy not null = someone is still alive
        foreach (Enemy enemy in roomEnemies)
        {
            if (enemy != null) return;
        }

        // (A room with no enemies at all is cleared straight away)
        IsCleared = true;
        // The "room cleared" jingle only after a fight (not in the start room or rest rooms).
        // The last room is skipped too: the chapter clear tune plays there instead.
        if (roomEnemies.Length > 0 && !ChapterManager.IsLastRoom) SoundManager.Play(Sound.RoomClear);
        if (exitDoor != null) exitDoor.Open();
        Pickup.CollectAll(); // the reward: all XP and coins on the floor fly to the player
    }

    // In the prefab the "Enemies" object is unticked, so the enemies don't wake up before
    // the NavMesh exists (that gives "Failed to create agent" warnings).
    // The enemies themselves are asleep now, so switch their parent objects back on.
    void WakeUpParents(Transform enemy)
    {
        Transform parent = enemy.parent;
        while (parent != null && parent != transform)
        {
            parent.gameObject.SetActive(true);
            parent = parent.parent;
        }
    }

    void BuildNavMesh()
    {
        NavMeshSurface surface = GetComponent<NavMeshSurface>();
        if (surface == null)
        {
            Debug.LogError(name + " needs a NavMeshSurface component, or its enemies can't walk.");
            return;
        }

        // Only look at this room's own children, and use their colliders as the shape of the level.
        // The enemies are inactive right now, so they don't punch holes in the NavMesh.
        surface.collectObjects = CollectObjects.Children;
        surface.useGeometry = UnityEngine.AI.NavMeshCollectGeometry.PhysicsColliders;
        surface.BuildNavMesh();
    }

    // Shows the player start in the Scene view when the room is selected
    void OnDrawGizmosSelected()
    {
        if (playerStart == null) return;

        Gizmos.color = new Color(0.3f, 1f, 0.5f);
        Gizmos.DrawWireSphere(playerStart.position, 0.5f);
    }
}
