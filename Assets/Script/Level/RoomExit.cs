using System;
using UnityEngine;

// The way out of a room: put it just behind the room's exit door.
// The door blocks it until the room is cleared. When the player walks in,
// ChapterManager fades out and loads the next room (or shows "STAGE CLEAR" after the last one).
// Needs a Box Collider (it is turned into a trigger automatically).
// Don't put it under "Walls", or arrows would break on it.
[RequireComponent(typeof(BoxCollider))]
public class RoomExit : MonoBehaviour
{
    // ChapterManager listens to this
    public static event Action Reached;

    private bool reached;

    void Awake()
    {
        GetComponent<BoxCollider>().isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (reached) return;

        PlayerHealth player = other.GetComponentInParent<PlayerHealth>();
        if (player == null || player.IsDead) return;

        reached = true;
        Reached?.Invoke();
    }
}
