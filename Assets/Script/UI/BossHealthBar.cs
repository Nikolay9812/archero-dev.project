using UnityEngine;
using UnityEngine.UI;

// A wide red health bar with the boss's name, shown under the room counter while a boss is alive.
// It builds its own UI when the game starts, so you only need to add this component to an empty GameObject.
// It listens to EnemyBoss.Appeared / Disappeared, and to the boss's Enemy.HealthChanged.
public class BossHealthBar : MonoBehaviour
{
    public Color barColor = new Color(0.9f, 0.15f, 0.15f);
    public Color backgroundColor = new Color(0f, 0f, 0f, 0.6f);

    private GameObject root;   // everything we show/hide together
    private Image fill;
    private Text nameText;
    private Enemy bossEnemy;   // the boss we are showing right now (null = none)

    void Awake()
    {
        BuildUI();
        root.SetActive(false);
    }

    void OnEnable()
    {
        EnemyBoss.Appeared += OnBossAppeared;
        EnemyBoss.Disappeared += OnBossDisappeared;
    }

    void OnDisable()
    {
        EnemyBoss.Appeared -= OnBossAppeared;
        EnemyBoss.Disappeared -= OnBossDisappeared;
        StopListeningToBoss();
    }

    void OnBossAppeared(EnemyBoss boss)
    {
        StopListeningToBoss();

        // HealthChanged is NOT static: it belongs to this one boss, so we subscribe on the boss itself
        bossEnemy = boss.Enemy;
        bossEnemy.HealthChanged += Refresh;

        nameText.text = boss.bossName;
        root.SetActive(true);
        Refresh();
    }

    void OnBossDisappeared()
    {
        StopListeningToBoss();
        root.SetActive(false);
    }

    void StopListeningToBoss()
    {
        if (bossEnemy != null) bossEnemy.HealthChanged -= Refresh;
        bossEnemy = null;
    }

    void Refresh()
    {
        if (bossEnemy == null) return;
        fill.fillAmount = Mathf.Max(0, bossEnemy.CurrentHealth) / (float)bossEnemy.maxHealth;
    }

    void BuildUI()
    {
        GameObject canvasObject = new GameObject("BossBarCanvas", typeof(Canvas), typeof(CanvasScaler));
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100; // same layer as the XP bar

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;

        root = new GameObject("BossBar", typeof(RectTransform));
        root.transform.SetParent(canvasObject.transform, false);
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = rootRect.offsetMax = Vector2.zero;

        Texture2D texture = Texture2D.whiteTexture;
        Sprite white = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));

        // The bar: stretched across the top of the screen, under the room counter (which ends 235 below the top)
        Image background = CreateImage("Background", root.transform, white, backgroundColor);
        RectTransform barRect = background.rectTransform;
        barRect.anchorMin = new Vector2(0f, 1f);
        barRect.anchorMax = new Vector2(1f, 1f);
        barRect.pivot = new Vector2(0.5f, 1f);
        barRect.offsetMin = new Vector2(120f, -340f); // left margin, bottom edge 340 below the top
        barRect.offsetMax = new Vector2(-120f, -305f); // right margin, top edge 305 below the top

        fill = CreateImage("Fill", background.transform, white, barColor);
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        RectTransform fillRect = fill.rectTransform;
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(4f, 4f);
        fillRect.offsetMax = new Vector2(-4f, -4f);

        // The name sits just above the bar
        GameObject textObject = new GameObject("BossName", typeof(Text));
        textObject.transform.SetParent(root.transform, false);
        nameText = textObject.GetComponent<Text>();
        nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        nameText.fontSize = 40;
        nameText.fontStyle = FontStyle.Bold;
        nameText.alignment = TextAnchor.MiddleCenter;
        nameText.color = Color.white;
        nameText.horizontalOverflow = HorizontalWrapMode.Overflow;
        nameText.raycastTarget = false;
        textObject.AddComponent<Outline>().effectColor = Color.black;

        RectTransform nameRect = nameText.rectTransform;
        nameRect.anchorMin = nameRect.anchorMax = new Vector2(0.5f, 1f);
        nameRect.pivot = new Vector2(0.5f, 1f);
        nameRect.sizeDelta = new Vector2(800f, 60f);
        nameRect.anchoredPosition = new Vector2(0f, -245f);
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
}
