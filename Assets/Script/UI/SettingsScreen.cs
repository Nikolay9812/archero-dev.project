using UnityEngine;
using UnityEngine.UI;

// A window with the player's settings: music volume, sound volume and screen shake on/off.
// Opened from the main menu (button in the corner of the PLAY page) and from the pause menu.
// Call SettingsScreen.Open(canvas): it builds itself on top of that canvas and removes itself on CLOSE.
public class SettingsScreen : MonoBehaviour
{
    private static readonly Color WindowColor = new Color(0.13f, 0.16f, 0.24f);
    private static readonly Color MusicColor = new Color(0.3f, 0.75f, 1f);
    private static readonly Color SoundColor = new Color(0.95f, 0.65f, 0.15f);
    private static readonly Color OnColor = new Color(0.3f, 0.7f, 0.3f);
    private static readonly Color OffColor = new Color(0.4f, 0.4f, 0.45f);

    private Text musicLabel;
    private Text soundLabel;
    private Text shakeLabel;
    private Image shakeButton;

    // "static" so other screens don't need a reference: SettingsScreen.Open(myCanvas.transform)
    public static void Open(Transform canvas)
    {
        // The dark panel covers everything behind it, and this component lives on it
        GameObject panel = UiBuilder.CreateFullScreenPanel("Settings", canvas, new Color(0f, 0f, 0f, 0.75f));
        panel.AddComponent<SettingsScreen>().Build();
    }

    void Build()
    {
        Transform window = UiBuilder.CreateBox("Window", transform, WindowColor, Vector2.zero, new Vector2(900f, 900f)).transform;

        UiBuilder.CreateText("Title", window, "SETTINGS", 90, new Vector2(0f, 350f), new Vector2(880f, 130f));

        musicLabel = UiBuilder.CreateText("MusicLabel", window, "", 48, new Vector2(0f, 220f), new Vector2(880f, 70f));
        UiBuilder.CreateSlider("MusicSlider", window, new Vector2(0f, 140f), new Vector2(700f, 50f), MusicColor, Settings.MusicVolume, OnMusicChanged);

        soundLabel = UiBuilder.CreateText("SoundLabel", window, "", 48, new Vector2(0f, 40f), new Vector2(880f, 70f));
        UiBuilder.CreateSlider("SoundSlider", window, new Vector2(0f, -40f), new Vector2(700f, 50f), SoundColor, Settings.SoundVolume, OnSoundChanged);

        shakeLabel = UiBuilder.CreateButton("ShakeButton", window, "", new Vector2(0f, -180f), new Vector2(640f, 120f), OnColor, ToggleShake);
        shakeLabel.fontSize = 46;
        shakeButton = shakeLabel.transform.parent.GetComponent<Image>();

        UiBuilder.CreateButton("CloseButton", window, "CLOSE", new Vector2(0f, -340f), new Vector2(480f, 120f), OffColor, Close);

        RefreshTexts();
    }

    void OnMusicChanged(float value)
    {
        Settings.MusicVolume = value;
        RefreshTexts();
    }

    void OnSoundChanged(float value)
    {
        Settings.SoundVolume = value;
        SoundManager.Play(Sound.Coin); // a sample, so the player hears how loud it is now
        RefreshTexts();
    }

    void ToggleShake()
    {
        Settings.ScreenShake = !Settings.ScreenShake; // "!" flips true <-> false
        RefreshTexts();
    }

    void RefreshTexts()
    {
        // 0.8 -> "80%"
        musicLabel.text = "MUSIC   " + Mathf.RoundToInt(Settings.MusicVolume * 100f) + "%";
        soundLabel.text = "SOUND   " + Mathf.RoundToInt(Settings.SoundVolume * 100f) + "%";

        shakeLabel.text = "SCREEN SHAKE: " + (Settings.ScreenShake ? "ON" : "OFF");
        shakeButton.color = Settings.ScreenShake ? OnColor : OffColor;
    }

    void Close()
    {
        Destroy(gameObject);
    }
}
