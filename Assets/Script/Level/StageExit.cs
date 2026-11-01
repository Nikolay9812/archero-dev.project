using System;
using UnityEngine;

// The finish line: put it just behind the last room's exit door.
// When the player walks into it the stage is cleared, and GameOverScreen shows "STAGE CLEAR".
// Needs a Box Collider (it is turned into a trigger automatically).
// Don't put it under "Walls", or arrows would break on it.
[RequireComponent(typeof(BoxCollider))]
public class StageExit : MonoBehaviour
{
    // GameOverScreen listens to this, the same way it listens to PlayerHealth.Died
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
