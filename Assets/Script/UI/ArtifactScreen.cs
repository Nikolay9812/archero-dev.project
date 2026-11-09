using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// The Artifact tab of the Collectible page:
//   top    - DETAILS (the total of all artifacts) and a filter button (ALL / LEGENDARY / EPIC / RARE)
//   middle - every artifact in a grid; the ones not found yet are dark
//   bottom - the wild shards and the WISH button (gems for a random artifact)
// Tap an artifact to see it and give it a star.
// CollectibleScreen adds this component by code and calls Build(), so there is nothing to set up in the scene.
public class ArtifactScreen : MonoBehaviour
{
    public Color notFoundColor = new Color(0.22f, 0.22f, 0.3f);
    public Color windowColor = new Color(0.13f, 0.16f, 0.24f);
    public Color sectionColor = new Color(0.22f, 0.26f, 0.38f);
    public Color goodButtonColor = new Color(0.3f, 0.7f, 0.3f);
    public Color unavailableColor = new Color(0.4f, 0.4f, 0.45f);
    public Color wishColor = new Color(0.35f, 0.45f, 0.95f);
    public Color starColor = new Color(1f, 0.8f, 0.15f);
    public Color emptyStarColor = new Color(0.1f, 0.1f, 0.15f);

    private const int Columns = 4;
    private static readonly Vector2 CellSize = new Vector2(245f, 300f);

    // Rich text colors: Unity's Text understands <color=#rrggbb>...</color> inside the text
    private const string Green = "#7CFC7C";
    private const string Red = "#FF6060";

    // The filter button goes through these. null = show all.
    private static readonly Rarity?[] Filters = { null, Rarity.Legendary, Rarity.Epic, Rarity.Rare };
    private int filterIndex;

    private ScrollRect grid;    // CODEX: every artifact
    private ScrollRect setList; // SET: one card per set
    private bool showSets;
    private Image codexTab;
    private Image setTab;
    private Text foundText;
    private Text filterLabel;
    private Text wildShardsText;
    private Image wishButton;
    private Transform overlayParent; // the whole canvas, so popups cover the header and the tabs too
    private GameObject popup;
    private bool built;

    public void Build(Transform panel)
    {
        overlayParent = panel.GetComponentInParent<Canvas>().transform;

        // The top row, just under the TALENT CARDS / ARTIFACT tabs
        Text details = UiBuilder.CreateButton("DetailsButton", panel, "DETAILS", new Vector2(-380f, 570f), new Vector2(280f, 90f), sectionColor, ShowDetails);
        details.fontSize = 40;
        foundText = UiBuilder.CreateText("Found", panel, "", 38, new Vector2(0f, 570f), new Vector2(400f, 90f));
        filterLabel = UiBuilder.CreateButton("FilterButton", panel, "", new Vector2(380f, 570f), new Vector2(280f, 90f), sectionColor, NextFilter);
        filterLabel.fontSize = 40;

        // The grid and the set list: the same fixed box in the middle, only one is shown at a time.
        // Both scroll if there is more than fits.
        grid = UiBuilder.CreateScrollGrid("ArtifactGrid", panel, CellSize, 15f, Columns);
        PlaceInMiddle(grid);
        setList = UiBuilder.CreateScrollGrid("SetList", panel, new Vector2(1020f, 430f), 20f, 1);
        PlaceInMiddle(setList);

        // CODEX / SET switch under the box
        Vector2 switchSize = new Vector2(260f, 80f);
        Text codexLabel = UiBuilder.CreateButton("CodexTab", panel, "CODEX", new Vector2(-130f, -540f), switchSize, sectionColor, () => ShowSets(false));
        Text setLabel = UiBuilder.CreateButton("SetTab", panel, "SET", new Vector2(130f, -540f), switchSize, sectionColor, () => ShowSets(true));
        codexLabel.fontSize = setLabel.fontSize = 40;
        codexTab = codexLabel.transform.parent.GetComponent<Image>();
        setTab = setLabel.transform.parent.GetComponent<Image>();

        // The bottom row
        wildShardsText = UiBuilder.CreateText("WildShards", panel, "", 38, new Vector2(-260f, -660f), new Vector2(500f, 120f));
        Text wish = UiBuilder.CreateButton("WishButton", panel, "WISH\n" + ArtifactCatalog.WishPrice + " gems", new Vector2(300f, -660f), new Vector2(420f, 150f), wishColor, Wish);
        wish.fontSize = 48;
        wishButton = wish.transform.parent.GetComponent<Image>();

        built = true;
        Refresh();
    }

    void OnEnable()
    {
        Artifacts.Changed += Refresh;
        Wallet.Changed += Refresh; // gems spent somewhere else change the color of the WISH button
    }

    void OnDisable()
    {
        Artifacts.Changed -= Refresh;
        Wallet.Changed -= Refresh;
    }

    // Redraws the page from the save data (called after every change)
    void Refresh()
    {
        if (!built) return;

        foundText.text = "Found " + Artifacts.FoundCount + " / " + ArtifactCatalog.All.Length;
        filterLabel.text = Filters[filterIndex] == null ? "ALL" : GearCatalog.RarityName(Filters[filterIndex].Value);
        wildShardsText.text = "Wild shards: " + Artifacts.WildShards + "\n<size=28>count for any artifact</size>";
        wishButton.color = Wallet.Gems >= ArtifactCatalog.WishPrice ? wishColor : unavailableColor;

        // Only one of the two views is visible. The filter is for the codex only.
        grid.gameObject.SetActive(!showSets);
        setList.gameObject.SetActive(showSets);
        filterLabel.transform.parent.gameObject.SetActive(!showSets);
        codexTab.color = showSets ? sectionColor : wishColor;
        setTab.color = showSets ? wishColor : sectionColor;

        RefreshSets();

        UiBuilder.ClearChildren(grid.content);
        foreach (ArtifactType type in ArtifactCatalog.All)
        {
            // Filters[filterIndex] is null for ALL; "!=" on a nullable compares the value inside
            if (Filters[filterIndex] != null && type.rarity != Filters[filterIndex]) continue;

            ArtifactType tapped = type; // a copy for the lambda (same reason as in GearScreen)
            CreateCell(grid.content, type, () => ShowArtifact(tapped));
        }
    }

    void NextFilter()
    {
        filterIndex = (filterIndex + 1) % Filters.Length; // % makes it go back to 0 after the last one
        Refresh();
    }

    void ShowSets(bool sets)
    {
        showSets = sets;
        Refresh();
    }

    // Centers a scroll box in the page with a fixed size (CreateScrollGrid makes it fill the parent)
    static void PlaceInMiddle(ScrollRect scroll)
    {
        RectTransform rect = scroll.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(1040f, 970f);
        rect.anchoredPosition = new Vector2(0f, 15f);
    }

    // ---------- The SET view ----------

    void RefreshSets()
    {
        UiBuilder.ClearChildren(setList.content);
        foreach (ArtifactSet set in ArtifactSets.All) CreateSetCard(setList.content, set);
    }

    // One set: its name, its 3 artifacts on the left, its 3 bonus tiers on the right
    void CreateSetCard(Transform parent, ArtifactSet set)
    {
        Image card = UiBuilder.CreateBox(set.title, parent, sectionColor, Vector2.zero, Vector2.zero); // the grid sets the size
        Transform cardTransform = card.transform;

        Text title = UiBuilder.CreateText("Title", cardTransform, set.title, 46, new Vector2(0f, 170f), new Vector2(960f, 70f));
        title.alignment = TextAnchor.MiddleLeft;
        Text count = UiBuilder.CreateText("Count", cardTransform, "Found " + ArtifactSets.FoundCount(set) + " / " + set.artifactIds.Length, 36, new Vector2(0f, 170f), new Vector2(960f, 70f));
        count.alignment = TextAnchor.MiddleRight;

        // The artifacts, small. Tap one to open its window.
        ArtifactType[] members = ArtifactSets.Members(set);
        for (int i = 0; i < members.Length; i++)
        {
            ArtifactType type = members[i];
            CreateMiniCell(cardTransform, type, new Vector2(-400f + i * 155f, -30f));
        }

        // The tiers: bright when active, dark when not yet
        for (int tier = 0; tier < set.tiers.Length; tier++)
        {
            bool active = ArtifactSets.IsTierActive(set, tier);
            SetBonus bonus = set.tiers[tier];

            Image row = UiBuilder.CreateBox("Tier" + tier, cardTransform, active ? new Color(0.25f, 0.5f, 0.3f) : windowColor, new Vector2(255f, 70f - tier * 100f), new Vector2(480f, 88f));
            row.raycastTarget = false; // drags go through to the list
            string line = ArtifactSets.Requirement(tier) + "\n<size=34>" + ArtifactCatalog.Format(bonus.stat, bonus.value) + "</size>";
            Text text = UiBuilder.CreateText("Text", row.transform, line, 28, Vector2.zero, new Vector2(460f, 88f));
            text.color = active ? Color.white : new Color(1f, 1f, 1f, 0.45f);
        }
    }

    // A small artifact square for the set cards: letters, stars and name
    void CreateMiniCell(Transform parent, ArtifactType type, Vector2 position)
    {
        bool found = Artifacts.IsFound(type);
        Vector2 size = new Vector2(140f, 250f);

        Text label = UiBuilder.CreateButton(type.id, parent, "", position, size, found ? GearCatalog.RarityColor(type.rarity) : notFoundColor, () => ShowArtifact(type));
        Transform cell = label.transform.parent;

        UiBuilder.CreateText("Letters", cell, found ? Initials(type.title) : "?", 56, new Vector2(0f, 50f), new Vector2(140f, 100f));
        CreateStars(cell, Artifacts.Stars(type), new Vector2(0f, -25f), 16f, 25f);
        UiBuilder.CreateText("Title", cell, type.title, 24, new Vector2(0f, -85f), new Vector2(135f, 70f));
    }

    // ---------- The grid ----------

    void CreateCell(Transform parent, ArtifactType type, UnityAction onTap)
    {
        bool found = Artifacts.IsFound(type);
        Color rarityColor = GearCatalog.RarityColor(type.rarity);

        Text label = UiBuilder.CreateButton(type.id, parent, "", Vector2.zero, CellSize, found ? Color.Lerp(rarityColor, Color.black, 0.25f) : notFoundColor, onTap);
        Transform cell = label.transform.parent;

        // The "picture": a darker square with the first letters of the name. Not found yet = "?"
        Image icon = UiBuilder.CreateBox("Icon", cell, found ? rarityColor : new Color(0.15f, 0.15f, 0.2f), new Vector2(0f, 50f), new Vector2(170f, 170f));
        icon.raycastTarget = false; // taps go through to the button
        Text letters = UiBuilder.CreateText("Letters", icon.transform, found ? Initials(type.title) : "?", 70, Vector2.zero, new Vector2(170f, 170f));
        letters.color = found ? Color.white : new Color(1f, 1f, 1f, 0.3f);

        CreateStars(cell, Artifacts.Stars(type), new Vector2(0f, -55f), 24f, 36f);

        Text title = UiBuilder.CreateText("Title", cell, type.title, 30, new Vector2(0f, -110f), new Vector2(CellSize.x - 10f, 60f));
        title.color = found ? Color.white : new Color(1f, 1f, 1f, 0.5f);

        // Shard progress on the picture, and a green tag when a star can be added right now
        if (found && !Artifacts.IsMaxStars(type))
        {
            int cost = ArtifactCatalog.StarCost(Artifacts.Stars(type));
            Text progress = UiBuilder.CreateText("Shards", icon.transform, Artifacts.Shards(type) + "/" + cost, 28, new Vector2(0f, -65f), new Vector2(170f, 40f));
            progress.gameObject.AddComponent<Outline>().effectColor = Color.black;
        }
        if (Artifacts.CanStarUp(type))
        {
            Image tag = UiBuilder.CreateBox("UpTag", cell, goodButtonColor, new Vector2(85f, 130f), new Vector2(80f, 44f));
            tag.raycastTarget = false;
            UiBuilder.CreateText("Up", tag.transform, "UP!", 28, Vector2.zero, new Vector2(80f, 44f));
        }
    }

    // A row of stars: small squares turned 45 degrees, like on the talent cards
    void CreateStars(Transform parent, int filled, Vector2 center, float size, float spacing)
    {
        for (int i = 0; i < ArtifactCatalog.MaxStars; i++)
        {
            float x = center.x + (i - (ArtifactCatalog.MaxStars - 1) / 2f) * spacing;
            Image star = UiBuilder.CreateBox("Star" + i, parent, i < filled ? starColor : emptyStarColor, new Vector2(x, center.y), new Vector2(size, size));
            star.rectTransform.localEulerAngles = new Vector3(0f, 0f, 45f);
            star.raycastTarget = false;
        }
    }

    // "Giant's Tooth" -> "GT"
    static string Initials(string title)
    {
        string result = "";
        foreach (string word in title.Split(' ')) // Split cuts the text into words at every space
        {
            if (word.Length > 0) result += word[0];
        }
        return result.ToUpper();
    }

    // ---------- The artifact window ----------

    void ShowArtifact(ArtifactType type)
    {
        bool found = Artifacts.IsFound(type);
        int stars = Artifacts.Stars(type);
        bool maxed = Artifacts.IsMaxStars(type);
        Color rarityColor = GearCatalog.RarityColor(type.rarity);

        Transform window = OpenPopup("ArtifactPopup", new Vector2(920f, 1360f));

        Image header = UiBuilder.CreateBox("Header", window, rarityColor, new Vector2(0f, 590f), new Vector2(920f, 180f));
        UiBuilder.CreateText("Title", header.transform, type.title, 64, new Vector2(0f, 25f), new Vector2(900f, 90f));
        UiBuilder.CreateText("Rarity", header.transform, GearCatalog.RarityName(type.rarity), 36, new Vector2(0f, -50f), new Vector2(900f, 50f));

        // Picture + description side by side
        Image icon = UiBuilder.CreateBox("Icon", window, found ? rarityColor : notFoundColor, new Vector2(-250f, 340f), new Vector2(280f, 280f));
        UiBuilder.CreateText("Letters", icon.transform, found ? Initials(type.title) : "?", 110, Vector2.zero, new Vector2(280f, 280f));
        Text description = UiBuilder.CreateText("Description", window, type.description, 36, new Vector2(180f, 340f), new Vector2(500f, 280f));
        description.alignment = TextAnchor.UpperLeft;
        CreateStars(window, stars, new Vector2(-250f, 165f), 34f, 52f);

        // Which set it belongs to, under the description
        ArtifactSet set = ArtifactSets.SetOf(type);
        if (set != null)
        {
            Text setText = UiBuilder.CreateText("Set", window, "Set: " + set.title, 34, new Vector2(180f, 165f), new Vector2(500f, 60f));
            setText.alignment = TextAnchor.MiddleLeft;
            setText.color = new Color(0.8f, 0.85f, 1f);
        }

        // Base stats, with what the next star would make of them in green
        CreateSection(window, "BASE STATS", 80f);
        string baseText = StatLine(type, ArtifactStat.Attack, stars, maxed) + "\n" + StatLine(type, ArtifactStat.MaxHealth, stars, maxed);
        CreateLeftText(window, "BaseStats", baseText, new Vector2(0f, -15f), 100f);

        CreateSection(window, "SPECIAL STATS", -120f);
        CreateLeftText(window, "SpecialStats", StatLine(type, type.special, stars, maxed), new Vector2(0f, -190f), 60f);

        // What she has and what the next star costs, then the button
        if (!found)
        {
            UiBuilder.CreateText("NotFound", window, "Not found yet.\nMake a WISH to find it!", 44, new Vector2(0f, -340f), new Vector2(880f, 140f));
        }
        else if (maxed)
        {
            Text max = UiBuilder.CreateText("Max", window, "MAX STARS", 56, new Vector2(0f, -340f), new Vector2(880f, 100f));
            max.color = starColor;
        }
        else
        {
            int cost = ArtifactCatalog.StarCost(stars);
            int own = Artifacts.Shards(type);
            string ownText = own + " / " + cost;
            if (own + Artifacts.WildShards < cost) ownText = "<color=" + Red + ">" + own + "</color> / " + cost;

            UiBuilder.CreateText("Cost", window, "Shards " + ownText + "    +    Wild " + Artifacts.WildShards, 40, new Vector2(0f, -300f), new Vector2(880f, 70f));

            bool canStarUp = Artifacts.CanStarUp(type);
            Text starUp = UiBuilder.CreateButton("StarUp", window, "STAR UP", new Vector2(0f, -420f), new Vector2(460f, 140f), canStarUp ? goodButtonColor : unavailableColor, () => StarUp(type));
            starUp.fontSize = 60;
        }

        UiBuilder.CreateButton("Close", window, "CLOSE", new Vector2(0f, -590f), new Vector2(460f, 110f), unavailableColor, ClosePopup);
    }

    // "ATK +6" or, if a star can still be added, "ATK +6  (+9)" with the next value in green
    string StatLine(ArtifactType type, ArtifactStat stat, int stars, bool maxed)
    {
        string line = ArtifactCatalog.Format(stat, type.Value(stat, stars));
        if (!maxed) line += "   <color=" + Green + ">(" + ArtifactCatalog.FormatValue(stat, type.Value(stat, stars + 1)) + ")</color>";
        return line;
    }

    void StarUp(ArtifactType type)
    {
        if (!Artifacts.TryStarUp(type)) return;

        SoundManager.Play(Sound.LevelUp);
        ShowArtifact(type); // build the window again with the new numbers
    }

    // ---------- The DETAILS window ----------

    // Every stat that her artifacts give, added up (like the Details list in the reference)
    void ShowDetails()
    {
        Transform window = OpenPopup("DetailsPopup", new Vector2(920f, 1360f));

        Image header = UiBuilder.CreateBox("Header", window, wishColor, new Vector2(0f, 600f), new Vector2(920f, 160f));
        UiBuilder.CreateText("Title", header.transform, "DETAILS", 64, Vector2.zero, new Vector2(900f, 160f));

        ScrollRect list = UiBuilder.CreateScrollGrid("StatList", window, new Vector2(840f, 90f), 12f, 1);
        RectTransform listRect = list.GetComponent<RectTransform>();
        listRect.offsetMin = new Vector2(20f, 200f);  // leave room for the CLOSE button
        listRect.offsetMax = new Vector2(-20f, -200f); // and for the header

        int rows = 0;
        foreach (ArtifactStat stat in System.Enum.GetValues(typeof(ArtifactStat)))
        {
            float total = Artifacts.Bonus(stat);
            if (total <= 0f) continue;

            Image row = UiBuilder.CreateBox(stat + "Row", list.content, sectionColor, Vector2.zero, Vector2.zero);
            Text name = CreateLeftText(row.transform, "Name", ArtifactCatalog.StatName(stat), Vector2.zero, 90f);
            name.rectTransform.sizeDelta = new Vector2(780f, 90f);
            Text value = UiBuilder.CreateText("Value", row.transform, ArtifactCatalog.FormatValue(stat, total), 44, Vector2.zero, new Vector2(780f, 90f));
            value.alignment = TextAnchor.MiddleRight;
            value.color = new Color(0.5f, 1f, 0.5f);
            rows++;
        }

        if (rows == 0) UiBuilder.CreateText("Empty", window, "No artifacts yet.\nMake a WISH to find one!", 44, Vector2.zero, new Vector2(880f, 200f));

        UiBuilder.CreateButton("Close", window, "CLOSE", new Vector2(0f, -590f), new Vector2(460f, 110f), unavailableColor, ClosePopup);
    }

    // ---------- Wishing ----------

    void Wish()
    {
        if (!Artifacts.TryWish(out ArtifactType won, out int shards)) return;

        SoundManager.Play(Sound.WheelPrize);
        Color rarityColor = GearCatalog.RarityColor(won.rarity);

        Transform window = OpenPopup("WishPopup", new Vector2(860f, 760f));

        string headline = shards == 0 ? "NEW ARTIFACT!" : "+" + shards + " SHARDS";
        Text title = UiBuilder.CreateText("Headline", window, headline, 70, new Vector2(0f, 260f), new Vector2(840f, 100f));
        title.color = starColor;

        Image icon = UiBuilder.CreateBox("Icon", window, rarityColor, new Vector2(0f, 60f), new Vector2(220f, 220f));
        UiBuilder.CreateText("Letters", icon.transform, Initials(won.title), 90, Vector2.zero, new Vector2(220f, 220f));
        Text name = UiBuilder.CreateText("Name", window, won.title + "\n<size=34>" + GearCatalog.RarityName(won.rarity) + "</size>", 54, new Vector2(0f, -120f), new Vector2(840f, 130f));
        name.color = rarityColor;

        bool canWishAgain = Wallet.Gems >= ArtifactCatalog.WishPrice;
        UiBuilder.CreateButton("Again", window, "WISH AGAIN", new Vector2(-200f, -290f), new Vector2(370f, 120f), canWishAgain ? wishColor : unavailableColor, Wish).fontSize = 46;
        UiBuilder.CreateButton("Ok", window, "OK", new Vector2(200f, -290f), new Vector2(370f, 120f), goodButtonColor, ClosePopup);
    }

    // ---------- Popup helpers ----------

    // A dark screen with a window in the middle. Tapping the dark part (outside the window) closes it.
    // Returns the window, to put the contents in.
    Transform OpenPopup(string name, Vector2 size)
    {
        ClosePopup();

        popup = UiBuilder.CreateFullScreenPanel(name, overlayParent, new Color(0f, 0f, 0f, 0.75f));

        // An invisible button over the whole screen, BEHIND the window. A tap on the window itself
        // doesn't reach it, because the window is drawn on top and catches the tap first.
        GameObject closeArea = UiBuilder.CreateFullScreenPanel("CloseArea", popup.transform, Color.clear);
        closeArea.AddComponent<Button>().onClick.AddListener(ClosePopup);

        return UiBuilder.CreateBox("Window", popup.transform, windowColor, Vector2.zero, size).transform;
    }

    void ClosePopup()
    {
        if (popup != null) Destroy(popup);
    }

    // A darker bar with a title, across the window
    void CreateSection(Transform window, string title, float y)
    {
        Image bar = UiBuilder.CreateBox(title, window, sectionColor, new Vector2(0f, y), new Vector2(860f, 70f));
        CreateLeftText(bar.transform, "Title", title, Vector2.zero, 70f).color = new Color(0.8f, 0.85f, 1f);
    }

    // Text that starts at the left edge, with a small gap
    Text CreateLeftText(Transform parent, string name, string content, Vector2 position, float height)
    {
        Text text = UiBuilder.CreateText(name, parent, content, 44, position, new Vector2(800f, height));
        text.alignment = TextAnchor.MiddleLeft;
        return text;
    }
}
