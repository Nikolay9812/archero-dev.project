using System;
using UnityEngine;

// The player's settings: music volume, sound volume and screen shake.
// Kept in the save file, so they stay the same after closing the game.
// The volumes are on top of the mix in the SoundLibrary: library 0.4 x setting 0.5 = 0.2.
public static class Settings
{
    // SoundManager listens to this, so a moved slider is heard right away
    public static event Action Changed;

    // A property with get AND set: reading it reads the save, writing it saves and tells everyone
    public static float MusicVolume
    {
        get => SaveSystem.Data.musicVolume;
        set
        {
            SaveSystem.Data.musicVolume = Mathf.Clamp01(value); // "value" = what was written into the property
            SaveAndTell();
        }
    }

    public static float SoundVolume
    {
        get => SaveSystem.Data.soundVolume;
        set
        {
            SaveSystem.Data.soundVolume = Mathf.Clamp01(value);
            SaveAndTell();
        }
    }

    public static bool ScreenShake
    {
        get => SaveSystem.Data.screenShake;
        set
        {
            SaveSystem.Data.screenShake = value;
            SaveAndTell();
        }
    }

    static void SaveAndTell()
    {
        SaveSystem.Save();
        Changed?.Invoke();
    }
}
