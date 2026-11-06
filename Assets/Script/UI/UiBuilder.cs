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

        buttonObject.GetComponent<Button>().onClick.AddListener(onClick);

        return CreateText("Label", buttonObject.transform, label, 72, Vector2.zero, size);
    }
}
