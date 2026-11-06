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

    // A new save goes from level 1 to level 5 with 700 XP (100 + 150 + 200 + 250), which unlocks the first draws.
    // Use this outside Play mode: the main menu only reads the level when it opens.
    [MenuItem("Archero/Add 700 Profile XP + 2000 Coins + 100 Gems")]
    static void AddTestRewards()
    {
        PlayerProfile.AddXp(700);
        Wallet.Add(2000);
        Wallet.AddGems(100);
        Debug.Log("Profile level is now " + PlayerProfile.Level + ", coins: " + Wallet.Coins);
    }
}
