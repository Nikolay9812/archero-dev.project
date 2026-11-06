using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// A pause button in the corner of the screen. Tapping it pauses the game and shows
// the skills taken this run, with buttons to carry on or quit to the main menu.
// It builds its own UI when the game starts, so you only need to add this component to an empty GameObject.
public class PauseMenu : MonoBehaviour
{
    public string menuSceneName = "MainMenu"; // must be in the Build Profile's scene list
    public Color overlayColor = new Color(0f, 0f, 0f, 0.8f);
    public Color pauseButtonColor = new Color(0f, 0f, 0f, 0.5f);
    public Color resumeButtonColor = new Color(0.95f, 0.65f, 0.15f);
    public Color quitButtonColor = new Color(0.35f, 0.4f, 0.5f);

    // Static so FloatingJoystick can check it without needing a reference to this object
    private static RectTransform pauseButtonRect;

    private GameObject panel;
    private Transform skillList;      // the skill cards are children of this
    private Text noSkillsText;

    // True if this screen position is on the pause button, so tapping it doesn't also start the joystick
    public static bool IsOverPauseButton(Vector2 screenPosition)
    {
        // A null camera is correct here: this canvas is a Screen Space Overlay
        return pauseButtonRect != null && RectTransformUtility.RectangleContainsScreenPoint(pauseButtonRect, screenPosition, null);
    }

    void Awake()
    {
        BuildUI();
        panel.SetActive(false);
    }

    public void Pause()
    {
        // timeScale 0 = something else already paused the game (skill cards, the wheel, Game Over).
        // If we paused on top of it, our Resume button would unpause the game too early.
        if (Time.timeScale == 0f) return;

        // Dead: the Game Over screen is on its way
        PlayerHealth player = PlayerHealth.Instance;
        if (player == null || player.IsDead) return;

        ShowSkills(player.GetComponent<PlayerSkills>());
        panel.SetActive(true);
        Time.timeScale = 0f;
    }

    public void Resume()
    {
        panel.SetActive(false);
        Time.timeScale = 1f;
    }

    public void QuitToMenu()
    {
        Time.timeScale = 1f; // the new scene must not start frozen
        SceneManager.LoadScene(menuSceneName);
    }

    // Rebuilds the list of cards every time, because new skills may have been taken since the last pause
    void ShowSkills(PlayerSkills playerSkills)
    {
        foreach (Transform oldCard in skillList)
        {
            Destroy(oldCard.gameObject);
        }

        if (playerSkills == null) return;

        // Taking the same skill twice gives one card with "x2", so first collect each skill once
        List<SkillData> taken = playerSkills.Taken;
        List<SkillData> differentSkills = new List<SkillData>();
        foreach (SkillData skill in taken)
        {
            if (!differentSkills.Contains(skill)) differentSkills.Add(skill);
        }

        noSkillsText.gameObject.SetActive(differentSkills.Count == 0);

        for (int i = 0; i < differentSkills.Count; i++)
        {
            SkillData skill = differentSkills[i];
            int count = taken.FindAll(other => other == skill).Count;
            AddSkillCard(skill, count, i);
        }
    }

    // Three cards per row. index % 3 = the column (0, 1, 2), index / 3 = the row (whole numbers only).
    void AddSkillCard(SkillData skill, int count, int index)
    {
        Vector2 cardSize = new Vector2(300f, 210f);
        float x = (index % 3 - 1) * 330f;
        float y = -(index / 3) * 230f;

        GameObject card = new GameObject(skill.title, typeof(Image));
        card.transform.SetParent(skillList, false);
        card.GetComponent<Image>().color = skill.cardColor;
        RectTransform cardRect = card.GetComponent<RectTransform>();
        cardRect.sizeDelta = cardSize;
        cardRect.anchoredPosition = new Vector2(x, y);

        // No picture set on the skill? Then there's simply no icon.
        if (skill.icon != null)
        {
            GameObject icon = new GameObject("Icon", typeof(Image));
            icon.transform.SetParent(card.transform, false);
            icon.GetComponent<Image>().sprite = skill.icon;
            icon.GetComponent<Image>().raycastTarget = false;
            RectTransform iconRect = icon.GetComponent<RectTransform>();
            iconRect.sizeDelta = new Vector2(100f, 100f);
            iconRect.anchoredPosition = new Vector2(0f, 40f);
        }

        string title = skill.title;
        if (count > 1) title += " x" + count;
        UiBuilder.CreateText("Title", card.transform, title, 34, new Vector2(0f, -60f), new Vector2(280f, 80f));
    }

    void BuildUI()
    {
        // The button sits on its own canvas under the other screens (100),
        // so the skill cards and Game Over cover it. The pause screen goes above them (180).
        Canvas buttonCanvas = UiBuilder.CreateCanvas("PauseButtonCanvas", transform, 100);
        Text buttonLabel = UiBuilder.CreateButton("PauseButton", buttonCanvas.transform, "II", Vector2.zero, new Vector2(100f, 100f), pauseButtonColor, Pause);

        // Top-left of the screen, just under the XP bar
        pauseButtonRect = buttonLabel.transform.parent.GetComponent<RectTransform>();
        pauseButtonRect.anchorMin = pauseButtonRect.anchorMax = new Vector2(0f, 1f);
        pauseButtonRect.pivot = new Vector2(0f, 1f);
        pauseButtonRect.anchoredPosition = new Vector2(40f, -165f);

        Canvas canvas = UiBuilder.CreateCanvas("PauseCanvas", transform, 180);
        panel = UiBuilder.CreateFullScreenPanel("Panel", canvas.transform, overlayColor);

        UiBuilder.CreateText("Title", panel.transform, "PAUSED", 110, new Vector2(0f, 680f), new Vector2(1000f, 150f));
        UiBuilder.CreateText("SkillsHeader", panel.transform, "YOUR SKILLS", 56, new Vector2(0f, 550f), new Vector2(1000f, 80f));

        noSkillsText = UiBuilder.CreateText("NoSkills", panel.transform, "No skills yet", 48, new Vector2(0f, 300f), new Vector2(1000f, 80f));

        // An empty object: the first row of cards is at its position
        GameObject listObject = new GameObject("SkillList", typeof(RectTransform));
        listObject.transform.SetParent(panel.transform, false);
        listObject.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 390f);
        skillList = listObject.transform;

        Vector2 buttonSize = new Vector2(480f, 150f);
        UiBuilder.CreateButton("ResumeButton", panel.transform, "RESUME", new Vector2(0f, -420f), buttonSize, resumeButtonColor, Resume);
        UiBuilder.CreateButton("QuitButton", panel.transform, "QUIT", new Vector2(0f, -600f), buttonSize, quitButtonColor, QuitToMenu);
    }
}
