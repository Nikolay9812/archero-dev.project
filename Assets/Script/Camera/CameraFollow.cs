using UnityEngine;

// Archero-style camera: high, fixed angle that follows the player smoothly
// but never rotates (rotating the camera makes the controls feel confusing).
// [ExecuteAlways] also runs it in the editor, so the Game view shows the real angle without pressing Play.
[ExecuteAlways]
public class CameraFollow : MonoBehaviour
{
    public Transform target; // the player
    public Vector3 offset = new Vector3(0f, 14f, -8f);
    public float smoothTime = 0.15f; // lower = snappier

    // How many meters of the level should fit across the screen (the room is 12 m wide + walls).
    // Phones have different shapes, so the camera zooms to always show this width.
    public float visibleWidth = 14f;

    private Vector3 velocity;
    private Camera cam;

    void Awake()
    {
        cam = GetComponent<Camera>();
    }

    void Start()
    {
        SnapToTarget();
    }

    // Jumps straight to the player and aims at them; the angle then stays fixed.
    // Also used by ChapterManager after the player is moved to a new room.
    public void SnapToTarget()
    {
        if (target == null) return;

        transform.position = target.position + offset;
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

        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothTime);
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
