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

    private Vector3 velocity;

    void Start()
    {
        if (target == null) return;

        // Snap into place and aim at the player once; the angle then stays fixed
        transform.position = target.position + offset;
        transform.rotation = Quaternion.LookRotation(-offset);
    }

    void LateUpdate()
    {
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
}
