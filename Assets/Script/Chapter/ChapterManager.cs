using System;
using System.Collections;
using UnityEngine;

// Plays a chapter one room at a time, Archero-style:
// fade to black -> remove the old room -> spawn the next one -> put the player at its start -> fade in.
// After the last room it fires ChapterCleared, and GameOverScreen shows "STAGE CLEAR".
// Put it on an empty GameObject and drag a ChapterData asset into "Chapter".
public class ChapterManager : MonoBehaviour
{
    public ChapterData chapter;
    public Vector3 roomPosition = Vector3.zero; // where every room is spawned
    public float fallLimit = -5f; // safety net: below this height the player is put back at the room's start

    // RoomCounter listens to RoomStarted, GameOverScreen to ChapterCleared
    public static event Action<int, int> RoomStarted; // (room number, 0 in the start room; number of rooms)
    public static event Action ChapterCleared;

    // True while the player is in the chapter's final room. Enemy reads this.
    public static bool IsLastRoom { get; private set; }

    // The chapter's difficulty, for Enemy and PlayerHealth. 1 when no chapter is playing (e.g. a test scene).
    // "?." gives null when there is no chapter, and "?? 1f" turns that null into 1.
    private static ChapterData current;
    public static float EnemyHealthMultiplier => current?.enemyHealthMultiplier ?? 1f;
    public static float EnemyDamageMultiplier => current?.enemyDamageMultiplier ?? 1f;
    public static float CoinMultiplier => current?.coinMultiplier ?? 1f;

    private Room currentRoom;
    private int roomIndex = -1;    // -1 = no room loaded yet
    private bool changingRoom;     // true while fading, so the exit can't trigger twice

    void OnEnable()
    {
        RoomExit.Reached += OnRoomExitReached;
        current = chapter; // before any room (and its enemies) is spawned
    }

    void OnDisable()
    {
        RoomExit.Reached -= OnRoomExitReached;
        current = null;
    }

    void Start()
    {
        if (chapter == null || chapter.rooms.Count == 0)
        {
            Debug.LogError("ChapterManager needs a Chapter with at least one room.");
            return;
        }

        Wallet.StartRun();
        PlayerProfile.StartRun();
        Inventory.StartRun();
        Artifacts.StartRun();
        StartCoroutine(GoToRoom(0));
    }

    void Update()
    {
        // If the player somehow gets off the floor (a gap we missed), catch her instead of falling forever
        PlayerHealth player = PlayerHealth.Instance;
        if (player != null && currentRoom != null && player.transform.position.y < fallLimit)
        {
            MovePlayerToStart();
        }
    }

    void OnRoomExitReached()
    {
        if (changingRoom) return;

        bool wasLastRoom = roomIndex + 1 >= chapter.rooms.Count;
        if (wasLastRoom)
        {
            changingRoom = true; // so the reward can never be given twice
            PlayerProfile.AddXp(chapter.profileXpReward);
            Wallet.AddGems(chapter.gemReward);
            for (int i = 0; i < chapter.gearReward; i++) Inventory.AddRandomGear(Rarity.Common);
            Artifacts.AddWildShards(chapter.wildShardReward);
            ChapterCleared?.Invoke();
            return;
        }

        StartCoroutine(GoToRoom(roomIndex + 1));
    }

    // A coroutine: a method that can pause ("yield") and carry on in a later frame.
    // Each "yield return" waits for the fade to finish before going to the next line.
    IEnumerator GoToRoom(int index)
    {
        changingRoom = true;
        ScreenFader fader = ScreenFader.Instance;

        if (fader != null) yield return fader.FadeOut();

        // Remove the old room. Destroy only happens at the end of the frame,
        // so hide it right now so its walls don't get in the way of the new room.
        if (currentRoom != null)
        {
            currentRoom.gameObject.SetActive(false);
            Destroy(currentRoom.gameObject);
        }

        roomIndex = index;
        IsLastRoom = index == chapter.rooms.Count - 1;
        currentRoom = Instantiate(chapter.rooms[index], roomPosition, Quaternion.identity);
        currentRoom.name = chapter.rooms[index].name; // without "(Clone)"

        MovePlayerToStart();

        // The Room Heal talent. At full HP (like in the first room) this does nothing.
        if (PlayerHealth.Instance != null) PlayerHealth.Instance.Heal(Upgrades.RoomHeal);

        // The start room doesn't count, so it is number 0 and the first fight is "1 / 10"
        int skippedRooms = chapter.firstRoomIsStartRoom ? 1 : 0;
        RoomStarted?.Invoke(roomIndex + 1 - skippedRooms, chapter.rooms.Count - skippedRooms);

        if (fader != null) yield return fader.FadeIn();

        currentRoom.Activate(); // enemies wake up only once the player can see them
        changingRoom = false;
    }

    void MovePlayerToStart()
    {
        PlayerHealth player = PlayerHealth.Instance;
        if (player == null) return;

        Transform start = currentRoom.playerStart;
        if (start == null)
        {
            Debug.LogWarning(currentRoom.name + " has no Player Start, so the player is put in the middle of the room.");
            start = currentRoom.transform;
        }

        player.GetComponent<PlayerMovement>().Teleport(start.position, start.rotation);

        // Jump the camera too, instead of letting it slide across the whole level
        CameraFollow cameraFollow = FindAnyObjectByType<CameraFollow>();
        if (cameraFollow != null) cameraFollow.SnapToTarget();
    }
}
