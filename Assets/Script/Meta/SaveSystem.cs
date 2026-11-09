using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Everything that must survive closing the game. To save something new later, just add a field here.
// [Serializable] lets JsonUtility turn this class into text and back. The file looks like: {"coins":120,"gems":3}
// An older save file without a field just keeps the starting value written here (0 if none is written).
[Serializable]
public class SaveData
{
    public int coins;
    public int gems;
    public int profileLevel = 1;
    public int profileXp;

    // Levels of the talents on the Collectible page (see Upgrades)
    public int attackUpgrade;
    public int healthUpgrade;
    public int speedUpgrade;
    public int moveUpgrade;
    public int roomHealUpgrade;
    public int coinUpgrade;

    // Crystals: the currency bought with real money, which can pay for any paid shop offer (see Wallet)
    public int crystals;

    // The shop (see Shop): how often each offer was bought, and the gear chests' keys, free opens and luck counters
    public List<ShopPurchase> shopPurchases = new List<ShopPurchase>();
    public List<ChestState> chests = new List<ChestState>();

    // Wishes in a row without an Epic / Legendary artifact. A wish after enough bad luck is guaranteed better.
    public int wishesWithoutEpic;
    public int wishesWithoutLegendary;

    // Gear (see Inventory). JsonUtility can save lists and arrays of [Serializable] classes too.
    public List<OwnedGear> gear = new List<OwnedGear>();
    public int[] equipped = new int[GearCatalog.SlotCount]; // the id of the item worn in each slot, 0 = empty
    public int[] scrolls = new int[GearCatalog.SlotCount];  // scrolls owned for each slot
    public int nextGearId = 1;                               // every new item gets its own id number

    // Artifacts (see Artifacts). Only found artifacts are in the list.
    public List<OwnedArtifact> artifacts = new List<OwnedArtifact>();
    public int wildShards; // shards that count for ANY artifact

    // Player settings (see Settings). 1 = full volume, 0 = off.
    public float musicVolume = 1f;
    public float soundVolume = 1f;
    public bool screenShake = true;
}

// One item the player owns. Two "Oak Bow"s are two OwnedGear objects with different ids.
[Serializable]
public class OwnedGear
{
    public int id;
    public string type; // the id of its GearType, e.g. "oak_bow"
    public Rarity rarity;
    public int level = 1;
}

// One artifact the player has found. Unlike gear there is only one of each.
[Serializable]
public class OwnedArtifact
{
    public string id; // the id of its ArtifactType, e.g. "sun_medal"
    public int stars;
    public int shards; // collected towards the next star
}

// How often one shop offer was bought in one period. 'period' is the day/week/month number from Shop.PeriodKey,
// so when a new day starts the old record no longer counts: the offer is back without resetting anything.
[Serializable]
public class ShopPurchase
{
    public string id;    // the offer's id, e.g. "weekly_2"
    public int period;
    public int count;
}

// One gear chest in the shop
[Serializable]
public class ChestState
{
    public string id;               // the ChestType's id, e.g. "royal"
    public int keys;                // a key opens the chest once without paying
    public long lastFreeOpen;       // when it was last opened for free (DateTime ticks, UTC; 0 = never)
    public int opensWithoutEpic;    // opens in a row without Epic or better
    public int opensWithoutLegendary;
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
