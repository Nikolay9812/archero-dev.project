using UnityEngine;

// Plays the idle and walk animations by telling the Animator how fast she is moving.
// The Animator Controller uses a "Speed" float: 0 = Idle, 1 = Walk (set up in a Blend Tree).
[RequireComponent(typeof(PlayerMovement))]
public class PlayerAnimation : MonoBehaviour
{
    // The Animator on the character model. If left empty, it is found automatically.
    public Animator animator;

    // How long (in seconds) it takes to blend between idle and walk. Bigger = smoother but slower.
    public float blendTime = 0.1f;

    // Turning a parameter name into a number once is faster than using the string every frame
    private static readonly int SpeedHash = Animator.StringToHash("Speed");

    private PlayerMovement movement;

    void Awake()
    {
        movement = GetComponent<PlayerMovement>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

        // Our script moves her with the CharacterController, so the animation must not move her too
        if (animator != null) animator.applyRootMotion = false;
    }

    void Update()
    {
        if (animator == null) return;

        // The damp version slides the value smoothly toward the target instead of jumping
        animator.SetFloat(SpeedHash, movement.MoveAmount, blendTime, Time.deltaTime);
    }
}
