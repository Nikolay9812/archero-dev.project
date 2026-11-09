using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// The SHOP page of the main menu. One long list that scrolls up and down, made of sections:
//   Chapter Pack (slide left/right) - Daily Shop - Gear Chest - Wish
//   Daily / Weekly / Monthly Special Pack - Gems - Coins - Crystals
// The shop tabs at the bottom jump to a section, and the tab of the section you are looking at lights up.
// Limited Offer and Top Up get a second row of small tabs for their three sections.
//
// What is sold is in ShopCatalog, the buying rules are in Shop. This script only draws it.
// After every purchase the whole list is built again (it is fast, and much simpler than updating each card).
// MainMenu adds this component by code and calls Build(), so there is nothing to set up in the scene.
public class ShopScreen : MonoBehaviour
{
    [Header("Colors")]
    public Color panelColor = new Color(0.17f, 0.21f, 0.32f);
    public Color bannerColor = new Color(0.2f, 0.45f, 0.75f);
    public Color infoTextColor = new Color(0.5f, 0.78f, 1f);
    public Color blueCardColor = new Color(0.25f, 0.55f, 0.85f);
    public Color purpleCardColor = new Color(0.55f, 0.3f, 0.8f);
    public Color goldCardColor = new Color(0.9f, 0.68f, 0.2f);
    public Color crystalCardColor = new Color(0.75f, 0.35f, 0.8f);
    public Color soldOutColor = new Color(0.35f, 0.37f, 0.42f);
    public Color priceButtonColor = new Color(0.85f, 0.93f, 1f);
    public Color priceTextColor = new Color(0.15f, 0.2f, 0.35f);
    public Color actionButtonColor = new Color(0.35f, 0.75f, 0.25f);
    public Color valueBadgeColor = new Color(0.85f, 0.2f, 0.25f);
    public Color discountBadgeColor = new Color(1f, 0.75f, 0.1f);
    public Color bonusBadgeColor = new Color(0.25f, 0.7f, 0.3f);
    public Color tabColor = new Color(0.17f, 0.21f, 0.32f);
    public Color selectedTabColor = new Color(1f, 0.8f, 0.15f);
    public Color windowColor = new Color(0.13f, 0.16f, 0.24f);

    // ---------- Layout numbers (canvas units, the menu is always 1080 wide) ----------
    private const float SectionWidth = 1040f;
    private const float CardWidth = 320f;
    private const float CardHeight = 440f;
    private const float CardStepX = 340f;  // from the middle of one card to the next
    private const float CardStepY = 485f;
    private const float TabBarHeight = 120f;
    private const float SectionGap = 40f;

    // The shop tabs. The numbers are the tab indexes.
    private static readonly string[] TabNames = { "Daily\nShop", "Gear\nChest", "Wish", "Limited\nOffer", "Top Up" };
    private const int LimitedTab = 3;
    private const int TopUpTab = 4;
    private static readonly string[] LimitedTabNames = { "Daily Special", "Weekly Special", "Monthly Special" };
    private static readonly string[] TopUpTabNames = { "Gems", "Coins", "Crystals" };

    // Where each section starts (distance from the top of the list). Filled in while building.
    private float[] tabTops = new float[TabNames.Length];
    private float[] limitedTops = new float[3];
    private float[] topUpTops = new float[3];

    private Transform overlayParent; // the whole canvas, so popups cover the header and tabs too
    private ScrollRect list;
    private RectTransform content;
    private float cursor; // how far down the list the next section goes

    private Image[] tabs;
    private GameObject subTabRow;
    private Image[] subTabs = new Image[3];
    private Text[] subTabLabels = new Text[3];
    private int subRowShown = -1; // 0 = Limited Offer tabs, 1 = Top Up tabs, -1 = hidden

    private Carousel chapterSlider;
    private int chapterCard;   // remembered when the list is built again
    private int heroFocus;     // the Hero Chest's focus tab

    // Texts that count down every second ("Refresh in 05:19:26").
    // Func<string> = "a method that returns a string": it makes the new text each time.
    private class Timer
    {
        public Text text;
        public Func<string> makeText;
    }
    private List<Timer> timers = new List<Timer>();
    private float nextTimerUpdate;
    private string builtState;  // see StateKey
    private bool rebuildNeeded;
    private GameObject popup;

    public void Build(Transform page)
    {
        overlayParent = page.GetComponentInParent<Canvas>().transform;

        BuildList(page);
        BuildTabs(page);
        Fill();
    }

    void OnEnable()
    {
        Shop.Changed += AskForRebuild;
        Artifacts.Changed += AskForRebuild; // the wish luck counter
    }

    void OnDisable()
    {
        Shop.Changed -= AskForRebuild;
        Artifacts.Changed -= AskForRebuild;
    }

    // A x10 wish changes things 10 times: just remember it, and build once in the next Update
    void AskForRebuild()
    {
        rebuildNeeded = true;
    }

    void Update()
    {
        if (content == null) return;

        if (rebuildNeeded)
        {
            rebuildNeeded = false;
            Fill();
        }

        // Once a second: count the timers down. If a new day started or a free chest is ready, build again.
        if (Time.unscaledTime >= nextTimerUpdate)
        {
            nextTimerUpdate = Time.unscaledTime + 1f;
            if (StateKey() != builtState) Fill();
            foreach (Timer timer in timers) timer.text.text = timer.makeText();
        }

        UpdateTabs();
    }

    // Everything that changes the shop by itself, just because time passes, as one string
    string StateKey()
    {
        return Shop.PeriodKey(ShopPeriod.Daily) + " " + Shop.PeriodKey(ShopPeriod.Weekly) + " " + Shop.PeriodKey(ShopPeriod.Monthly)
            + " " + Shop.IsFree(ShopCatalog.BronzeChest) + " " + Shop.IsFree(ShopCatalog.RoyalChest);
    }

    // ---------- The list ----------

    void BuildList(Transform page)
    {
        // The visible window, above the shop tabs. Like UiBuilder.CreateScrollGrid, but without a grid:
        // the sections have different heights, so this script places them itself.
        GameObject view = UiBuilder.CreateFullScreenPanel("ShopList", page, Color.clear);
        view.AddComponent<RectMask2D>();
        view.GetComponent<RectTransform>().offsetMin = new Vector2(0f, TabBarHeight);

        GameObject contentObject = new GameObject("Content", typeof(RectTransform));
        contentObject.transform.SetParent(view.transform, false);
        content = contentObject.GetComponent<RectTransform>();
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);

        list = view.AddComponent<NestedScrollRect>();
        list.viewport = view.GetComponent<RectTransform>();
        list.content = content;
        list.horizontal = false;
        list.movementType = ScrollRect.MovementType.Clamped;
        list.scrollSensitivity = 60f;
    }

    // Builds every section from the top. Called again after each purchase; the scroll position stays.
    void Fill()
    {
        float scrolled = content.anchoredPosition.y;
        if (chapterSlider != null) chapterCard = chapterSlider.CurrentCard;

        UiBuilder.ClearChildren(content);
        timers.Clear();
        cursor = 20f;

        tabTops[0] = 0f;
        BuildChapterPacks();
        BuildDailyShop();

        tabTops[1] = cursor;
        BuildGearChests();

        tabTops[2] = cursor;
        BuildWish();

        tabTops[LimitedTab] = limitedTops[0] = cursor;
        BuildPackSection("Daily Special Pack", ShopCatalog.DailySpecials, ShopPeriod.Daily, blueCardColor);
        limitedTops[1] = cursor;
        BuildPackSection("Weekly Special Pack", ShopCatalog.WeeklySpecials, ShopPeriod.Weekly, purpleCardColor);
        limitedTops[2] = cursor;
        BuildPackSection("Monthly Special Pack", ShopCatalog.MonthlySpecials, ShopPeriod.Monthly, goldCardColor);

        tabTops[TopUpTab] = topUpTops[0] = cursor;
        BuildPackSection("Gems", ShopCatalog.GemPacks, ShopPeriod.Unlimited, purpleCardColor);
        topUpTops[1] = cursor;
        BuildPackSection("Coins", ShopCatalog.CoinPacks, ShopPeriod.Unlimited, blueCardColor);
        topUpTops[2] = cursor;
        BuildCrystals();

        // Extra space at the end, so the last section can scroll up above the shop tabs
        content.sizeDelta = new Vector2(0f, cursor + 400f);
        content.anchoredPosition = new Vector2(0f, scrolled);

        builtState = StateKey();
    }

    // ---------- Sections ----------

    void BuildChapterPacks()
    {
        RectTransform section = BeginSection("Chapter Pack", out float y);

        const float viewHeight = 400f;
        const float cardStep = 960f; // card width 920 + a gap of 40
        ShopOffer[] packs = ShopCatalog.ChapterPacks;

        // The window of the slider. RectMask2D hides the cards outside it.
        Image view = UiBuilder.CreateBox("ChapterSlider", section, Color.clear, Vector2.zero, new Vector2(SectionWidth, viewHeight));
        PinTop(view.rectTransform, 0f, y);
        view.gameObject.AddComponent<RectMask2D>();

        // The row of cards, measured from its left edge. 40 extra on each side, so a card sits in the middle.
        GameObject rowObject = new GameObject("Cards", typeof(RectTransform));
        rowObject.transform.SetParent(view.transform, false);
        RectTransform row = rowObject.GetComponent<RectTransform>();
        row.anchorMin = row.anchorMax = row.pivot = new Vector2(0f, 0.5f);
        row.sizeDelta = new Vector2(packs.Length * cardStep + 80f, viewHeight);

        for (int i = 0; i < packs.Length; i++)
        {
            CreateChapterCard(row, packs[i], i + 1, new Vector2(40f + i * cardStep + cardStep / 2f, 0f));
        }

        // The dots under the slider
        Image[] dots = new Image[packs.Length];
        for (int i = 0; i < packs.Length; i++)
        {
            dots[i] = UiBuilder.CreateBox("Dot" + i, section, Color.white, Vector2.zero, new Vector2(22f, 22f));
            PinTop(dots[i].rectTransform, (i - (packs.Length - 1) / 2f) * 40f, y + viewHeight + 15f);
        }

        chapterSlider = view.gameObject.AddComponent<Carousel>();
        chapterSlider.viewport = view.rectTransform;
        chapterSlider.content = row;
        chapterSlider.horizontal = true;
        chapterSlider.vertical = false;
        chapterSlider.inertia = false;
        chapterSlider.movementType = ScrollRect.MovementType.Clamped;
        chapterSlider.dots = dots;
        chapterSlider.Setup(cardStep, packs.Length, chapterCard);

        EndSection(section, y + viewHeight + 55f);
    }

    void BuildDailyShop()
    {
        RectTransform section = BeginSection("Daily Shop", out float y);
        y = AddTimer(section, y, ShopPeriod.Daily);
        y = AddCards(section, ShopCatalog.DailyShop(Shop.PeriodKey(ShopPeriod.Daily)), y, blueCardColor);
        EndSection(section, y);
    }

    void BuildGearChests()
    {
        RectTransform section = BeginSection("Gear Chest", out float y);

        y = CreateChestCard(section, ShopCatalog.HeroChest, y);

        // The focus tabs under the Hero Chest
        for (int i = 0; i < ShopCatalog.FocusNames.Length; i++)
        {
            int focus = i; // a copy for the lambda (see BottomTabBar)
            Color color = i == heroFocus ? selectedTabColor : tabColor;
            Text label = UiBuilder.CreateButton("Focus" + i, section, ShopCatalog.FocusNames[i], Vector2.zero, new Vector2(320f, 90f), color, () => ChooseFocus(focus));
            label.fontSize = 38;
            if (i == heroFocus) label.color = priceTextColor;
            PinTop(label.transform.parent.GetComponent<RectTransform>(), (i - 1) * CardStepX, y);
        }
        y += 120f;

        y = CreateChestCard(section, ShopCatalog.PrismChest, y);

        CreateSmallChestCard(section, ShopCatalog.BronzeChest, -255f, y);
        CreateSmallChestCard(section, ShopCatalog.RoyalChest, 255f, y);
        y += 580f;

        EndSection(section, y);
    }

    void BuildWish()
    {
        RectTransform section = BeginSection("Wish", out float y);

        string luck = ShopCatalog.Colored("Epic", GearCatalog.RarityColor(Rarity.Epic)) + " artifact within " + Artifacts.WishesUntilEpic + " wishes\n"
            + ShopCatalog.Colored("Legendary", GearCatalog.RarityColor(Rarity.Legendary)) + " artifact within " + Artifacts.WishesUntilLegendary + " wishes";

        Transform card = CreateBigCard(section, y, "Wish", "WISH", "Find a random artifact.\nOnes you have give shards.", luck, purpleCardColor, ShowWishInfo);

        int price = ArtifactCatalog.WishPrice;
        CreateActionButton(card, "WISH x1\n<size=34>" + price + " gems</size>", -245f, () => Wish(1));
        CreateActionButton(card, "WISH x10\n<size=34>" + price * 10 + " gems</size>", 245f, () => Wish(10));

        EndSection(section, y + 540f);
    }

    // A section of packs in rows of three. A period other than Unlimited shows when they come back.
    void BuildPackSection(string title, ShopOffer[] packs, ShopPeriod period, Color color)
    {
        RectTransform section = BeginSection(title, out float y);
        if (period != ShopPeriod.Unlimited) y = AddTimer(section, y, period);
        y = AddCards(section, packs, y, color);
        EndSection(section, y);
    }

    void BuildCrystals()
    {
        RectTransform section = BeginSection("Crystals", out float y);
        y = AddTimer(section, y, ShopPeriod.Monthly);
        y = AddLine(section, "Crystals can pay for any paid offer in the shop", y);
        y = AddLine(section, "You have " + Wallet.Crystals + " crystals", y);
        y = AddCards(section, ShopCatalog.CrystalPacks, y, crystalCardColor);
        EndSection(section, y);
    }

    // A panel with a title bar, at the cursor. 'y' = where the first thing inside it goes.
    RectTransform BeginSection(string title, out float y)
    {
        RectTransform section = UiBuilder.CreateBox(title, content, panelColor, Vector2.zero, new Vector2(SectionWidth, 0f)).rectTransform;
        PinTop(section, 0f, cursor);

        Image banner = UiBuilder.CreateBox("Banner", section, bannerColor, Vector2.zero, new Vector2(SectionWidth - 100f, 90f));
        PinTop(banner.rectTransform, 0f, 20f);
        Text text = UiBuilder.CreateText("Title", banner.transform, title, 56, Vector2.zero, new Vector2(SectionWidth - 100f, 90f));
        text.gameObject.AddComponent<Outline>().effectColor = Color.black;

        y = 130f;
        return section;
    }

    // Now that we know how tall the section is: set its height and move the cursor below it
    void EndSection(RectTransform section, float height)
    {
        section.sizeDelta = new Vector2(SectionWidth, height + 20f);
        cursor += height + 20f + SectionGap;
    }

    float AddLine(Transform section, string text, float y)
    {
        Text line = UiBuilder.CreateText("Line", section, text, 36, Vector2.zero, new Vector2(SectionWidth - 40f, 50f));
        line.color = infoTextColor;
        PinTop(line.rectTransform, 0f, y);
        return y + 55f;
    }

    // "Refresh in ..." that counts down
    float AddTimer(Transform section, float y, ShopPeriod period)
    {
        Text text = UiBuilder.CreateText("Timer", section, "", 36, Vector2.zero, new Vector2(SectionWidth - 40f, 50f));
        text.color = infoTextColor;
        PinTop(text.rectTransform, 0f, y);
        AddCountdown(text, () => "Refresh in " + Shop.FormatTime(Shop.TimeUntilRefresh(period)));
        return y + 60f;
    }

    void AddCountdown(Text text, Func<string> makeText)
    {
        text.text = makeText();
        timers.Add(new Timer { text = text, makeText = makeText });
    }

    // Offer cards in rows of three. Returns the y below the last row.
    float AddCards(Transform section, IList<ShopOffer> offers, float y, Color color)
    {
        for (int i = 0; i < offers.Count; i++)
        {
            int column = i % 3; // % = what is left after dividing: 0, 1, 2, 0, 1, 2...
            int row = i / 3;    // whole numbers only: 0, 0, 0, 1, 1, 1...
            CreateOfferCard(section, offers[i], (column - 1) * CardStepX, y + row * CardStepY, color);
        }

        int rows = (offers.Count + 2) / 3; // 4 offers = 2 rows
        return y + rows * CardStepY;
    }

    // ---------- Cards ----------

    // One offer: badges on top, the rewards, "Purchases left" and the price button at the bottom
    void CreateOfferCard(Transform parent, ShopOffer offer, float x, float top, Color color)
    {
        bool soldOut = Shop.IsSoldOut(offer);

        Image card = UiBuilder.CreateBox(offer.id, parent, soldOut ? soldOutColor : color, Vector2.zero, new Vector2(CardWidth, CardHeight));
        PinTop(card.rectTransform, x, top);
        Transform t = card.transform;

        // One reward = one big tile (with the title above it), more rewards = a 2 x 2 grid of small tiles
        if (offer.rewards.Length == 1)
        {
            if (offer.title != "") CreateOutlinedText(t, offer.title, 36, new Vector2(0f, 165f), new Vector2(CardWidth - 20f, 70f));
            CreateTile(t, offer.rewards[0], new Vector2(0f, 30f), 170f);
        }
        else
        {
            for (int i = 0; i < offer.rewards.Length && i < 4; i++)
            {
                float tileX = i % 2 == 0 ? -70f : 70f;
                float tileY = i < 2 ? 110f : -22f;
                CreateTile(t, offer.rewards[i], new Vector2(tileX, tileY), 125f);
            }
        }

        if (offer.valuePercent > 0) CreateBadge(t, offer.valuePercent + "%\nValue", valueBadgeColor, new Vector2(100f, 222f), new Vector2(140f, 70f));
        if (offer.discountPercent > 0) CreateBadge(t, offer.discountPercent + "%\nOff", discountBadgeColor, new Vector2(-115f, 222f), new Vector2(110f, 70f));
        if (Shop.HasFirstTimeBonus(offer)) CreateBadge(t, "Bonus: " + offer.rewards[0].amount, bonusBadgeColor, new Vector2(70f, 228f), new Vector2(200f, 50f));

        if (Shop.HasLimit(offer) && !soldOut)
        {
            CreateOutlinedText(t, "Purchases left: " + Shop.PurchasesLeft(offer), 30, new Vector2(0f, -110f), new Vector2(CardWidth, 50f));
        }

        CreatePriceButton(t, offer, soldOut, new Vector2(0f, -175f), new Vector2(290f, 90f));
    }

    // A wide card in the chapter slider
    void CreateChapterCard(Transform parent, ShopOffer offer, int chapter, Vector2 position)
    {
        bool soldOut = Shop.IsSoldOut(offer);
        Image card = UiBuilder.CreateBox(offer.id, parent, soldOut ? soldOutColor : goldCardColor, position, new Vector2(920f, 380f));
        Transform t = card.transform;

        // Measure 'position' from the LEFT edge of the row (not its middle), like the row itself
        card.rectTransform.anchorMin = card.rectTransform.anchorMax = new Vector2(0f, 0.5f);

        // A placeholder picture: a darker box with the chapter number
        Image picture = UiBuilder.CreateBox("Picture", t, Color.Lerp(goldCardColor, Color.black, 0.35f), new Vector2(-260f, 0f), new Vector2(340f, 320f));
        picture.raycastTarget = false; // drags go through to the slider
        UiBuilder.CreateText("Chapter", picture.transform, "CH.\n" + chapter, 90, Vector2.zero, new Vector2(340f, 320f));

        CreateOutlinedText(t, offer.title, 58, new Vector2(200f, 140f), new Vector2(500f, 80f));
        Image pill = UiBuilder.CreateBox("OneTime", t, new Color(0f, 0f, 0f, 0.3f), new Vector2(200f, 75f), new Vector2(360f, 50f));
        pill.raycastTarget = false;
        UiBuilder.CreateText("Text", pill.transform, "One-time only", 32, Vector2.zero, new Vector2(360f, 50f));

        for (int i = 0; i < offer.rewards.Length; i++)
        {
            CreateTile(t, offer.rewards[i], new Vector2(200f + (i - 1.5f) * 115f, -25f), 105f);
        }

        CreateBadge(t, offer.valuePercent + "%\nValue", valueBadgeColor, new Vector2(-390f, 160f), new Vector2(150f, 75f));
        CreatePriceButton(t, offer, soldOut, new Vector2(200f, -135f), new Vector2(420f, 90f));
    }

    // A wide chest card: picture, title, chances, keys and the DRAW x1 / x10 buttons. Returns the y below it.
    float CreateChestCard(Transform section, ChestType chest, float y)
    {
        string luck = "";
        if (chest.epicWithin > 0) luck += ShopCatalog.Colored("Epic", GearCatalog.RarityColor(Rarity.Epic)) + " gear within " + Shop.OpensUntilEpic(chest) + " opens\n";
        if (chest.legendaryWithin > 0) luck += ShopCatalog.Colored("Legendary", GearCatalog.RarityColor(Rarity.Legendary)) + " gear within " + Shop.OpensUntilLegendary(chest) + " opens\n";
        int keys = Shop.Keys(chest);
        if (keys > 0) luck += "Keys: " + keys;

        Transform card = CreateBigCard(section, y, chest.title, chest.title.Split(' ')[0].ToUpper(), chest.description, luck, chest.color, () => ShowChestInfo(chest));

        string price1 = keys >= 1 ? "1 key" : chest.price + " gems";
        string price10 = keys >= 10 ? "10 keys" : chest.price * 10 + " gems";
        CreateActionButton(card, "DRAW x1\n<size=34>" + price1 + "</size>", -245f, () => OpenChest(chest, 1));
        CreateActionButton(card, "OPEN x10\n<size=34>" + price10 + "</size>", 245f, () => OpenChest(chest, 10));

        return y + 540f;
    }

    // A wide card with a picture on the left and text on the right. The caller adds the buttons.
    Transform CreateBigCard(Transform section, float top, string title, string letters, string description, string luck, Color color, UnityAction onInfo)
    {
        Image card = UiBuilder.CreateBox(title, section, color, Vector2.zero, new Vector2(1000f, 520f));
        PinTop(card.rectTransform, 0f, top);
        Transform t = card.transform;

        Image picture = UiBuilder.CreateBox("Picture", t, Color.Lerp(color, Color.black, 0.35f), new Vector2(-320f, 70f), new Vector2(300f, 280f));
        picture.raycastTarget = false;
        UiBuilder.CreateText("Letters", picture.transform, letters, 64, Vector2.zero, new Vector2(300f, 280f));

        CreateOutlinedText(t, title, 62, new Vector2(160f, 205f), new Vector2(620f, 80f));
        UiBuilder.CreateText("Description", t, description, 32, new Vector2(160f, 120f), new Vector2(640f, 90f));
        UiBuilder.CreateText("Luck", t, luck, 32, new Vector2(160f, 15f), new Vector2(640f, 120f));

        Text info = UiBuilder.CreateButton("Info", t, "i", new Vector2(450f, 210f), new Vector2(70f, 70f), Color.white, onInfo);
        info.fontSize = 50;
        info.color = priceTextColor;

        return t;
    }

    // The small chests have a free open every few hours
    void CreateSmallChestCard(Transform section, ChestType chest, float x, float top)
    {
        Image card = UiBuilder.CreateBox(chest.title, section, chest.color, Vector2.zero, new Vector2(490f, 560f));
        PinTop(card.rectTransform, x, top);
        Transform t = card.transform;

        CreateOutlinedText(t, chest.title, 48, new Vector2(0f, 235f), new Vector2(470f, 70f));
        UiBuilder.CreateText("Description", t, chest.description, 30, new Vector2(0f, 175f), new Vector2(460f, 60f));

        Image picture = UiBuilder.CreateBox("Picture", t, Color.Lerp(chest.color, Color.black, 0.35f), new Vector2(0f, 35f), new Vector2(220f, 190f));
        picture.raycastTarget = false;
        UiBuilder.CreateText("Letters", picture.transform, chest.title.Split(' ')[0].ToUpper(), 44, Vector2.zero, new Vector2(220f, 190f));

        Text info = UiBuilder.CreateButton("Info", t, "i", new Vector2(200f, 235f), new Vector2(60f, 60f), Color.white, () => ShowChestInfo(chest));
        info.fontSize = 44;
        info.color = priceTextColor;

        // "Free in 05:19:11", counting down. When it reaches zero, Update builds the shop again (see StateKey).
        bool free = Shop.IsFree(chest);
        Text status = UiBuilder.CreateText("Status", t, "", 32, new Vector2(0f, -100f), new Vector2(470f, 50f));
        if (free) status.text = "Free now!";
        else AddCountdown(status, () => "Free in " + Shop.FormatTime(Shop.FreeOpenIn(chest)));

        int keys = Shop.Keys(chest);
        if (keys > 0) UiBuilder.CreateText("Keys", t, "Keys: " + keys, 30, new Vector2(0f, -145f), new Vector2(470f, 45f));

        string label = free ? "OPEN FREE" : keys > 0 ? "OPEN\n<size=30>1 key</size>" : "OPEN x1\n<size=30>" + chest.price + " gems</size>";
        Text button = UiBuilder.CreateButton("Open", t, label, new Vector2(0f, -220f), new Vector2(420f, 100f), actionButtonColor, () => OpenChest(chest, 1));
        button.fontSize = 42;
    }

    // ---------- Small parts ----------

    // A square that shows one reward: its kind in the middle, the amount in the corner
    void CreateTile(Transform parent, Reward reward, Vector2 position, float size)
    {
        Image tile = UiBuilder.CreateBox("Tile", parent, reward.TileColor(), position, new Vector2(size, size));
        tile.raycastTarget = false;

        UiBuilder.CreateText("Symbol", tile.transform, reward.Symbol(), Mathf.RoundToInt(size * 0.17f), new Vector2(0f, size * 0.08f), new Vector2(size - 8f, size * 0.7f));

        Text amount = CreateOutlinedText(tile.transform, reward.ShortAmount(), Mathf.RoundToInt(size * 0.22f), new Vector2(0f, -size * 0.33f), new Vector2(size - 14f, size * 0.32f));
        amount.alignment = TextAnchor.MiddleRight;
    }

    void CreateBadge(Transform parent, string text, Color color, Vector2 position, Vector2 size)
    {
        Image badge = UiBuilder.CreateBox("Badge", parent, color, position, size);
        badge.raycastTarget = false;
        CreateOutlinedText(badge.transform, text, 26, Vector2.zero, size);
    }

    void CreatePriceButton(Transform parent, ShopOffer offer, bool soldOut, Vector2 position, Vector2 size)
    {
        string price;
        if (soldOut) price = "SOLD OUT";
        else if (offer.priceType == PriceType.Free) price = "FREE";
        else if (offer.priceType == PriceType.Gems) price = offer.price + " gems";
        else if (offer.priceType == PriceType.Coins) price = offer.price + " coins";
        else price = Shop.FormatEuros(offer.price);

        Text label = UiBuilder.CreateButton("Price", parent, price, position, size, soldOut ? soldOutColor : priceButtonColor, () => Buy(offer));
        label.fontSize = 44;
        label.color = soldOut ? Color.white : priceTextColor;
    }

    // The two green buttons at the bottom of a big card
    void CreateActionButton(Transform card, string label, float x, UnityAction onClick)
    {
        Text text = UiBuilder.CreateButton("Action", card, label, new Vector2(x, -180f), new Vector2(440f, 120f), actionButtonColor, onClick);
        text.fontSize = 50;
    }

    Text CreateOutlinedText(Transform parent, string content, int size, Vector2 position, Vector2 box)
    {
        Text text = UiBuilder.CreateText("Text", parent, content, size, position, box);
        text.gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.8f);
        return text;
    }

    // Puts 'rect' so that its top edge is 'top' units below the top of its parent, 'x' from the middle
    static void PinTop(RectTransform rect, float x, float top)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(x, -top);
    }

    // ---------- Shop tabs ----------

    void BuildTabs(Transform page)
    {
        tabs = new Image[TabNames.Length];
        for (int i = 0; i < TabNames.Length; i++)
        {
            int tab = i;
            Text label = UiBuilder.CreateButton("ShopTab" + i, page, TabNames[i], Vector2.zero, new Vector2(204f, 105f), tabColor, () => JumpTo(tabTops[tab]));
            label.fontSize = 34;
            tabs[i] = label.transform.parent.GetComponent<Image>();
            PinBottom(tabs[i].rectTransform, (i - 2) * 212f, 8f);
        }

        // The second row, for Limited Offer and Top Up. It floats over the bottom of the list.
        Image row = UiBuilder.CreateBox("SubTabs", page, selectedTabColor, Vector2.zero, new Vector2(SectionWidth, 110f));
        PinBottom(row.rectTransform, 0f, TabBarHeight + 5f);
        subTabRow = row.gameObject;
        UiBuilder.CreateBox("Inside", row.transform, new Color(0.09f, 0.11f, 0.18f), Vector2.zero, new Vector2(SectionWidth - 10f, 100f));

        for (int i = 0; i < 3; i++)
        {
            int index = i;
            subTabLabels[i] = UiBuilder.CreateButton("SubTab" + i, row.transform, "", new Vector2((i - 1) * 340f, 0f), new Vector2(320f, 80f), tabColor, () => JumpToSubTab(index));
            subTabLabels[i].fontSize = 36;
            subTabs[i] = subTabLabels[i].transform.parent.GetComponent<Image>();
        }
        subTabRow.SetActive(false);
    }

    static void PinBottom(RectTransform rect, float x, float bottom)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(x, bottom);
    }

    void JumpToSubTab(int index)
    {
        JumpTo(subRowShown == 0 ? limitedTops[index] : topUpTops[index]);
    }

    // Scrolls so that this section is at the top (or as far as the list can go)
    void JumpTo(float top)
    {
        list.StopMovement();
        float maxScroll = Mathf.Max(0f, content.rect.height - list.viewport.rect.height);
        content.anchoredPosition = new Vector2(0f, Mathf.Clamp(top - 10f, 0f, maxScroll));
    }

    // Lights up the tab of the section that fills the upper part of the screen
    void UpdateTabs()
    {
        float lookAt = content.anchoredPosition.y + list.viewport.rect.height * 0.3f;

        int tab = SectionAt(tabTops, lookAt);
        for (int i = 0; i < tabs.Length; i++) SetTabColor(tabs[i], i == tab);

        int row = tab == LimitedTab ? 0 : tab == TopUpTab ? 1 : -1;
        if (row != subRowShown)
        {
            subRowShown = row;
            subTabRow.SetActive(row >= 0);
            string[] names = row == 0 ? LimitedTabNames : TopUpTabNames;
            for (int i = 0; i < 3; i++) subTabLabels[i].text = names[i];
        }
        if (row < 0) return;

        int subTab = SectionAt(row == 0 ? limitedTops : topUpTops, lookAt);
        for (int i = 0; i < 3; i++) SetTabColor(subTabs[i], i == subTab);
    }

    void SetTabColor(Image tab, bool selected)
    {
        tab.color = selected ? selectedTabColor : tabColor;
        tab.GetComponentInChildren<Text>().color = selected ? priceTextColor : Color.white;
    }

    // The last section that starts above 'y'
    static int SectionAt(float[] tops, float y)
    {
        int result = 0;
        for (int i = 0; i < tops.Length; i++)
        {
            if (y >= tops[i]) result = i;
        }
        return result;
    }

    // ---------- Buying ----------

    void Buy(ShopOffer offer)
    {
        if (Shop.IsSoldOut(offer)) return;

        List<string> lines = new List<string>();
        if (!Shop.TryBuy(offer, lines, out string problem))
        {
            ShowProblem(problem);
            return;
        }

        SoundManager.Play(Sound.WheelPrize);
        ShowResults("YOU GOT", lines, null, null);
    }

    void OpenChest(ChestType chest, int count)
    {
        List<OwnedGear> items = new List<OwnedGear>();
        int focus = chest.hasFocus ? heroFocus : -1;
        if (!Shop.TryOpenChest(chest, count, focus, items, out string problem))
        {
            ShowProblem(problem);
            return;
        }

        List<string> lines = new List<string>();
        foreach (OwnedGear item in items)
        {
            lines.Add(ShopCatalog.Colored(GearCatalog.RarityName(item.rarity) + " " + Inventory.TypeOf(item).title, GearCatalog.RarityColor(item.rarity)));
        }

        SoundManager.Play(Sound.WheelPrize);
        ShowResults(chest.title.ToUpper(), lines, count == 1 ? "AGAIN" : "AGAIN x10", () => OpenChest(chest, count));
    }

    void Wish(int count)
    {
        List<Artifacts.WishResult> results = new List<Artifacts.WishResult>();
        if (!Artifacts.TryWish(count, results))
        {
            ShowProblem("Not enough gems");
            return;
        }

        List<string> lines = new List<string>();
        foreach (Artifacts.WishResult result in results)
        {
            string name = ShopCatalog.Colored(result.artifact.title, GearCatalog.RarityColor(result.artifact.rarity));
            lines.Add(result.shards == 0 ? "NEW! " + name : name + " +" + result.shards + " shards");
        }

        SoundManager.Play(Sound.WheelPrize);
        ShowResults("WISH", lines, count == 1 ? "AGAIN" : "AGAIN x10", () => Wish(count));
    }

    void ChooseFocus(int focus)
    {
        heroFocus = focus;
        rebuildNeeded = true; // to color the tabs
    }

    // ---------- Popups ----------

    // "YOU GOT" + one line per reward. 'againLabel' + 'again' add a second button (null = only OK).
    void ShowResults(string title, List<string> lines, string againLabel, UnityAction again)
    {
        float height = 330f + lines.Count * 55f;
        Transform window = OpenPopup("Results", new Vector2(860f, height));

        CreateOutlinedText(window, title, 70, new Vector2(0f, height / 2f - 75f), new Vector2(840f, 100f)).color = selectedTabColor;
        UiBuilder.CreateText("Lines", window, string.Join("\n", lines), 40, new Vector2(0f, 20f), new Vector2(820f, lines.Count * 55f));

        float buttonY = -height / 2f + 90f;
        if (again == null)
        {
            UiBuilder.CreateButton("Ok", window, "OK", new Vector2(0f, buttonY), new Vector2(400f, 120f), actionButtonColor, ClosePopup);
            return;
        }
        UiBuilder.CreateButton("Again", window, againLabel, new Vector2(-200f, buttonY), new Vector2(370f, 120f), bannerColor, again).fontSize = 50;
        UiBuilder.CreateButton("Ok", window, "OK", new Vector2(200f, buttonY), new Vector2(370f, 120f), actionButtonColor, ClosePopup);
    }

    // "Not enough gems" - with a button that goes to the gem packs
    void ShowProblem(string problem)
    {
        Transform window = OpenPopup("Problem", new Vector2(860f, 420f));
        UiBuilder.CreateText("Message", window, problem, 60, new Vector2(0f, 80f), new Vector2(820f, 120f));

        if (problem == "Not enough gems")
        {
            UiBuilder.CreateButton("GetGems", window, "GET GEMS", new Vector2(-200f, -100f), new Vector2(370f, 120f), bannerColor, () =>
            {
                ClosePopup();
                JumpTo(topUpTops[0]);
            }).fontSize = 50;
            UiBuilder.CreateButton("Ok", window, "OK", new Vector2(200f, -100f), new Vector2(370f, 120f), actionButtonColor, ClosePopup);
            return;
        }
        UiBuilder.CreateButton("Ok", window, "OK", new Vector2(0f, -100f), new Vector2(400f, 120f), actionButtonColor, ClosePopup);
    }

    void ShowChestInfo(ChestType chest)
    {
        List<string> lines = new List<string>();
        for (int i = 0; i < chest.chance.Length; i++)
        {
            if (chest.chance[i] > 0) lines.Add(RarityLine((Rarity)i, chest.chance[i]));
        }
        if (chest.epicWithin > 0) lines.Add("Epic or better at least every " + chest.epicWithin + " opens");
        if (chest.legendaryWithin > 0) lines.Add("Legendary at least every " + chest.legendaryWithin + " opens");
        if (chest.freeHours > 0) lines.Add("Free once every " + chest.freeHours + " hours");
        if (chest.hasFocus) lines.Add("Half of the items come from the FOCUS slots");

        ShowResults(chest.title.ToUpper(), lines, null, null);
    }

    void ShowWishInfo()
    {
        List<string> lines = new List<string>();
        for (int i = 0; i < ArtifactCatalog.WishChance.Length; i++)
        {
            if (ArtifactCatalog.WishChance[i] > 0) lines.Add(RarityLine((Rarity)i, ArtifactCatalog.WishChance[i]));
        }
        lines.Add("Epic or better at least every " + ArtifactCatalog.EpicWishPity + " wishes");
        lines.Add("Legendary at least every " + ArtifactCatalog.LegendaryWishPity + " wishes");
        lines.Add("An artifact you have gives shards");

        ShowResults("WISH", lines, null, null);
    }

    static string RarityLine(Rarity rarity, int percent)
    {
        return ShopCatalog.Colored(GearCatalog.RarityName(rarity), GearCatalog.RarityColor(rarity)) + ": " + percent + "%";
    }

    // A dark screen with a window in the middle. Tapping outside the window closes it (as in ArtifactScreen).
    Transform OpenPopup(string name, Vector2 size)
    {
        ClosePopup();

        popup = UiBuilder.CreateFullScreenPanel(name, overlayParent, new Color(0f, 0f, 0f, 0.75f));
        GameObject closeArea = UiBuilder.CreateFullScreenPanel("CloseArea", popup.transform, Color.clear);
        closeArea.AddComponent<Button>().onClick.AddListener(ClosePopup);

        return UiBuilder.CreateBox("Window", popup.transform, windowColor, Vector2.zero, size).transform;
    }

    void ClosePopup()
    {
        if (popup != null) Destroy(popup);
    }
}
