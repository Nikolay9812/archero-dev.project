using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// Small helpers for screens that build their UI from code (main menu, game over...).
// "static" = there is only one, it needs no GameObject: just call UiBuilder.CreateText(...).
public static class UiBuilder
{
    // A canvas that scales with the screen and can receive taps on its buttons
    public static Canvas CreateCanvas(string name, Transform parent, int sortingOrder)
    {
        // Buttons need an EventSystem to receive clicks/taps
        if (Object.FindAnyObjectByType<EventSystem>() == null)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        GameObject canvasObject = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(parent, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder; // higher numbers are drawn on top

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;

        return canvas;
    }

    // A colored image over the whole screen (also blocks taps on anything behind it)
    public static GameObject CreateFullScreenPanel(string name, Transform parent, Color color)
    {
        GameObject panel = new GameObject(name, typeof(Image));
        panel.transform.SetParent(parent, false);
        panel.GetComponent<Image>().color = color;

        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;

        return panel;
    }

    // The part of the screen not covered by a phone's notch or rounded corners. Put header and footer in here.
    public static RectTransform CreateSafeArea(Transform parent)
    {
        GameObject area = new GameObject("SafeArea", typeof(RectTransform));
        area.transform.SetParent(parent, false);

        // Screen.safeArea is in pixels, anchors go from 0 to 1 across the screen
        Rect safe = Screen.safeArea;
        RectTransform rect = area.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
        rect.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
        rect.offsetMin = rect.offsetMax = Vector2.zero;

        return rect;
    }

    // A bar across the whole width of the parent, stuck to its top or bottom edge
    public static RectTransform CreateEdgeBar(string name, Transform parent, bool atTop, float height, Color color)
    {
        RectTransform rect = CreateBox(name, parent, color, Vector2.zero, Vector2.zero).rectTransform;

        float edge = atTop ? 1f : 0f;
        rect.anchorMin = new Vector2(0f, edge);
        rect.anchorMax = new Vector2(1f, edge);
        rect.pivot = new Vector2(0.5f, edge);
        rect.sizeDelta = new Vector2(0f, height); // x = 0: exactly as wide as the anchors
        rect.anchoredPosition = Vector2.zero;

        return rect;
    }

    // A plain colored rectangle. Position is measured from the middle of the parent.
    public static Image CreateBox(string name, Transform parent, Color color, Vector2 position, Vector2 size)
    {
        GameObject box = new GameObject(name, typeof(Image));
        box.transform.SetParent(parent, false);

        Image image = box.GetComponent<Image>();
        image.color = color;
        image.rectTransform.sizeDelta = size;
        image.rectTransform.anchoredPosition = position;

        return image;
    }

    // Position is measured from the middle of the parent
    public static Text CreateText(string name, Transform parent, string content, int fontSize, Vector2 position, Vector2 size)
    {
        GameObject textObject = new GameObject(name, typeof(Text));
        textObject.transform.SetParent(parent, false);

        Text text = textObject.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = content;
        text.fontSize = fontSize;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.raycastTarget = false; // text must not steal taps from the button under it

        RectTransform rect = text.rectTransform;
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        return text;
    }

    // UnityAction = "a method with no inputs", so you can pass the method to run when tapped.
    // Returns the button's label, so the caller can change its text later.
    public static Text CreateButton(string name, Transform parent, string label, Vector2 position, Vector2 size, Color color, UnityAction onClick)
    {
        GameObject buttonObject = new GameObject(name, typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        buttonObject.GetComponent<Image>().color = color;

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.sizeDelta = size;
        rect.anchoredPosition = position;

        // Every button made here clicks, so no screen has to remember to add the sound itself
        Button button = buttonObject.GetComponent<Button>();
        button.onClick.AddListener(() => SoundManager.Play(Sound.Click));
        button.onClick.AddListener(onClick);

        return CreateText("Label", buttonObject.transform, label, 72, Vector2.zero, size);
    }
}
