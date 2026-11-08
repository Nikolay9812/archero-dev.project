using UnityEngine;

// Archero-style camera: high, fixed angle that follows the player smoothly
// but never rotates (rotating the camera makes the controls feel confusing).
// [ExecuteAlways] also runs it in the editor, so the Game view shows the real angle without pressing Play.
// It can also shake: call CameraFollow.Shake(...) from anywhere (player hit, enemy death...).
[ExecuteAlways]
public class CameraFollow : MonoBehaviour
{
    public Transform target; // the player
    public Vector3 offset = new Vector3(0f, 14f, -8f);
    public float smoothTime = 0.15f; // lower = snappier

    // How many meters of the level should fit across the screen (the room is 12 m wide + walls).
    // Phones have different shapes, so the camera zooms to always show this width.
    public float visibleWidth = 14f;

    // Untick to switch every shake off (some players don't like it, a nice option for a settings menu later)
    public bool shakeEnabled = true;

    // The camera in the scene, so Shake can be called without finding it first
    public static CameraFollow Instance { get; private set; }

    private Vector3 velocity;
    private Camera cam;

    // Where the camera would be without shaking. We smooth THIS position,
    // and add the shake on top only at the end, so shaking never disturbs the following.
    private Vector3 followPosition;

    private float shakeStrength; // meters
    private float shakeTime;     // how long the current shake lasts in total
    private float shakeTimer;    // how much of it is left

    void Awake()
    {
        cam = GetComponent<Camera>();
    }

    void OnEnable()
    {
        Instance = this;
    }

    void OnDisable()
    {
        if (Instance == this) Instance = null;
    }

    void Start()
    {
        SnapToTarget();
    }

    // Shakes the camera by up to 'strength' meters, calming down over 'time' seconds.
    // "static" so other scripts can just write CameraFollow.Shake(0.2f, 0.15f).
    public static void Shake(float strength, float time)
    {
        // Off if unticked here OR switched off by the player in the settings
        if (Instance == null || !Instance.shakeEnabled || !Settings.ScreenShake) return;

        // A weak shake must not cut short a strong one that is still going (e.g. the boss dying)
        float currentStrength = Instance.shakeTimer > 0f ? Instance.shakeStrength * (Instance.shakeTimer / Instance.shakeTime) : 0f;
        if (strength < currentStrength) return;

        Instance.shakeStrength = strength;
        Instance.shakeTime = time;
        Instance.shakeTimer = time;
    }

    // Jumps straight to the player and aims at them; the angle then stays fixed.
    // Also used by ChapterManager after the player is moved to a new room.
    public void SnapToTarget()
    {
        if (target == null) return;

        followPosition = target.position + offset;
        transform.position = followPosition;
        transform.rotation = Quaternion.LookRotation(-offset);
        velocity = Vector3.zero; // forget the old smoothing speed
    }

    void LateUpdate()
    {
        FitWidth();

        if (target == null) return;

        Vector3 desiredPosition = target.position + offset;

        // In the editor (not playing): snap straight into place so changes to Offset show instantly
        if (!Application.isPlaying)
        {
            transform.position = desiredPosition;
            transform.rotation = Quaternion.LookRotation(-offset);
            return;
        }

        followPosition = Vector3.SmoothDamp(followPosition, desiredPosition, ref velocity, smoothTime);
        transform.position = followPosition + ShakeOffset();
    }

    // A random little offset that gets weaker as the shake runs out (zero when not shaking).
    // Unscaled time: if the game pauses mid-shake (level up cards), the shake still calms down.
    Vector3 ShakeOffset()
    {
        if (shakeTimer <= 0f) return Vector3.zero;

        shakeTimer -= Time.unscaledDeltaTime;
        float strength = shakeStrength * Mathf.Max(shakeTimer / shakeTime, 0f);

        // Move along the screen's own left/right and up/down, so it looks the same from any angle
        Vector2 random = Random.insideUnitCircle * strength;
        return transform.right * random.x + transform.up * random.y;
    }

    // Unity's Field of View is the VERTICAL angle. On a tall phone that leaves very little width,
    // so we work backwards: from the width we want to see, calculate the vertical angle.
    void FitWidth()
    {
        if (cam == null) cam = GetComponent<Camera>();
        if (cam == null || visibleWidth <= 0f) return;

        float distance = offset.magnitude; // how far the camera is from the player
        float halfWidthAngle = Mathf.Atan(visibleWidth * 0.5f / distance);         // horizontal half-angle
        float halfHeightAngle = Mathf.Atan(Mathf.Tan(halfWidthAngle) / cam.aspect); // aspect = width / height
        cam.fieldOfView = halfHeightAngle * 2f * Mathf.Rad2Deg;
    }
}
