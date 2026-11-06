using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// The first screen of the game: title, the player's coins and a PLAY button.
// It builds its own UI when the game starts, so you only need to add this component to an empty GameObject.
public class MainMenu : MonoBehaviour
{
    public string gameTitle = "ARROW RUN";
    public string gameSceneName = "Game"; // must be in the Build Profile's scene list
    public Color backgroundColor = new Color(0.13f, 0.17f, 0.25f);
    public Color titleColor = Color.white;
    public Color coinsColor = new Color(1f, 0.8f, 0.15f);
    public Color buttonColor = new Color(0.95f, 0.65f, 0.15f);

    private bool canPressPlay; // false while fading, so two fades can't run against each other

    void Awake()
    {
        Time.timeScale = 1f; // in case we came here from a paused Game Over
        BuildUI();
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

    void BuildUI()
    {
        Canvas canvas = UiBuilder.CreateCanvas("MenuCanvas", transform, 0);
        GameObject panel = UiBuilder.CreateFullScreenPanel("Background", canvas.transform, backgroundColor);

        Text title = UiBuilder.CreateText("Title", panel.transform, gameTitle, 130, new Vector2(0f, 400f), new Vector2(1000f, 220f));
        title.color = titleColor;

        Text coins = UiBuilder.CreateText("Coins", panel.transform, "Coins: " + Wallet.Coins, 60, new Vector2(0f, 180f), new Vector2(1000f, 90f));
        coins.color = coinsColor;

        UiBuilder.CreateButton("PlayButton", panel.transform, "PLAY", new Vector2(0f, -100f), new Vector2(560f, 180f), buttonColor, Play);
    }
}
