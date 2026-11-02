using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// A black image over the whole screen that can fade in and out. ChapterManager uses it between rooms.
// It starts fully black, so the first room fades in nicely when the game starts.
// It builds its own UI when the game starts, so you only need to add this component to an empty GameObject.
public class ScreenFader : MonoBehaviour
{
    public float fadeTime = 0.35f;
    public Color color = Color.black;

    // So ChapterManager can find it without a drag-and-drop field
    public static ScreenFader Instance { get; private set; }

    private Image image;

    void Awake()
    {
        Instance = this;
        BuildUI();
        SetAlpha(1f);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // To black. Use with "yield return" inside a coroutine to wait until it's done.
    public IEnumerator FadeOut()
    {
        return FadeTo(1f);
    }

    // Back to the game
    public IEnumerator FadeIn()
    {
        return FadeTo(0f);
    }

    IEnumerator FadeTo(float targetAlpha)
    {
        image.raycastTarget = true; // block taps while fading

        while (!Mathf.Approximately(image.color.a, targetAlpha))
        {
            SetAlpha(Mathf.MoveTowards(image.color.a, targetAlpha, Time.deltaTime / fadeTime));
            yield return null; // wait one frame
        }

        image.raycastTarget = targetAlpha > 0f;
    }

    void SetAlpha(float alpha)
    {
        Color c = color;
        c.a = alpha;
        image.color = c;
    }

    void BuildUI()
    {
        GameObject canvasObject = new GameObject("FadeCanvas", typeof(Canvas));
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 150; // above the game and XP bar, below the Game Over screen (200)

        GameObject imageObject = new GameObject("Fade", typeof(Image));
        imageObject.transform.SetParent(canvasObject.transform, false);
        image = imageObject.GetComponent<Image>();

        // Stretch over the whole screen
        RectTransform rect = image.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
}
