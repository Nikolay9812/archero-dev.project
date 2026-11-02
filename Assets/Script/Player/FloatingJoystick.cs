using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Archero-style floating joystick: touch anywhere, the stick appears under your finger,
// drag to move. Works with the mouse in the editor too.
// It builds its own UI when the game starts, so you only need to add this component to an empty GameObject.
public class FloatingJoystick : MonoBehaviour
{
    public float radius = 130f;    // how far the knob can move (in pixels on a 1080x1920 screen)
    public float deadZone = 0.1f;  // ignore tiny finger movements
    public Color backgroundColor = new Color(1f, 1f, 1f, 0.25f);
    public Color knobColor = new Color(1f, 1f, 1f, 0.7f);

    // x = left/right, y = forward/back, length 0..1. PlayerMovement reads this.
    public Vector2 Direction { get; private set; }

    private RectTransform canvasRect;
    private RectTransform background;
    private RectTransform knob;
    private Vector2 startPosition;
    private bool isHeld;

    void Awake()
    {
        BuildUI();
        Hide();
    }

    void Update()
    {
        // Pointer = mouse on PC, first finger on a phone
        Pointer pointer = Pointer.current;

        // Paused (skill choice, Game Over): taps are for the buttons, not for walking
        if (pointer == null || Time.timeScale == 0f)
        {
            Hide();
            return;
        }

        Vector2 localPoint = ScreenToCanvas(pointer.position.ReadValue());

        if (pointer.press.wasPressedThisFrame)
        {
            // Finger down: place the joystick right here
            isHeld = true;
            startPosition = localPoint;
            background.anchoredPosition = startPosition;
            knob.anchoredPosition = Vector2.zero;
            background.gameObject.SetActive(true);
        }
        else if (isHeld && pointer.press.isPressed)
        {
            // Dragging: move the knob, but keep it inside the circle
            Vector2 offset = Vector2.ClampMagnitude(localPoint - startPosition, radius);
            knob.anchoredPosition = offset;

            Vector2 direction = offset / radius;
            Direction = direction.magnitude < deadZone ? Vector2.zero : direction;
        }
        else if (isHeld)
        {
            // Finger lifted
            Hide();
        }
    }

    private void Hide()
    {
        isHeld = false;
        Direction = Vector2.zero;
        if (background != null) background.gameObject.SetActive(false);
    }

    private Vector2 ScreenToCanvas(Vector2 screenPosition)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition, null, out Vector2 localPoint);
        return localPoint;
    }

    private void BuildUI()
    {
        // Canvas that scales the same on every phone
        GameObject canvasObject = new GameObject("JoystickCanvas", typeof(Canvas), typeof(CanvasScaler));
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasRect = canvasObject.GetComponent<RectTransform>();

        Sprite circle = CreateCircleSprite(128);
        background = CreateImage("Background", canvasRect, circle, backgroundColor, radius * 2f);
        knob = CreateImage("Knob", background, circle, knobColor, radius * 0.9f);
    }

    private static RectTransform CreateImage(string name, Transform parent, Sprite sprite, Color color, float size)
    {
        GameObject imageObject = new GameObject(name, typeof(Image));
        imageObject.transform.SetParent(parent, false);

        Image image = imageObject.GetComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;

        RectTransform rect = imageObject.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(size, size);
        return rect;
    }

    // Draws a soft-edged white circle so we don't need any image files
    private static Sprite CreateCircleSprite(int size)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float center = (size - 1) / 2f;
        float circleRadius = size / 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                float alpha = Mathf.Clamp01(circleRadius - distance);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }
}
