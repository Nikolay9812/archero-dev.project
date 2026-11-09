using System;
using System.Collections.Generic;
using UnityEngine;

// The player's artifacts: which ones she has found, their stars and shards, wishing and starring up.
// Every found artifact counts all the time: nothing has to be worn. Kept in the save file, like Inventory.
public static class Artifacts
{
    // The artifact page listens to this, so it can redraw after a wish or a star up
    public static event Action Changed;

    // Found since the chapter started (GameOverScreen shows it at the end)
    public static int WildShardsFoundThisRun { get; private set; }

    public static int WildShards => SaveSystem.Data.wildShards;

    private static List<OwnedArtifact> Owned => SaveSystem.Data.artifacts;

    // ChapterManager calls this when a chapter starts
    public static void StartRun()
    {
        WildShardsFoundThisRun = 0;
    }

    // ---------- Reading ----------

    // The saved data of an artifact, or null if she hasn't found it yet
    public static OwnedArtifact Get(ArtifactType type)
    {
        foreach (OwnedArtifact owned in Owned)
        {
            if (owned.id == type.id) return owned;
        }
        return null;
    }

    public static bool IsFound(ArtifactType type) => Get(type) != null;

    public static int Stars(ArtifactType type)
    {
        OwnedArtifact owned = Get(type);
        return owned != null ? owned.stars : 0;
    }

    public static int Shards(ArtifactType type)
    {
        OwnedArtifact owned = Get(type);
        return owned != null ? owned.shards : 0;
    }

    public static int FoundCount => Owned.Count;

    public static bool IsMaxStars(ArtifactType type) => Stars(type) >= ArtifactCatalog.MaxStars;

    // Own shards + wild shards are enough for the next star
    public static bool CanStarUp(ArtifactType type)
    {
        if (!IsFound(type) || IsMaxStars(type)) return false;
        return Shards(type) + WildShards >= ArtifactCatalog.StarCost(Stars(type));
    }

    // ---------- Stats ----------

    // All found artifacts that give this stat, plus the active set bonuses, added up.
    // The player scripts read this when a run starts.
    public static float Bonus(ArtifactStat stat)
    {
        float total = ArtifactSets.Bonus(stat);
        foreach (OwnedArtifact owned in Owned)
        {
            ArtifactType type = ArtifactCatalog.Find(owned.id);
            if (type != null) total += type.Value(stat, owned.stars); // null = an artifact removed from the game
        }
        return total;
    }

    public static int BonusInt(ArtifactStat stat)
    {
        return Mathf.RoundToInt(Bonus(stat));
    }

    // A percent stat as a multiplier: +5% -> 1.05. Handy for "damage * Multiplier(...)".
    public static float Multiplier(ArtifactStat stat)
    {
        return 1f + Bonus(stat) / 100f;
    }

    // ---------- Getting new things ----------

    // A new artifact is found with 0 stars. Finding one she already has gives shards instead.
    // Returns how many shards it gave (0 = it was new).
    public static int Collect(ArtifactType type)
    {
        OwnedArtifact owned = Get(type);
        int shards = 0;

        if (owned == null)
        {
            owned = new OwnedArtifact();
            owned.id = type.id;
            Owned.Add(owned);
        }
        else
        {
            shards = ArtifactCatalog.ShardsForDuplicate(type.rarity);
            owned.shards += shards;
        }

        SaveAndTell();
        return shards;
    }

    public static void AddShards(ArtifactType type, int amount)
    {
        OwnedArtifact owned = Get(type);
        if (owned == null) return; // shards only make sense for a found artifact

        owned.shards += amount;
        SaveAndTell();
    }

    public static void AddWildShards(int amount)
    {
        if (amount <= 0) return;

        SaveSystem.Data.wildShards += amount;
        WildShardsFoundThisRun += amount;
        SaveAndTell();
    }

    // Pays the gems and collects a random artifact. Returns false (and takes nothing) without enough gems.
    // "out" gives the caller two more results: which artifact, and how many shards (0 = a new one).
    public static bool TryWish(out ArtifactType won, out int shards)
    {
        won = null;
        shards = 0;
        if (!Wallet.SpendGems(ArtifactCatalog.WishPrice)) return false;

        WishResult result = WishOnce();
        won = result.artifact;
        shards = result.shards;
        return true;
    }

    // What one wish gave
    public class WishResult
    {
        public ArtifactType artifact;
        public int shards; // 0 = it was a new artifact
    }

    // Several wishes at once (the shop's "Wish x10"). Pays for all of them first, or for none.
    public static bool TryWish(int count, List<WishResult> results)
    {
        if (!Wallet.SpendGems(ArtifactCatalog.WishPrice * count)) return false;

        for (int i = 0; i < count; i++) results.Add(WishOnce());
        return true;
    }

    // How many more wishes until Epic (or Legendary) is guaranteed. The shop shows these.
    public static int WishesUntilEpic => ArtifactCatalog.EpicWishPity - SaveSystem.Data.wishesWithoutEpic;
    public static int WishesUntilLegendary => ArtifactCatalog.LegendaryWishPity - SaveSystem.Data.wishesWithoutLegendary;

    // One wish, already paid for. Counts the bad luck: after too many wishes without Epic, the next one is Epic.
    static WishResult WishOnce()
    {
        SaveData data = SaveSystem.Data;

        Rarity minimum = Rarity.Common;
        if (data.wishesWithoutEpic + 1 >= ArtifactCatalog.EpicWishPity) minimum = Rarity.Epic;
        if (data.wishesWithoutLegendary + 1 >= ArtifactCatalog.LegendaryWishPity) minimum = Rarity.Legendary;

        WishResult result = new WishResult();
        result.artifact = ArtifactCatalog.RandomForWish(minimum);

        // Lucky wish = start counting again. Legendary counts as "Epic or better" too.
        data.wishesWithoutEpic = result.artifact.rarity >= Rarity.Epic ? 0 : data.wishesWithoutEpic + 1;
        data.wishesWithoutLegendary = result.artifact.rarity >= Rarity.Legendary ? 0 : data.wishesWithoutLegendary + 1;

        result.shards = Collect(result.artifact); // this also saves
        return result;
    }

    // Uses the artifact's own shards first, the wild shards only for what is missing
    public static bool TryStarUp(ArtifactType type)
    {
        if (!CanStarUp(type)) return false;

        OwnedArtifact owned = Get(type);
        int cost = ArtifactCatalog.StarCost(owned.stars);

        int fromOwn = Mathf.Min(owned.shards, cost);
        owned.shards -= fromOwn;
        SaveSystem.Data.wildShards -= cost - fromOwn;
        owned.stars++;

        SaveAndTell();
        return true;
    }

    private static void SaveAndTell()
    {
        SaveSystem.Save();
        Changed?.Invoke();
    }
}
