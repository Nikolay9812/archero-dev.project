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
    public static event Action<int, int> RoomStarted; // (room number starting at 1, number of rooms)
    public static event Action ChapterCleared;

    private Room currentRoom;
    private int roomIndex = -1;    // -1 = no room loaded yet
    private bool changingRoom;     // true while fading, so the exit can't trigger twice

    void OnEnable()
    {
        RoomExit.Reached += OnRoomExitReached;
    }

    void OnDisable()
    {
        RoomExit.Reached -= OnRoomExitReached;
    }

    void Start()
    {
        if (chapter == null || chapter.rooms.Count == 0)
        {
            Debug.LogError("ChapterManager needs a Chapter with at least one room.");
            return;
        }

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
        currentRoom = Instantiate(chapter.rooms[index], roomPosition, Quaternion.identity);
        currentRoom.name = chapter.rooms[index].name; // without "(Clone)"

        MovePlayerToStart();
        RoomStarted?.Invoke(roomIndex + 1, chapter.rooms.Count);

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
