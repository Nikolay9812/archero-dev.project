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

    // 3 Common Oak Bows (ready to merge), 10 random items and 5 scrolls for every slot
    [MenuItem("Archero/Add Test Gear + Scrolls")]
    static void AddTestGear()
    {
        GearType oakBow = GearCatalog.Find("oak_bow");
        for (int i = 0; i < 3; i++) Inventory.AddGear(oakBow, Rarity.Common);
        for (int i = 0; i < 10; i++) Inventory.AddRandomGear(Rarity.Common);
        for (int slot = 0; slot < GearCatalog.SlotCount; slot++) Inventory.AddScrolls((GearSlot)slot, 5);

        Debug.Log("You now own " + Inventory.All.Count + " pieces of gear.");
    }

    // Finds half of the artifacts (with 15 shards each, enough for one star) and adds 30 wild shards
    [MenuItem("Archero/Add Test Artifacts")]
    static void AddTestArtifacts()
    {
        for (int i = 0; i < ArtifactCatalog.All.Length; i += 2) // i += 2 = every second artifact
        {
            ArtifactType type = ArtifactCatalog.All[i];
            Artifacts.Collect(type);
            Artifacts.AddShards(type, 15);
        }
        Artifacts.AddWildShards(30);

        Debug.Log("Found " + Artifacts.FoundCount + " artifacts, wild shards: " + Artifacts.WildShards);
    }
}
