using UnityEngine;

// What a reward is. Saved nowhere, so the order can change freely.
public enum ShopRewardType
{
    Gems,
    Coins,
    Crystals,
    Scrolls,
    ChestKey,
    WildShards,
    Gear
}

// One thing a shop offer gives, for example "500 gems" or "1 Epic gear".
// Made with the small helpers at the top: Reward.Gems(500), Reward.Gear(Rarity.Epic, 1)...
public class Reward
{
    public ShopRewardType type;
    public int amount;
    public bool anySlot = true;  // Scrolls: true = a random slot when it is given
    public GearSlot slot;        // Scrolls: the slot when anySlot is false
    public Rarity rarity;        // Gear
    public string gearId;        // Gear: null = a random item
    public string chestId;       // ChestKey: which chest the key opens

    // ---------- Making rewards ----------
    // "new Reward { type = ..., amount = ... }" makes a Reward and sets these fields in one go.

    public static Reward Gems(int amount) => new Reward { type = ShopRewardType.Gems, amount = amount };
    public static Reward Coins(int amount) => new Reward { type = ShopRewardType.Coins, amount = amount };
    public static Reward Crystals(int amount) => new Reward { type = ShopRewardType.Crystals, amount = amount };
    public static Reward WildShards(int amount) => new Reward { type = ShopRewardType.WildShards, amount = amount };
    public static Reward Key(string chestId, int amount) => new Reward { type = ShopRewardType.ChestKey, chestId = chestId, amount = amount };
    public static Reward Gear(Rarity rarity, int amount) => new Reward { type = ShopRewardType.Gear, rarity = rarity, amount = amount };
    public static Reward Gear(string gearId, Rarity rarity) => new Reward { type = ShopRewardType.Gear, gearId = gearId, rarity = rarity, amount = 1 };

    // Scrolls for one random slot
    public static Reward Scrolls(int amount) => new Reward { type = ShopRewardType.Scrolls, amount = amount };
    public static Reward Scrolls(GearSlot slot, int amount) => new Reward { type = ShopRewardType.Scrolls, anySlot = false, slot = slot, amount = amount };

    // ---------- Giving it ----------

    // Gives the reward to the player and returns a line for the "You got" window, e.g. "+500 Gems"
    public string Give()
    {
        switch (type)
        {
            case ShopRewardType.Gems:
                Wallet.AddGems(amount);
                return "+" + amount + " Gems";

            case ShopRewardType.Coins:
                Wallet.Add(amount);
                return "+" + amount + " Coins";

            case ShopRewardType.Crystals:
                Wallet.AddCrystals(amount);
                return "+" + amount + " Crystals";

            case ShopRewardType.WildShards:
                Artifacts.AddWildShards(amount);
                return "+" + amount + " Artifact Shards";

            case ShopRewardType.ChestKey:
                Shop.AddKeys(chestId, amount);
                return "+" + amount + " " + ShopCatalog.FindChest(chestId).title + " Key";

            case ShopRewardType.Scrolls:
                // (GearSlot) turns a random number 0..6 into a slot
                GearSlot scrollSlot = anySlot ? (GearSlot)Random.Range(0, GearCatalog.SlotCount) : slot;
                Inventory.AddScrolls(scrollSlot, amount);
                return "+" + amount + " " + ShopCatalog.SlotTitle(scrollSlot) + " Scrolls";

            default: // Gear: every item is its own line, in its rarity color
                string lines = "";
                for (int i = 0; i < amount; i++)
                {
                    GearType gearType = gearId == null ? GearCatalog.RandomType() : GearCatalog.Find(gearId);
                    Inventory.AddGear(gearType, rarity);
                    if (i > 0) lines += "\n";
                    lines += ShopCatalog.Colored(GearCatalog.RarityName(rarity) + " " + gearType.title, GearCatalog.RarityColor(rarity));
                }
                return lines;
        }
    }

    // ---------- How it looks on a tile ----------

    // The amount, short: 14000 -> "14K", 1800 stays "1800"
    public string ShortAmount()
    {
        if (amount >= 10000 && amount % 1000 == 0) return amount / 1000 + "K";
        if (amount >= 10000) return (amount / 1000f).ToString("0.#") + "K";
        return amount.ToString();
    }

    // The big word in the middle of the tile
    public string Symbol()
    {
        switch (type)
        {
            case ShopRewardType.Gems: return "GEMS";
            case ShopRewardType.Coins: return "COINS";
            case ShopRewardType.Crystals: return "CRYSTAL";
            case ShopRewardType.WildShards: return "SHARDS";
            case ShopRewardType.ChestKey: return "KEY";
            case ShopRewardType.Scrolls: return anySlot ? "SCROLL\n?" : "SCROLL\n" + ShopCatalog.SlotTitle(slot).ToUpper();
            default: return gearId == null ? GearCatalog.RarityName(rarity) + "\nGEAR" : GearCatalog.Find(gearId).title.ToUpper().Replace(' ', '\n');
        }
    }

    public Color TileColor()
    {
        switch (type)
        {
            case ShopRewardType.Gems: return new Color(0.2f, 0.6f, 0.45f);
            case ShopRewardType.Coins: return new Color(0.75f, 0.6f, 0.15f);
            case ShopRewardType.Crystals: return new Color(0.85f, 0.4f, 0.75f);
            case ShopRewardType.WildShards: return new Color(0.55f, 0.3f, 0.8f);
            case ShopRewardType.ChestKey: return ShopCatalog.FindChest(chestId).color;
            case ShopRewardType.Scrolls: return new Color(0.3f, 0.4f, 0.75f);
            default: return GearCatalog.RarityColor(rarity);
        }
    }
}
