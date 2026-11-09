using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// The Collectible page of the main menu, with two small tabs of its own: Talent Cards and Artifact.
// The DRAW button upgrades a random talent, see Upgrades.TryDraw. The Artifact tab is built by ArtifactScreen.
// MainMenu adds this component by code and calls Build(), so there is nothing to set up in the scene.
public class CollectibleScreen : MonoBehaviour
{
    public float rollStepTime = 0.08f; // how long each card lights up during a draw
    public int rollSteps = 12;
    public Color tabColor = new Color(0.17f, 0.21f, 0.32f);
    public Color selectedTabColor = new Color(0.4f, 0.45f, 0.9f);
    public Color cardColor = new Color(0.45f, 0.3f, 0.7f);
    public Color highlightColor = new Color(0.95f, 0.65f, 0.15f);
    public Color starColor = new Color(1f, 0.8f, 0.15f);
    public Color emptyStarColor = new Color(0.2f, 0.15f, 0.3f);
    public Color drawColor = new Color(0.3f, 0.7f, 0.3f);
    public Color unavailableColor = new Color(0.4f, 0.4f, 0.45f);

    private const int CardsPerRow = 3;

    // The parts of one card that change after a draw
    private class Card
    {
        public UpgradeType type;
        public Image background;
        public Text bonus;
        public Image[] stars;
    }

    private Card[] cards;
    private GameObject talentPanel;
    private GameObject artifactPanel;
    private Image talentTab;
    private Image artifactTab;
    private Text resultText;
    private Text statusText;
    private Text drawLabel;
    private Image drawButton;
    private bool drawing; // true during the short animation, so a draw can't start twice

    public void Build(Transform page)
    {
        talentPanel = UiBuilder.CreateFullScreenPanel("TalentCards", page, Color.clear);
        artifactPanel = UiBuilder.CreateFullScreenPanel("Artifact", page, Color.clear);

        Vector2 tabSize = new Vector2(480f, 110f);
        Text talentLabel = UiBuilder.CreateButton("TalentTab", page, "TALENT CARDS", new Vector2(-250f, 680f), tabSize, tabColor, () => ShowTab(true));
        Text artifactLabel = UiBuilder.CreateButton("ArtifactTab", page, "ARTIFACT", new Vector2(250f, 680f), tabSize, tabColor, () => ShowTab(false));
        talentLabel.fontSize = artifactLabel.fontSize = 46;
        talentTab = talentLabel.transform.parent.GetComponent<Image>();
        artifactTab = artifactLabel.transform.parent.GetComponent<Image>();

        BuildTalentPanel(talentPanel.transform);
        gameObject.AddComponent<ArtifactScreen>().Build(artifactPanel.transform);

        Refresh();
        ShowTab(true);
    }

    void ShowTab(bool showTalents)
    {
        talentPanel.SetActive(showTalents);
        artifactPanel.SetActive(!showTalents);
        talentTab.color = showTalents ? selectedTabColor : tabColor;
        artifactTab.color = showTalents ? tabColor : selectedTabColor;
    }

    void Draw()
    {
        if (drawing) return;

        // The result is decided and saved right away. The animation only shows it.
        if (Upgrades.TryDraw(out UpgradeType won)) StartCoroutine(PlayDraw(won));
    }

    IEnumerator PlayDraw(UpgradeType won)
    {
        drawing = true;
        resultText.text = "";

        // Light up the cards one after another
        for (int step = 0; step < rollSteps; step++)
        {
            Highlight(Upgrades.All[step % cards.Length]); // % = what is left after dividing, so it goes 0 1 2 0 1 2...
            yield return new WaitForSeconds(rollStepTime);
        }

        Refresh();
        Highlight(won);
        resultText.text = Upgrades.GetTitle(won) + " +1 star!";
        drawing = false;
    }

    void Highlight(UpgradeType type)
    {
        foreach (Card card in cards)
        {
            card.background.color = card.type == type ? highlightColor : cardColor;
        }
    }

    // Writes the current numbers on the cards and under them
    void Refresh()
    {
        foreach (Card card in cards)
        {
            card.background.color = cardColor;
            card.bonus.text = Upgrades.GetBonusText(card.type);

            for (int i = 0; i < card.stars.Length; i++)
            {
                card.stars[i].color = i < Upgrades.GetLevel(card.type) ? starColor : emptyStarColor;
            }
        }

        int price = Upgrades.Price;
        bool canDraw = !Upgrades.AllMaxed && Upgrades.DrawsLeft > 0;

        if (Upgrades.AllMaxed) statusText.text = "All talents are maxed";
        else if (canDraw) statusText.text = "Draws left: " + Upgrades.DrawsLeft;
        else statusText.text = "Next " + Upgrades.DrawsPerBatch + " draws at level " + Upgrades.NextUnlockLevel;

        drawLabel.text = canDraw ? "DRAW\n" + price + " coins" : "DRAW"; // \n = new line
        drawButton.color = canDraw && Wallet.Coins >= price ? drawColor : unavailableColor;
    }

    void BuildTalentPanel(Transform panel)
    {
        cards = new Card[Upgrades.All.Length];
        for (int i = 0; i < cards.Length; i++)
        {
            // Three cards per row. % gives the column (0 1 2 0 1 2...), dividing two ints gives the row (0 0 0 1 1 1...).
            int column = i % CardsPerRow;
            int row = i / CardsPerRow;
            float x = (column - 1) * 340f; // -340, 0, +340
            float y = 450f - row * 320f;
            cards[i] = CreateCard(panel, Upgrades.All[i], new Vector2(x, y));
        }

        resultText = UiBuilder.CreateText("Result", panel, "", 60, new Vector2(0f, -90f), new Vector2(1000f, 90f));
        resultText.color = highlightColor;

        statusText = UiBuilder.CreateText("Status", panel, "", 50, new Vector2(0f, -180f), new Vector2(1000f, 80f));

        drawLabel = UiBuilder.CreateButton("DrawButton", panel, "", new Vector2(0f, -350f), new Vector2(560f, 200f), drawColor, Draw);
        drawLabel.fontSize = 56;
        drawButton = drawLabel.transform.parent.GetComponent<Image>();
    }

    Card CreateCard(Transform parent, UpgradeType type, Vector2 position)
    {
        Card card = new Card();
        card.type = type;
        card.background = UiBuilder.CreateBox(type + "Card", parent, cardColor, position, new Vector2(310f, 300f));
        Transform cardTransform = card.background.transform;

        UiBuilder.CreateText("Title", cardTransform, Upgrades.GetTitle(type), 38, new Vector2(0f, 20f), new Vector2(300f, 90f));
        card.bonus = UiBuilder.CreateText("Bonus", cardTransform, "", 32, new Vector2(0f, -85f), new Vector2(300f, 80f));

        // The stars along the top of the card. Each one is a small square, turned 45 degrees.
        card.stars = new Image[Upgrades.MaxLevel];
        for (int i = 0; i < card.stars.Length; i++)
        {
            float x = (i - (card.stars.Length - 1) / 2f) * 52f;
            card.stars[i] = UiBuilder.CreateBox("Star" + i, cardTransform, emptyStarColor, new Vector2(x, 110f), new Vector2(30f, 30f));
            card.stars[i].rectTransform.localEulerAngles = new Vector3(0f, 0f, 45f);
        }

        return card;
    }
}
