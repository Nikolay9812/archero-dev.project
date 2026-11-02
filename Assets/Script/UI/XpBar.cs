using UnityEngine;
using UnityEngine.UI;

// The XP bar at the top of the screen with the player's level, plus a short "LEVEL UP!" message.
// It builds its own UI when the game starts, so you only need to add this component to an empty GameObject.
// It only listens to PlayerXp's events: it never changes the XP itself.
public class XpBar : MonoBehaviour
{
    public Color barColor = new Color(0.3f, 0.9f, 1f);
    public Color backgroundColor = new Color(0f, 0f, 0f, 0.6f);
    public float levelUpMessageTime = 1.2f;

    private Image fill;
    private Text levelText;
    private Text levelUpText;

    void Awake()
    {
        BuildUI();
        levelUpText.gameObject.SetActive(false);
    }

    void OnEnable()
    {
        PlayerXp.XpChanged += Refresh;
        PlayerXp.LeveledUp += OnLeveledUp;
    }

    void OnDisable()
    {
        PlayerXp.XpChanged -= Refresh;
        PlayerXp.LeveledUp -= OnLeveledUp;
    }

    void Start()
    {
        // Draw the starting values once (no event has happened yet)
        Refresh();
    }

    void Refresh()
    {
        PlayerXp xp = PlayerXp.Instance;
        if (xp == null) return;

        fill.fillAmount = (float)xp.CurrentXp / xp.XpToNextLevel;
        levelText.text = "Lv " + xp.Level;
    }

    void OnLeveledUp(int newLevel)
    {
        levelUpText.gameObject.SetActive(true);

        // If we level up again while the message is showing, restart its timer
        CancelInvoke(nameof(HideLevelUpMessage));
        Invoke(nameof(HideLevelUpMessage), levelUpMessageTime);
    }

    void HideLevelUpMessage()
    {
        levelUpText.gameObject.SetActive(false);
    }

    void BuildUI()
    {
        GameObject canvasObject = new GameObject("XpCanvas", typeof(Canvas), typeof(CanvasScaler));
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100; // above the game, below the Game Over screen (200)

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;

        Sprite white = CreateWhiteSprite();

        // The bar: anchored to the top of the screen, stretched across its width (with a margin)
        Image background = CreateImage("XpBarBackground", canvasObject.transform, white, backgroundColor);
        RectTransform barRect = background.rectTransform;
        barRect.anchorMin = new Vector2(0f, 1f); // left-top
        barRect.anchorMax = new Vector2(1f, 1f); // right-top
        barRect.pivot = new Vector2(0.5f, 1f);
        barRect.offsetMin = new Vector2(60f, -150f); // left margin, bottom edge 150 below the top
        barRect.offsetMax = new Vector2(-60f, -100f); // right margin, top edge 100 below the top

        fill = CreateImage("Fill", background.transform, white, barColor);
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        StretchToParent(fill.rectTransform, 4f); // 4px border of background around the fill

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        levelText = CreateText("Level", background.transform, font, 40);
        StretchToParent(levelText.rectTransform, 0f);

        levelUpText = CreateText("LevelUp", canvasObject.transform, font, 90);
        levelUpText.text = "LEVEL UP!";
        levelUpText.color = new Color(1f, 0.85f, 0.3f);
        levelUpText.rectTransform.sizeDelta = new Vector2(1000f, 140f);
        levelUpText.rectTransform.anchoredPosition = new Vector2(0f, 400f); // above the middle of the screen
    }

    static void StretchToParent(RectTransform rect, float border)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(border, border);
        rect.offsetMax = new Vector2(-border, -border);
    }

    static Image CreateImage(string name, Transform parent, Sprite sprite, Color color)
    {
        GameObject imageObject = new GameObject(name, typeof(Image));
        imageObject.transform.SetParent(parent, false);

        Image image = imageObject.GetComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false; // the bar shouldn't block taps on the joystick
        return image;
    }

    static Text CreateText(string name, Transform parent, Font font, int fontSize)
    {
        GameObject textObject = new GameObject(name, typeof(Text));
        textObject.transform.SetParent(parent, false);

        Text text = textObject.GetComponent<Text>();
        text.font = font;
        text.fontSize = fontSize;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.raycastTarget = false;
        textObject.AddComponent<Outline>().effectColor = Color.black;
        return text;
    }

    // A filled Image needs a sprite, so make a tiny white one
    static Sprite CreateWhiteSprite()
    {
        Texture2D texture = Texture2D.whiteTexture;
        return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
    }
}
