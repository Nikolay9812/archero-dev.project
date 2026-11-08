using UnityEngine;

// The 7 places where the player can wear something. The number of each slot is used as an index
// in the save file (equipped[] and scrolls[]), so append new slots at the END only.
public enum GearSlot
{
    Weapon,
    Head,
    Armor,
    Hands,
    Boots,
    Ring,
    Amulet
}

// From worst to best. Merging 3 of the same item makes one of the next rarity.
// Saved as a number too, so append only.
public enum Rarity
{
    Common,
    Great,
    Rare,
    Epic,
    Legendary
}

// What a piece of gear makes better
public enum GearStat
{
    Damage,
    MaxHealth,
    FireRate,
    MoveSpeed,
    CoinBonus
}

// One kind of item, like "Oak Bow". The player can own many copies of it, each with its own rarity and level.
// "readonly" = set once in the constructor, never changed afterwards.
public class GearType
{
    public readonly string id;       // saved in the save file, so never rename an id once players have it
    public readonly string title;
    public readonly GearSlot slot;
    public readonly GearStat stat;
    public readonly float baseValue; // the bonus of a Common item at level 1

    public GearType(string id, string title, GearSlot slot, GearStat stat, float baseValue)
    {
        this.id = id; // "this.id" is the field, plain "id" is the parameter with the same name
        this.title = title;
        this.slot = slot;
        this.stat = stat;
        this.baseValue = baseValue;
    }
}

// Every item in the game, and the numbers that turn rarity and level into a bonus.
// To add an item: add one line to All. Nothing else needs to change.
public static class GearCatalog
{
    public static readonly GearType[] All =
    {
        new GearType("oak_bow",        "Oak Bow",        GearSlot.Weapon, GearStat.Damage,    10f),
        new GearType("swift_bow",      "Swift Bow",      GearSlot.Weapon, GearStat.FireRate,  0.25f),
        new GearType("leather_cap",    "Leather Cap",    GearSlot.Head,   GearStat.MaxHealth, 40f),
        new GearType("hawk_hood",      "Hawk Hood",      GearSlot.Head,   GearStat.Damage,    4f),
        new GearType("leather_vest",   "Leather Vest",   GearSlot.Armor,  GearStat.MaxHealth, 80f),
        new GearType("wind_cloak",     "Wind Cloak",     GearSlot.Armor,  GearStat.MoveSpeed, 0.3f),
        new GearType("archer_gloves",  "Archer Gloves",  GearSlot.Hands,  GearStat.FireRate,  0.15f),
        new GearType("iron_gauntlets", "Iron Gauntlets", GearSlot.Hands,  GearStat.MaxHealth, 50f),
        new GearType("swift_boots",    "Swift Boots",    GearSlot.Boots,  GearStat.MoveSpeed, 0.3f),
        new GearType("heavy_boots",    "Heavy Boots",    GearSlot.Boots,  GearStat.MaxHealth, 50f),
        new GearType("ruby_ring",      "Ruby Ring",      GearSlot.Ring,   GearStat.Damage,    5f),
        new GearType("quick_ring",     "Quick Ring",     GearSlot.Ring,   GearStat.FireRate,  0.15f),
        new GearType("life_amulet",    "Life Amulet",    GearSlot.Amulet, GearStat.MaxHealth, 60f),
        new GearType("lucky_amulet",   "Lucky Amulet",   GearSlot.Amulet, GearStat.CoinBonus, 10f)
    };

    public const int SlotCount = 7; // how many values GearSlot has

    // How much stronger each rarity is than Common. Index = rarity number.
    private static readonly float[] RarityMultiplier = { 1f, 1.5f, 2.2f, 3.2f, 4.5f };

    // Chance (in percent) to roll each rarity for a drop. Legendary only comes from merging.
    private static readonly int[] DropChance = { 70, 22, 7, 1, 0 };

    private const float BonusPerLevel = 0.1f; // every level after the first adds 10% of the item's value

    // Finds an item by its id. Returns null if no item has that id (e.g. an item was removed from the game).
    public static GearType Find(string id)
    {
        foreach (GearType type in All)
        {
            if (type.id == id) return type;
        }
        return null;
    }

    public static GearType RandomType()
    {
        return All[Random.Range(0, All.Length)];
    }

    // Rolls a rarity with the chances above. Anything below 'minimum' becomes 'minimum'.
    public static Rarity RandomRarity(Rarity minimum)
    {
        int roll = Random.Range(0, 100); // 0..99
        Rarity result = Rarity.Common;

        // Walk through the chances: 0-69 = Common, 70-91 = Great, 92-98 = Rare, 99 = Epic
        for (int i = 0; i < DropChance.Length; i++)
        {
            if (roll < DropChance[i])
            {
                result = (Rarity)i; // (Rarity) turns the number back into the enum
                break;
            }
            roll -= DropChance[i];
        }

        // Enums are numbers underneath, so they can be compared with < and >
        return result < minimum ? minimum : result;
    }

    // Common items go up to level 10, Great to 20... Legendary to 50
    public static int MaxLevel(Rarity rarity)
    {
        return ((int)rarity + 1) * 10;
    }

    public static float Value(GearType type, Rarity rarity, int level)
    {
        return type.baseValue * RarityMultiplier[(int)rarity] * (1f + (level - 1) * BonusPerLevel);
    }

    // Leveling costs scrolls of the item's slot: 1 scroll up to level 4, 2 up to level 9, 3 up to 14...
    public static int ScrollCost(int level)
    {
        return 1 + level / 5;
    }

    public static int CoinCost(int level)
    {
        return level * 50;
    }

    public static Color RarityColor(Rarity rarity)
    {
        switch (rarity)
        {
            case Rarity.Common: return new Color(0.6f, 0.62f, 0.65f);
            case Rarity.Great: return new Color(0.35f, 0.75f, 0.35f);
            case Rarity.Rare: return new Color(0.3f, 0.55f, 1f);
            case Rarity.Epic: return new Color(0.7f, 0.35f, 0.95f);
            default: return new Color(1f, 0.72f, 0.15f);
        }
    }

    public static string RarityName(Rarity rarity)
    {
        return rarity.ToString().ToUpper(); // the enum's own name: Rarity.Rare -> "RARE"
    }

    public static string SlotName(GearSlot slot)
    {
        return slot.ToString().ToUpper();
    }

    // For example "+12 attack" or "+0.30 arrows/sec"
    public static string FormatBonus(GearStat stat, float value)
    {
        switch (stat)
        {
            case GearStat.Damage: return "+" + Mathf.RoundToInt(value) + " attack";
            case GearStat.MaxHealth: return "+" + Mathf.RoundToInt(value) + " HP";
            case GearStat.FireRate: return "+" + value.ToString("0.00") + " arrows/sec";
            case GearStat.MoveSpeed: return "+" + value.ToString("0.0") + " speed";
            default: return "+" + Mathf.RoundToInt(value) + "% coins";
        }
    }
}
