using UnityEngine;
using UnityEngine.UI;

// Shows "3 / 10" under the XP bar: which room of the chapter we are in.
// It builds its own UI when the game starts, so you only need to add this component to an empty GameObject.
// It only listens to ChapterManager.RoomStarted.
public class RoomCounter : MonoBehaviour
{
    private Text label;

    void Awake()
    {
        BuildUI();
        label.text = "";
    }

    void OnEnable()
    {
        ChapterManager.RoomStarted += OnRoomStarted;
    }

    void OnDisable()
    {
        ChapterManager.RoomStarted -= OnRoomStarted;
    }

    void OnRoomStarted(int roomNumber, int roomCount)
    {
        // Room 0 is the start room: show nothing there
        if (roomNumber == 0) label.text = "";
        else label.text = roomNumber + " / " + roomCount;
    }

    void BuildUI()
    {
        GameObject canvasObject = new GameObject("RoomCounterCanvas", typeof(Canvas), typeof(CanvasScaler));
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100; // same layer as the XP bar

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject textObject = new GameObject("RoomNumber", typeof(Text));
        textObject.transform.SetParent(canvasObject.transform, false);

        label = textObject.GetComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = 48;
        label.fontStyle = FontStyle.Bold;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;
        label.raycastTarget = false;
        textObject.AddComponent<Outline>().effectColor = Color.black;

        // Top-middle of the screen, just under the XP bar (which ends 150 below the top)
        RectTransform rect = label.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(400f, 70f);
        rect.anchoredPosition = new Vector2(0f, -165f);
    }
}
