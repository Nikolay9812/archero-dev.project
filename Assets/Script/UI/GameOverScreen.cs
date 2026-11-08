using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Shows "GAME OVER" with Retry and Menu buttons when the player dies, and pauses the game.
// Shows "STAGE CLEAR" the same way when ChapterManager says the last room is done.
// It builds its own UI when the game starts, so you only need to add this component to an empty GameObject.
public class GameOverScreen : MonoBehaviour
{
    public float showDelay = 1f; // short pause after death so the player sees what happened
    public Color overlayColor = new Color(0f, 0f, 0f, 0.7f);
    public Color buttonColor = new Color(0.95f, 0.65f, 0.15f);
    public Color menuButtonColor = new Color(0.35f, 0.4f, 0.5f);
    public string menuSceneName = "MainMenu"; // must be in the Build Profile's scene list

    private GameObject panel;
    private Text titleText;
    private Text coinsText;
    private Text buttonText;

    void Awake()
    {
        Time.timeScale = 1f; // in case we came here from a paused Game Over
        BuildUI();
        panel.SetActive(false);
    }

    void OnEnable()
    {
        PlayerHealth.Died += OnPlayerDied;
        ChapterManager.ChapterCleared += OnStageCleared;
    }

    void OnDisable()
    {
        PlayerHealth.Died -= OnPlayerDied;
        ChapterManager.ChapterCleared -= OnStageCleared;
    }

    void OnPlayerDied()
    {
        Invoke(nameof(ShowGameOver), showDelay);
    }

    void OnStageCleared()
    {
        Show("STAGE CLEAR", "PLAY AGAIN");
    }

    void ShowGameOver()
    {
        Show("GAME OVER", "RETRY");
    }

    void Show(string title, string buttonLabel)
    {
        titleText.text = title;
        coinsText.text = "+" + Wallet.EarnedThisRun + " coins";
        if (Wallet.GemsEarnedThisRun > 0) coinsText.text += "   +" + Wallet.GemsEarnedThisRun + " gems";
        if (PlayerProfile.XpEarnedThisRun > 0) coinsText.text += "\n+" + PlayerProfile.XpEarnedThisRun + " profile XP"; // \n = new line

        // Gear and scrolls found on the way (only shown if there were any)
        string loot = "";
        if (Inventory.GearFoundThisRun > 0) loot += "+" + Inventory.GearFoundThisRun + " gear   ";
        if (Inventory.ScrollsFoundThisRun > 0) loot += "+" + Inventory.ScrollsFoundThisRun + " scrolls";
        if (loot != "") coinsText.text += "\n" + loot;
        buttonText.text = buttonLabel;
        panel.SetActive(true);
        Time.timeScale = 0f; // freeze enemies and bullets
    }

    public void Retry()
    {
        Time.timeScale = 1f;
        Scene scene = SceneManager.GetActiveScene();

        if (scene.buildIndex >= 0)
        {
            SceneManager.LoadScene(scene.buildIndex);
        }
        else
        {
#if UNITY_EDITOR
            // The scene isn't in the Build Profile's scene list yet: this still works in the editor
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(scene.path, new LoadSceneParameters(LoadSceneMode.Single));
#else
            Debug.LogError("Add this scene to the Build Profile's scene list so Retry can reload it.");
#endif
        }
    }

    public void GoToMenu()
    {
        Time.timeScale = 1f; // the new scene must not start frozen
        SceneManager.LoadScene(menuSceneName);
    }

    void BuildUI()
    {
        Canvas canvas = UiBuilder.CreateCanvas("GameOverCanvas", transform, 200); // 200 = above the joystick

        // Dark overlay over the whole screen
        panel = UiBuilder.CreateFullScreenPanel("Panel", canvas.transform, overlayColor);

        titleText = UiBuilder.CreateText("Title", panel.transform, "GAME OVER", 120, new Vector2(0f, 330f), new Vector2(1000f, 160f));

        // Up to 4 lines: coins + gems, profile XP, gear + scrolls
        coinsText = UiBuilder.CreateText("Coins", panel.transform, "", 48, new Vector2(0f, 130f), new Vector2(1000f, 240f));
        coinsText.color = new Color(1f, 0.8f, 0.15f);

        Vector2 buttonSize = new Vector2(480f, 150f);
        buttonText = UiBuilder.CreateButton("RetryButton", panel.transform, "RETRY", new Vector2(0f, -100f), buttonSize, buttonColor, Retry);
        UiBuilder.CreateButton("MenuButton", panel.transform, "MENU", new Vector2(0f, -280f), buttonSize, menuButtonColor, GoToMenu);
    }
}
