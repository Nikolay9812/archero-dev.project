using UnityEngine;
using UnityEngine.UI;

// Shows a live 3D copy of the character inside the menu UI.
// How: a second camera films the model far away from everything else, and draws into a RenderTexture
// (a picture that a camera can paint into every frame). A RawImage in the UI shows that picture.
// GearScreen adds this component by code and calls Build().
public class AvatarView : MonoBehaviour
{
    public float turnSpeed = 0f;          // degrees per second, 0 = stand still
    public float facingAngle = 200f;      // 180 = looking straight at the camera, a bit more = turned slightly
    public Vector3 stagePosition = new Vector3(0f, -500f, 0f); // far below anything else in the scene

    private RenderTexture picture;
    private Transform stage;
    private Transform model;

    public void Build(Transform parent, Vector2 position, Vector2 size, GameObject modelPrefab, RuntimeAnimatorController animatorController, Color background)
    {
        // A RawImage shows a texture (a normal Image only shows sprites)
        GameObject imageObject = new GameObject("Avatar", typeof(RawImage));
        imageObject.transform.SetParent(parent, false);
        RawImage image = imageObject.GetComponent<RawImage>();
        image.rectTransform.anchoredPosition = position;
        image.rectTransform.sizeDelta = size;
        image.raycastTarget = false; // let swipes go through to the pages

        if (modelPrefab == null)
        {
            // Nothing to film: show a plain box with a hint, so the page still works
            image.color = background;
            UiBuilder.CreateText("Hint", imageObject.transform, "Drag the character model\ninto MainMenu > Avatar Model", 30, Vector2.zero, size);
            return;
        }

        // The texture has the same shape as the UI box, so the picture isn't squashed
        picture = new RenderTexture(Mathf.RoundToInt(size.x), Mathf.RoundToInt(size.y), 24);
        image.texture = picture;

        stage = new GameObject("AvatarStage").transform;
        stage.position = stagePosition;

        model = Instantiate(modelPrefab, stage.position, Quaternion.Euler(0f, facingAngle, 0f), stage).transform;
        Animator animator = model.GetComponentInChildren<Animator>();
        if (animator != null)
        {
            if (animatorController != null) animator.runtimeAnimatorController = animatorController; // "Speed" is 0 = Idle
            animator.applyRootMotion = false;
        }

        model.gameObject.AddComponent<BowVisual>(); // the same bow as in the game, in the worn weapon's color

        CreateLight();
        CreateCamera(background);
    }

    void Update()
    {
        if (model != null) model.Rotate(0f, turnSpeed * Time.deltaTime, 0f);
    }

    void OnDestroy()
    {
        // A RenderTexture is not cleaned up by itself when the scene closes
        if (picture != null) picture.Release();
        if (stage != null) Destroy(stage.gameObject);
    }

    void CreateCamera(Color background)
    {
        GameObject cameraObject = new GameObject("AvatarCamera", typeof(Camera));
        cameraObject.transform.SetParent(stage, false);

        Camera avatarCamera = cameraObject.GetComponent<Camera>();
        avatarCamera.targetTexture = picture; // paint into the texture, not onto the screen
        avatarCamera.clearFlags = CameraClearFlags.SolidColor;
        avatarCamera.backgroundColor = background;
        avatarCamera.fieldOfView = 30f;

        // Fit the whole body into the picture: measure the model with its renderers' bounds
        Bounds body = new Bounds(model.position + Vector3.up, Vector3.one); // a guess, in case it has no renderers
        Renderer[] renderers = model.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            body = renderers[0].bounds;
            foreach (Renderer r in renderers) body.Encapsulate(r.bounds); // grow the box until everything is inside
        }

        // Half the height (plus some space) must fit into half the field of view:
        // tan(half angle) = half height / distance, so distance = half height / tan(half angle)
        float halfHeight = body.extents.y * 1.15f;
        float distance = halfHeight / Mathf.Tan(avatarCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);

        // The model is turned around (facingAngle about 180), so it looks towards -Z: put the camera there
        cameraObject.transform.position = body.center + Vector3.back * distance;
        cameraObject.transform.LookAt(body.center);
    }

    void CreateLight()
    {
        GameObject lightObject = new GameObject("AvatarLight", typeof(Light));
        lightObject.transform.SetParent(stage, false);
        lightObject.transform.rotation = Quaternion.Euler(30f, 20f, 0f); // shines from the camera's side, a bit from above

        Light avatarLight = lightObject.GetComponent<Light>();
        avatarLight.type = LightType.Directional;
        avatarLight.intensity = 1.2f;
    }
}
