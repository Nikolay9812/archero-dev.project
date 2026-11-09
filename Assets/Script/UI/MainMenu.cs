using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// The first screen of the game: a header that stays on top, tabs that stay at the bottom,
// and pages in between that you can swipe (PageSwiper). The tabs are in BottomTabBar.
// It builds its own UI when the game starts, so you only need to add this component to an empty GameObject.
public class MainMenu : MonoBehaviour
{
    public string gameTitle = "ARROW RUN";
    public string gameSceneName = "Game"; // must be in the Build Profile's scene list
    public Color backgroundColor = new Color(0.13f, 0.17f, 0.25f);
    public Color titleColor = Color.white;
    public Color profileColor = new Color(0.4f, 1f, 0.4f);
    public Color coinsColor = new Color(1f, 0.8f, 0.15f);
    public Color gemsColor = new Color(0.3f, 0.9f, 1f);
    public Color buttonColor = new Color(0.95f, 0.65f, 0.15f);
    public Color headerColor = new Color(0.09f, 0.11f, 0.18f);
    public Color slotColor = new Color(0.17f, 0.21f, 0.32f); // behind the XP bar, coins and gems
    public Color chapterCardColor = new Color(0.25f, 0.4f, 0.55f);

    [Header("Gear page")]
    public GameObject avatarModel;                     // drag the character model here (e.g. SanpakuChan)
    public RuntimeAnimatorController avatarAnimator;   // drag PlayerAnimator here, so she breathes (Idle)

    private const float HeaderHeight = 140f;
    private const float FooterHeight = 170f; // same as BottomTabBar.tabHeight

    // One page per tab. To add or remove a tab, change this list and the page numbers below it.
    private const int ShopPage = 0;
    private const int GearPage = 1;
    private const int PlayPage = 2;
    private const int CollectiblePage = 3;
    private const int EventsPage = 4;
    private readonly string[] tabNames = { "SHOP", "GEAR", "PLAY", "COLLECTIBLE", "EVENTS" };

    private bool canPressPlay; // false while fading, so two fades can't run against each other
    private Text coinsText;
    private Text gemsText;

    void Awake()
    {
        Time.timeScale = 1f; // in case we came here from a paused Game Over
        BuildUI();
    }

    void OnEnable()
    {
        Wallet.Changed += UpdateCurrencyTexts; // buying something in the shop changes the coins in the header
    }

    void OnDisable()
    {
        Wallet.Changed -= UpdateCurrencyTexts;
    }

    // Start can be a coroutine too: Unity runs it like StartCoroutine would.
    // ScreenFader starts fully black, so fade in first.
    IEnumerator Start()
    {
        if (ScreenFader.Instance != null) yield return ScreenFader.Instance.FadeIn();
        canPressPlay = true;
    }

    public void Play()
    {
        if (!canPressPlay) return;

        canPressPlay = false;
        StartCoroutine(FadeOutAndStartGame());
    }

    IEnumerator FadeOutAndStartGame()
    {
        if (ScreenFader.Instance != null) yield return ScreenFader.Instance.FadeOut();
        SceneManager.LoadScene(gameSceneName);
    }

    void UpdateCurrencyTexts()
    {
        coinsText.text = Wallet.Coins.ToString();
        gemsText.text = Wallet.Gems.ToString();
    }

    void BuildUI()
    {
        Canvas canvas = UiBuilder.CreateCanvas("MenuCanvas", transform, 0);

        // Always 1080 units wide, so the positions below fit every phone. Taller phones just get more height.
        canvas.GetComponent<CanvasScaler>().matchWidthOrHeight = 0f;

        GameObject background = UiBuilder.CreateFullScreenPanel("Background", canvas.transform, backgroundColor);
        RectTransform safeArea = UiBuilder.CreateSafeArea(background.transform);

        PageSwiper swiper = BuildPages(safeArea);
        BuildHeader(safeArea);

        string[] labels = (string[])tabNames.Clone();
        if (!PlayerProfile.IsShopUnlocked) labels[ShopPage] += "\nLV " + PlayerProfile.ShopUnlockLevel;
        gameObject.AddComponent<BottomTabBar>().Build(safeArea, labels, swiper);
    }

    PageSwiper BuildPages(RectTransform parent)
    {
        // The swiper's area must be able to receive touches, so it gets an invisible image
        GameObject area = UiBuilder.CreateFullScreenPanel("PageArea", parent, Color.clear);
        RectTransform rect = area.GetComponent<RectTransform>();
        rect.offsetMin = new Vector2(0f, FooterHeight);
        rect.offsetMax = new Vector2(0f, -HeaderHeight);

        PageSwiper swiper = area.AddComponent<PageSwiper>();
        swiper.Setup(tabNames.Length, PlayPage);

        for (int i = 0; i < tabNames.Length; i++)
        {
            Transform page = swiper.CreatePage(i);

            if (i == ShopPage) BuildShopPage(page);
            else if (i == GearPage) gameObject.AddComponent<GearScreen>().Build(page, avatarModel, avatarAnimator);
            else if (i == PlayPage) BuildPlayPage(page);
            else if (i == CollectiblePage) gameObject.AddComponent<CollectibleScreen>().Build(page);
            else if (i == EventsPage) gameObject.AddComponent<EventsScreen>().Build(page);
            else BuildComingSoonPage(page, tabNames[i]); // a new tab without its own page yet
        }

        return swiper;
    }

    void BuildPlayPage(Transform page)
    {
        Text title = UiBuilder.CreateText("Title", page, gameTitle, 120, new Vector2(0f, 600f), new Vector2(1000f, 220f));
        title.color = titleColor;

        // A placeholder for a picture of the chapter
        Image card = UiBuilder.CreateBox("ChapterCard", page, chapterCardColor, new Vector2(0f, 100f), new Vector2(640f, 640f));
        UiBuilder.CreateText("ChapterName", card.transform, "CHAPTER 1", 80, Vector2.zero, new Vector2(620f, 160f));

        // Keep the button at the bottom of the page, however tall the phone is
        Text label = UiBuilder.CreateButton("PlayButton", page, "PLAY", Vector2.zero, new Vector2(560f, 180f), buttonColor, Play);
        RectTransform button = label.transform.parent.GetComponent<RectTransform>();
        button.anchorMin = button.anchorMax = new Vector2(0.5f, 0f);
        button.anchoredPosition = new Vector2(0f, 170f);

        BuildSettingsButton(page);
    }

    // A square button in the top right corner of the page, with 3 white bars as its icon
    void BuildSettingsButton(Transform page)
    {
        Transform canvas = page.GetComponentInParent<Canvas>().transform;
        Text label = UiBuilder.CreateButton("SettingsButton", page, "", Vector2.zero, new Vector2(120f, 120f), slotColor, () => SettingsScreen.Open(canvas));

        RectTransform button = label.transform.parent.GetComponent<RectTransform>();
        button.anchorMin = button.anchorMax = button.pivot = new Vector2(1f, 1f); // measured from the top right corner
        button.anchoredPosition = new Vector2(-30f, -30f);

        for (int i = 0; i < 3; i++)
        {
            Image bar = UiBuilder.CreateBox("Bar" + i, button, Color.white, new Vector2(0f, (1 - i) * 28f), new Vector2(70f, 12f));
            bar.raycastTarget = false; // let the tap reach the button under it
        }
    }

    void BuildShopPage(Transform page)
    {
        if (PlayerProfile.IsShopUnlocked)
        {
            gameObject.AddComponent<ShopScreen>().Build(page);
            return;
        }

        string message = "Reach level " + PlayerProfile.ShopUnlockLevel + "\nto open the shop"; // \n = new line
        UiBuilder.CreateText("Title", page, tabNames[ShopPage], 110, new Vector2(0f, 500f), new Vector2(1000f, 180f));
        UiBuilder.CreateText("Locked", page, message, 70, Vector2.zero, new Vector2(1000f, 300f));
    }

    void BuildComingSoonPage(Transform page, string pageName)
    {
        UiBuilder.CreateText("Title", page, pageName, 110, new Vector2(0f, 500f), new Vector2(1000f, 180f));
        UiBuilder.CreateText("ComingSoon", page, "Coming soon", 70, Vector2.zero, new Vector2(1000f, 120f));
    }

    void BuildHeader(RectTransform parent)
    {
        RectTransform header = UiBuilder.CreateEdgeBar("Header", parent, true, HeaderHeight, headerColor);

        // Profile level + XP bar
        Image badge = UiBuilder.CreateBox("LevelBadge", header, profileColor, new Vector2(-470f, 0f), new Vector2(100f, 100f));
        Text level = UiBuilder.CreateText("Level", badge.transform, PlayerProfile.Level.ToString(), 60, Vector2.zero, new Vector2(100f, 100f));
        level.color = headerColor;

        Image xpBar = UiBuilder.CreateBox("XpBar", header, slotColor, new Vector2(-255f, 0f), new Vector2(300f, 56f));

        // The anchors make the fill exactly as wide as this part of the bar
        RectTransform fill = UiBuilder.CreateBox("Fill", xpBar.transform, profileColor, Vector2.zero, Vector2.zero).rectTransform;
        fill.anchorMin = Vector2.zero;
        fill.anchorMax = new Vector2((float)PlayerProfile.Xp / PlayerProfile.XpToNextLevel, 1f); // (float) = keep the decimals
        fill.offsetMin = fill.offsetMax = Vector2.zero;

        UiBuilder.CreateText("Xp", xpBar.transform, PlayerProfile.Xp + " / " + PlayerProfile.XpToNextLevel, 30, Vector2.zero, new Vector2(300f, 56f));

        // Coins and gems. The gem icon is the same square, turned 45 degrees.
        coinsText = CreateCurrency(header, "Coins", coinsColor, new Vector2(90f, 0f), 0f);
        gemsText = CreateCurrency(header, "Gems", gemsColor, new Vector2(355f, 0f), 45f);
        UpdateCurrencyTexts();
    }

    Text CreateCurrency(Transform header, string currencyName, Color color, Vector2 position, float iconAngle)
    {
        Image slot = UiBuilder.CreateBox(currencyName, header, slotColor, position, new Vector2(240f, 76f));

        Image icon = UiBuilder.CreateBox("Icon", slot.transform, color, new Vector2(-85f, 0f), new Vector2(40f, 40f));
        icon.rectTransform.localEulerAngles = new Vector3(0f, 0f, iconAngle);

        Text amount = UiBuilder.CreateText("Amount", slot.transform, "", 42, new Vector2(30f, 0f), new Vector2(170f, 76f));
        amount.color = color;
        return amount;
    }
}
