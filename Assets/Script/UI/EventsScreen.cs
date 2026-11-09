using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// The EVENTS page of the main menu: a list of event cards, with three small tabs at the bottom
// (ARENA, CHALLENGE, DUNGEON). The events aren't playable yet: tapping one says "Coming soon".
// To make one real later, change what OpenEvent does for it.
// MainMenu adds this component by code and calls Build(), so there is nothing to set up in the scene.
public class EventsScreen : MonoBehaviour
{
    public Color tabColor = new Color(0.17f, 0.21f, 0.32f);
    public Color selectedTabColor = new Color(0.95f, 0.65f, 0.15f);
    public Color windowColor = new Color(0.13f, 0.16f, 0.24f);
    public Color okColor = new Color(0.3f, 0.7f, 0.3f);

    // One event card. "struct" is like a small class for a few values that belong together.
    private struct EventInfo
    {
        public string title;
        public string subtitle;
        public Color color;

        public EventInfo(string title, string subtitle, Color color)
        {
            this.title = title;
            this.subtitle = subtitle;
            this.color = color;
        }
    }

    private static readonly string[] TabNames = { "ARENA", "CHALLENGE", "DUNGEON" };
    private const int StartTab = 1; // CHALLENGE, like in the reference

    // The events of each tab. Index = tab number. To add an event: add one line here.
    private static readonly EventInfo[][] Events =
    {
        new[] // ARENA
        {
            new EventInfo("Duel Arena", "Fight other archers", new Color(0.55f, 0.25f, 0.3f)),
            new EventInfo("Ranked Season", "Climb the season ladder", new Color(0.3f, 0.3f, 0.6f))
        },
        new[] // CHALLENGE
        {
            new EventInfo("Endless Tower", "How high can you climb?", new Color(0.3f, 0.35f, 0.65f)),
            new EventInfo("Boss Rush", "Every boss, one after another", new Color(0.7f, 0.3f, 0.2f)),
            new EventInfo("Monster Wave", "Survive wave after wave", new Color(0.45f, 0.2f, 0.4f)),
            new EventInfo("Seed Guard", "Protect the magic seed", new Color(0.15f, 0.5f, 0.45f))
        },
        new[] // DUNGEON
        {
            new EventInfo("Gold Cave", "Lots of coins, little time", new Color(0.65f, 0.5f, 0.15f)),
            new EventInfo("Scroll Vault", "Find scrolls for your gear", new Color(0.5f, 0.4f, 0.25f)),
            new EventInfo("Gem Mine", "Dig deep for gems", new Color(0.2f, 0.5f, 0.65f))
        }
    };

    private ScrollRect list;
    private Image[] tabs = new Image[TabNames.Length];
    private Transform overlayParent; // the whole canvas, so the popup covers the header and the tabs too
    private GameObject popup;

    public void Build(Transform page)
    {
        overlayParent = page.GetComponentInParent<Canvas>().transform;

        UiBuilder.CreateText("Title", page, "EVENTS", 100, new Vector2(0f, 680f), new Vector2(1000f, 150f));

        // The cards: a fixed box in the middle that scrolls if a tab has more events than fit
        list = UiBuilder.CreateScrollGrid("EventList", page, new Vector2(1020f, 280f), 25f, 1);
        RectTransform listRect = list.GetComponent<RectTransform>();
        listRect.anchorMin = listRect.anchorMax = new Vector2(0.5f, 0.5f);
        listRect.sizeDelta = new Vector2(1040f, 1150f);
        listRect.anchoredPosition = new Vector2(0f, 30f);

        // The three tabs along the bottom
        for (int i = 0; i < TabNames.Length; i++)
        {
            int tab = i; // a copy for the lambda (same reason as in BottomTabBar)
            Text label = UiBuilder.CreateButton(TabNames[i] + "Tab", page, TabNames[i], new Vector2((i - 1) * 345f, -660f), new Vector2(330f, 110f), tabColor, () => ShowTab(tab));
            label.fontSize = 44;
            tabs[i] = label.transform.parent.GetComponent<Image>();
        }

        ShowTab(StartTab);
    }

    void ShowTab(int tab)
    {
        for (int i = 0; i < tabs.Length; i++)
        {
            tabs[i].color = i == tab ? selectedTabColor : tabColor;
        }

        UiBuilder.ClearChildren(list.content);
        foreach (EventInfo info in Events[tab])
        {
            EventInfo tapped = info;
            CreateCard(list.content, info, () => OpenEvent(tapped));
        }
        list.verticalNormalizedPosition = 1f; // scroll back to the top
    }

    // One card: a "picture" on the left (placeholder: the first letters), title and subtitle on the right
    void CreateCard(Transform parent, EventInfo info, UnityAction onTap)
    {
        Text label = UiBuilder.CreateButton(info.title, parent, "", Vector2.zero, Vector2.zero, info.color, onTap);
        Transform card = label.transform.parent;

        Image picture = UiBuilder.CreateBox("Picture", card, Color.Lerp(info.color, Color.black, 0.35f), new Vector2(-340f, 0f), new Vector2(300f, 240f));
        picture.raycastTarget = false; // taps go through to the card
        UiBuilder.CreateText("Letters", picture.transform, Initials(info.title), 100, Vector2.zero, new Vector2(300f, 240f));

        Text title = UiBuilder.CreateText("Title", card, info.title, 64, new Vector2(150f, 60f), new Vector2(640f, 90f));
        title.alignment = TextAnchor.MiddleRight;
        title.gameObject.AddComponent<Outline>().effectColor = Color.black;

        Text subtitle = UiBuilder.CreateText("Subtitle", card, info.subtitle, 36, new Vector2(150f, -10f), new Vector2(640f, 60f));
        subtitle.alignment = TextAnchor.MiddleRight;

        // A small dark tag in the corner, so it is clear before tapping that it isn't ready
        Image soon = UiBuilder.CreateBox("SoonTag", card, new Color(0f, 0f, 0f, 0.5f), new Vector2(320f, -90f), new Vector2(300f, 60f));
        soon.raycastTarget = false;
        UiBuilder.CreateText("Soon", soon.transform, "COMING SOON", 30, Vector2.zero, new Vector2(300f, 60f));

    }

    // For now every event only shows a message. Later: start the event here instead.
    void OpenEvent(EventInfo info)
    {
        ClosePopup();

        popup = UiBuilder.CreateFullScreenPanel("EventPopup", overlayParent, new Color(0f, 0f, 0f, 0.75f));

        // Tapping outside the window closes it (the same trick as in ArtifactScreen)
        GameObject closeArea = UiBuilder.CreateFullScreenPanel("CloseArea", popup.transform, Color.clear);
        closeArea.AddComponent<Button>().onClick.AddListener(ClosePopup);

        Transform window = UiBuilder.CreateBox("Window", popup.transform, windowColor, Vector2.zero, new Vector2(860f, 620f)).transform;

        Image header = UiBuilder.CreateBox("Header", window, info.color, new Vector2(0f, 230f), new Vector2(860f, 160f));
        UiBuilder.CreateText("Title", header.transform, info.title, 64, Vector2.zero, new Vector2(840f, 160f));

        UiBuilder.CreateText("Message", window, "Coming soon!\n<size=40>This event arrives in a later update.</size>", 64, new Vector2(0f, 20f), new Vector2(820f, 220f));

        UiBuilder.CreateButton("Ok", window, "OK", new Vector2(0f, -210f), new Vector2(400f, 120f), okColor, ClosePopup);
    }

    void ClosePopup()
    {
        if (popup != null) Destroy(popup);
    }

    // "Endless Tower" -> "ET"
    static string Initials(string title)
    {
        string result = "";
        foreach (string word in title.Split(' '))
        {
            if (word.Length > 0) result += word[0];
        }
        return result.ToUpper();
    }
}
