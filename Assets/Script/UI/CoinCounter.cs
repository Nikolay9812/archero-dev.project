using UnityEngine;
using UnityEngine.UI;

// Shows the coins collected during this run, at the top right under the XP bar.
// The totals the player owns are only shown in the main menu.
// It builds its own UI when the game starts, so you only need to add this component to an empty GameObject.
// It only listens to Wallet.Changed.
public class CoinCounter : MonoBehaviour
{
    public Color textColor = new Color(1f, 0.8f, 0.15f);

    private Text label;

    void Awake()
    {
        BuildUI();
        UpdateLabel();
    }

    void OnEnable()
    {
        Wallet.Changed += UpdateLabel;
    }

    void OnDisable()
    {
        Wallet.Changed -= UpdateLabel;
    }

    void UpdateLabel()
    {
        label.text = "Coins: " + Wallet.EarnedThisRun;
    }

    void BuildUI()
    {
        GameObject canvasObject = new GameObject("CoinCounterCanvas", typeof(Canvas), typeof(CanvasScaler));
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100; // same layer as the XP bar

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject textObject = new GameObject("Coins", typeof(Text));
        textObject.transform.SetParent(canvasObject.transform, false);

        label = textObject.GetComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = 40;
        label.fontStyle = FontStyle.Bold;
        label.alignment = TextAnchor.MiddleRight;
        label.color = textColor;
        label.raycastTarget = false;
        textObject.AddComponent<Outline>().effectColor = Color.black;

        // Top-right of the screen, level with the room counter
        RectTransform rect = label.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.sizeDelta = new Vector2(400f, 70f);
        rect.anchoredPosition = new Vector2(-40f, -165f);
    }
}
