using UnityEditor;
using UnityEngine;

// Adds "Archero > Delete Save" to Unity's menu bar, to test the game as a brand new player.
public static class SaveTools
{
    [MenuItem("Archero/Delete Save")]
    static void DeleteSave()
    {
        SaveSystem.Delete();
        Debug.Log("Save file deleted: coins are back to 0.");
    }
}
