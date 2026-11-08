using UnityEngine;

// Tells the Animator what the player is doing, so it can pick the right animation:
//   "Speed"      (float)   0 = Idle, 0.5 = Walk, 1 = Run (Blend Tree in the Locomotion state), smoothed
//   "Moving"     (bool)    true while the joystick is used, NOT smoothed: cancels the bow animation
//   "Shoot"      (trigger) plays the bow animation once, every time PlayerShooter starts drawing
//   "ShootSpeed" (float)   speeds the bow animation up, so it always fits between two shots
//   "Die"        (trigger) plays the death animation (PlayerHealth asks for it)
[RequireComponent(typeof(PlayerMovement))]
public class PlayerAnimation : MonoBehaviour
{
    // The Animator on the character model. If left empty, it is found automatically.
    public Animator animator;

    // How long (in seconds) it takes to blend between idle and walk. Bigger = smoother but slower.
    public float blendTime = 0.1f;

    // Drag the shoot animation clip here. We only need it to know how long it is.
    public AnimationClip shootClip;

    // When in the shoot clip her hand lets go of the arrow: 0 = at the start, 1 = at the very end.
    // Find it in the clip preview: release frame / last frame (e.g. frame 22 of 31 = 0.7).
    [Range(0f, 1f)] public float releasePoint = 1f;

    // Turning a parameter name into a number once is faster than using the string every frame
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int MovingHash = Animator.StringToHash("Moving");
    private static readonly int ShootHash = Animator.StringToHash("Shoot");
    private static readonly int ShootSpeedHash = Animator.StringToHash("ShootSpeed");
    private static readonly int DieHash = Animator.StringToHash("Die");

    private PlayerMovement movement;
    private PlayerShooter shooter;

    // Which parameters the Animator Controller really has, checked once in Awake.
    // (Setting a parameter that doesn't exist only gives warnings, so we skip those.)
    private bool hasMoving;
    private bool hasShoot;
    private bool hasShootSpeed;
    private bool hasDie;

    void Awake()
    {
        movement = GetComponent<PlayerMovement>();
        shooter = GetComponent<PlayerShooter>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

        if (animator != null)
        {
            // Our script moves her with the CharacterController, so the animation must not move her too
            animator.applyRootMotion = false;

            hasMoving = HasParameter(MovingHash);
            hasShoot = HasParameter(ShootHash);
            hasShootSpeed = HasParameter(ShootSpeedHash);
            hasDie = HasParameter(DieHash);
        }
    }

    // Start listening to the shooter while we are switched on, stop when switched off
    // (the same OnEnable/OnDisable pair as Enemy.All, so we never listen twice)
    void OnEnable()
    {
        if (shooter != null) shooter.StartedDrawing += OnStartedDrawing;
    }

    void OnDisable()
    {
        if (shooter != null) shooter.StartedDrawing -= OnStartedDrawing;
    }

    // How fast the shoot clip plays: the whole clip fits into one shot.
    // A 1 second clip at 2 arrows per second plays at speed 2.
    // Never slower than normal (Max with 1), so a slow bow doesn't look like slow motion.
    float ShootSpeed(float fireRate)
    {
        return Mathf.Max(shootClip.length * fireRate, 1f);
    }

    // Seconds from the start of the draw until her hand lets go (PlayerShooter waits this long).
    // Example: 1 s clip, release at 0.7, speed 2 -> 0.7 / 2 = 0.35 seconds.
    public float ReleaseDelay(float fireRate)
    {
        if (shootClip == null || !hasShoot) return 0f; // no animation: shoot right away

        return shootClip.length * releasePoint / ShootSpeed(fireRate);
    }

    void Update()
    {
        if (animator == null) return;

        // The damp version slides the value smoothly toward the target instead of jumping
        animator.SetFloat(SpeedHash, movement.MoveAmount, blendTime, Time.deltaTime);

        // Not smoothed on purpose: PlayerShooter cancels the draw at the very same moment,
        // so the animation and the arrow always agree
        if (hasMoving) animator.SetBool(MovingHash, movement.IsMoving);
    }

    void OnStartedDrawing()
    {
        if (!hasShoot) return; // Animator not set up yet: just shoot without animation

        if (shootClip != null && hasShootSpeed) animator.SetFloat(ShootSpeedHash, ShootSpeed(shooter.fireRate));

        animator.SetTrigger(ShootHash);
    }

    // Plays the death animation. Returns false if the Animator has no "Die" trigger,
    // so PlayerHealth knows it has to make her fall over by code instead.
    public bool PlayDeath()
    {
        if (!hasDie) return false;

        animator.SetTrigger(DieHash);
        return true;
    }

    // Checks that the Animator Controller really has this parameter
    bool HasParameter(int hash)
    {
        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.nameHash == hash) return true;
        }
        return false;
    }
}
