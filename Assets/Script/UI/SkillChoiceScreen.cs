using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// When the player levels up: pause the game and show 3 random skill cards. Tapping a card applies
// the skill and the game goes on. The skills to pick from are SkillData assets:
// drag them from the Project window into the 'skills' list in the Inspector.
// It builds its own UI when the game starts, so you only need to add this component to an empty GameObject.
public class SkillChoiceScreen : MonoBehaviour
{
    public int cardCount = 3;
    public Color overlayColor = new Color(0f, 0f, 0f, 0.75f);

    // Every skill that can show up on a card (Assets/Skills)
    public List<SkillData> skills = new List<SkillData>();

    private GameObject panel;
    private Button[] cardButtons;
    private Text[] cardTitles;
    private Text[] cardDescriptions;

    private List<SkillData> shownSkills = new List<SkillData>(); // the skills on the cards right now
    private int choicesWaiting; // level-ups that still need a choice (two can happen at once)

    void Awake()
    {
        BuildUI();
        panel.SetActive(false);
    }

    void OnEnable()
    {
        PlayerXp.LeveledUp += OnLeveledUp;
    }

    void OnDisable()
    {
        PlayerXp.LeveledUp -= OnLeveledUp;
    }

    void OnLeveledUp(int newLevel)
    {
        choicesWaiting++;
        if (!panel.activeSelf) ShowCards();
    }

    void ShowCards()
    {
        PickRandomSkills();

        if (shownSkills.Count == 0)
        {
            // Don't pause the game forever with an empty screen
            Debug.LogWarning("SkillChoiceScreen has no skills: drag SkillData assets into its Skills list.");
            choicesWaiting = 0;
            return;
        }

        for (int i = 0; i < cardButtons.Length; i++)
        {
            // Fewer skills than cards? Hide the extra cards.
            bool hasSkill = i < shownSkills.Count;
            cardButtons[i].gameObject.SetActive(hasSkill);
            if (!hasSkill) continue;

            cardTitles[i].text = shownSkills[i].title;
            cardDescriptions[i].text = shownSkills[i].description;
            cardButtons[i].image.color = shownSkills[i].cardColor;
        }

        panel.SetActive(true);
        Time.timeScale = 0f; // pause: enemies, bullets and orbs all stop
    }

    // Fills shownSkills with different random skills (never the same one twice)
    void PickRandomSkills()
    {
        shownSkills.Clear();
        List<SkillData> pool = new List<SkillData>(skills); // a copy, so we can remove from it
        pool.RemoveAll(skill => skill == null);              // skip empty slots in the Inspector list

        while (shownSkills.Count < cardCount && pool.Count > 0)
        {
            int index = Random.Range(0, pool.Count); // max is exclusive: 0 .. Count-1
            shownSkills.Add(pool[index]);
            pool.RemoveAt(index);
        }
    }

    void OnCardChosen(int cardIndex)
    {
        PlayerHealth player = PlayerHealth.Instance;
        if (player != null)
        {
            PlayerSkills playerSkills = player.GetComponent<PlayerSkills>();
            if (playerSkills != null) playerSkills.Apply(shownSkills[cardIndex]);
            else Debug.LogWarning("Add the PlayerSkills component to the Player, or skills do nothing.");
        }

        choicesWaiting--;
        if (choicesWaiting > 0)
        {
            ShowCards(); // another level-up is waiting: new cards right away
        }
        else
        {
            panel.SetActive(false);
            Time.timeScale = 1f;
        }
    }

    void BuildUI()
    {
        // Buttons need an EventSystem to receive clicks/taps
        if (FindAnyObjectByType<EventSystem>() == null)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        GameObject canvasObject = new GameObject("SkillChoiceCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 150; // above the XP bar (100), below the Game Over screen (200)

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;

        // Dark overlay over the whole screen
        panel = new GameObject("Panel", typeof(Image));
        panel.transform.SetParent(canvasObject.transform, false);
        panel.GetComponent<Image>().color = overlayColor;
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = panelRect.offsetMax = Vector2.zero;

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        Text header = CreateText("Header", panel.transform, font, 80, new Vector2(0f, 600f), new Vector2(1000f, 120f));
        header.text = "CHOOSE A SKILL";
        header.color = new Color(1f, 0.85f, 0.3f);

        // The cards, one under the other (the screen is tall, not wide)
        cardButtons = new Button[cardCount];
        cardTitles = new Text[cardCount];
        cardDescriptions = new Text[cardCount];
        Vector2 cardSize = new Vector2(800f, 260f);
        float spacing = 300f;

        for (int i = 0; i < cardCount; i++)
        {
            GameObject cardObject = new GameObject("Card" + i, typeof(Image), typeof(Button));
            cardObject.transform.SetParent(panel.transform, false);
            RectTransform cardRect = cardObject.GetComponent<RectTransform>();
            cardRect.sizeDelta = cardSize;
            cardRect.anchoredPosition = new Vector2(0f, 250f - i * spacing);

            cardTitles[i] = CreateText("Title", cardObject.transform, font, 64, new Vector2(0f, 45f), new Vector2(cardSize.x, 90f));
            cardDescriptions[i] = CreateText("Description", cardObject.transform, font, 42, new Vector2(0f, -55f), new Vector2(cardSize.x, 70f));

            // Copy i into a new variable: the button remembers 'index' for later,
            // and every card needs its own number (i itself keeps changing in the loop)
            int index = i;
            cardButtons[i] = cardObject.GetComponent<Button>();
            cardButtons[i].onClick.AddListener(() => OnCardChosen(index));
        }
    }

    static Text CreateText(string name, Transform parent, Font font, int fontSize, Vector2 position, Vector2 size)
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
        text.raycastTarget = false; // let taps go through to the card button

        RectTransform rect = text.rectTransform;
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        return text;
    }
}
