using System;
using System.IO;
using UnityEngine;

// Everything that must survive closing the game. To save something new later, just add a field here.
// [Serializable] lets JsonUtility turn this class into text and back. The file looks like: {"coins":120}
[Serializable]
public class SaveData
{
    public int coins;
}

// Reads and writes the save file. "static" = there is only one, it needs no GameObject,
// and it keeps working when a new scene loads.
public static class SaveSystem
{
    private static SaveData data;

    // persistentDataPath is a folder Unity gives every game for its own files, on PC and on phones
    private static string FilePath => Path.Combine(Application.persistentDataPath, "save.json");

    // The save data. The first time anyone asks for it, it is read from the file.
    public static SaveData Data
    {
        get
        {
            if (data == null) Load();
            return data;
        }
    }

    public static void Save()
    {
        File.WriteAllText(FilePath, JsonUtility.ToJson(Data));
    }

    // Starts over with an empty save (used by the "Archero > Delete Save" menu while testing)
    public static void Delete()
    {
        File.Delete(FilePath); // no error if the file doesn't exist
        data = null;
    }

    static void Load()
    {
        data = new SaveData(); // first time playing: no file yet, so start empty
        if (!File.Exists(FilePath)) return;

        // A damaged file makes FromJson throw an error. "try/catch" catches it,
        // so the game still starts (with an empty save) instead of crashing every time.
        try
        {
            SaveData loaded = JsonUtility.FromJson<SaveData>(File.ReadAllText(FilePath));
            if (loaded != null) data = loaded; // an empty file gives null: keep the empty save
        }
        catch (Exception error)
        {
            Debug.LogWarning("Could not read the save file: " + error.Message);
        }
    }
}
