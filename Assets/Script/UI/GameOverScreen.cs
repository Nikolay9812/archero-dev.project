using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Shows "GAME OVER" with a Retry button when the player dies, and pauses the game.
// Shows "STAGE CLEAR" the same way when ChapterManager says the last room is done.
// It builds its own UI when the game starts, so you only need to add this component to an empty GameObject.
public class GameOverScreen : MonoBehaviour
{
    public float showDelay = 1f; // short pause after death so the player sees what happened
    public Color overlayColor = new Color(0f, 0f, 0f, 0.7f);
    public Color buttonColor = new Color(0.95f, 0.65f, 0.15f);

    private GameObject panel;
    private Text titleText;
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

    void BuildUI()
    {
        // Buttons need an EventSystem to receive clicks/taps
        if (FindAnyObjectByType<EventSystem>() == null)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        GameObject canvasObject = new GameObject("GameOverCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200; // above the joystick

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;

        // Dark overlay over the whole screen (also blocks clicks on anything behind it)
        panel = new GameObject("Panel", typeof(Image));
        panel.transform.SetParent(canvasObject.transform, false);
        panel.GetComponent<Image>().color = overlayColor;
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = panelRect.offsetMax = Vector2.zero;

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        titleText = CreateText("Title", panel.transform, font, "GAME OVER", 120, new Vector2(0f, 200f), new Vector2(1000f, 160f));

        // Retry button
        GameObject buttonObject = new GameObject("RetryButton", typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(panel.transform, false);
        buttonObject.GetComponent<Image>().color = buttonColor;
        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.sizeDelta = new Vector2(480f, 150f);
        buttonRect.anchoredPosition = new Vector2(0f, -100f);
        buttonObject.GetComponent<Button>().onClick.AddListener(Retry);

        buttonText = CreateText("Label", buttonObject.transform, font, "RETRY", 72, Vector2.zero, buttonRect.sizeDelta);
    }

    static Text CreateText(string name, Transform parent, Font font, string content, int fontSize, Vector2 position, Vector2 size)
    {
        GameObject textObject = new GameObject(name, typeof(Text));
        textObject.transform.SetParent(parent, false);

        Text text = textObject.GetComponent<Text>();
        text.font = font;
        text.text = content;
        text.fontSize = fontSize;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.raycastTarget = false;

        RectTransform rect = text.rectTransform;
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        return text;
    }
}
