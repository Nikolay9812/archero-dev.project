using System.Collections.Generic;
using UnityEngine;

// What an artifact can make better. Every artifact has Attack + MaxHealth ("base stats")
// and one of the others as its "special stat". Not saved as a number, so the order may change.
public enum ArtifactStat
{
    Attack,             // flat damage, like the talents and gear
    MaxHealth,          // flat HP
    AttackPercent,      // all damage x (1 + percent / 100)
    HealthPercent,
    AttackSpeedPercent,
    MoveSpeedPercent,
    CritChance,         // chance in percent that an arrow hits twice as hard (or harder, see CritDamage)
    CritDamage,         // extra damage of a critical hit, in percent
    Dodge,              // chance in percent that an enemy hit misses her
    BossDamage,         // extra damage against bosses, in percent
    CoinBonus           // extra coins from enemies, in percent
}

// One artifact, like "Sun Medal". Each artifact exists only once: collecting it again gives shards,
// and shards add stars. The values here are for 0 stars; every star makes them 50% bigger.
public class ArtifactType
{
    public readonly string id;          // saved in the save file, so never rename an id once players have it
    public readonly string title;
    public readonly Rarity rarity;
    public readonly float attack;       // base stat
    public readonly float health;       // base stat
    public readonly ArtifactStat special;
    public readonly float specialValue;
    public readonly string description;

    public ArtifactType(string id, string title, Rarity rarity, float attack, float health,
                        ArtifactStat special, float specialValue, string description)
    {
        this.id = id;
        this.title = title;
        this.rarity = rarity;
        this.attack = attack;
        this.health = health;
        this.special = special;
        this.specialValue = specialValue;
        this.description = description;
    }

    // How much of 'stat' this artifact gives with this many stars (0 if it doesn't give that stat at all)
    public float Value(ArtifactStat stat, int stars)
    {
        float value = 0f;
        if (stat == ArtifactStat.Attack) value += attack;
        if (stat == ArtifactStat.MaxHealth) value += health;
        if (stat == special) value += specialValue;
        return value * ArtifactCatalog.StarMultiplier(stars);
    }
}

// Every artifact in the game, and the rules for stars, shards and wishes.
// To add an artifact: add one line to All. Nothing else needs to change.
public static class ArtifactCatalog
{
    public static readonly ArtifactType[] All =
    {
        //                 id               title            rarity            ATK  HP   special                          value
        new ArtifactType("sun_medal",     "Sun Medal",     Rarity.Legendary, 6f, 40f, ArtifactStat.AttackPercent,      5f,
            "Warm to the touch, even in the middle of the night."),
        new ArtifactType("storm_jar",     "Storm Jar",     Rarity.Legendary, 6f, 40f, ArtifactStat.CritChance,         5f,
            "Someone put a thunderstorm in a jar. It still rumbles."),
        new ArtifactType("giants_tooth",  "Giant's Tooth", Rarity.Legendary, 6f, 40f, ArtifactStat.BossDamage,         10f,
            "Taken from a giant who lost an argument. Bosses remember it."),
        new ArtifactType("moon_mirror",   "Moon Mirror",   Rarity.Legendary, 6f, 40f, ArtifactStat.Dodge,              4f,
            "It shows where the arrow will be. Just step aside."),

        new ArtifactType("owl_feather",   "Owl Feather",   Rarity.Epic,      4f, 25f, ArtifactStat.AttackSpeedPercent, 4f,
            "Light as a breath, quick as a blink."),
        new ArtifactType("iron_acorn",    "Iron Acorn",    Rarity.Epic,      4f, 25f, ArtifactStat.HealthPercent,      5f,
            "It will never grow into a tree. It refuses."),
        new ArtifactType("lucky_clover",  "Lucky Clover",  Rarity.Epic,      4f, 25f, ArtifactStat.CoinBonus,          10f,
            "Four leaves. One of them is a little bent."),
        new ArtifactType("old_compass",   "Old Compass",   Rarity.Epic,      4f, 25f, ArtifactStat.MoveSpeedPercent,   4f,
            "It always points to where you wanted to go anyway."),

        new ArtifactType("ember_stone",   "Ember Stone",   Rarity.Rare,      2f, 15f, ArtifactStat.CritDamage,         15f,
            "Hot enough to cook an egg on. Please don't."),
        new ArtifactType("frost_bell",    "Frost Bell",    Rarity.Rare,      2f, 15f, ArtifactStat.HealthPercent,      3f,
            "It rings without a sound. Only snowflakes hear it."),
        new ArtifactType("river_pebble",  "River Pebble",  Rarity.Rare,      2f, 15f, ArtifactStat.AttackPercent,      3f,
            "Perfectly round. The river worked on it for a hundred years."),
        new ArtifactType("fox_tail",      "Fox Tail",      Rarity.Rare,      2f, 15f, ArtifactStat.Dodge,              2f,
            "Fluffy, and very hard to catch.")
    };

    public const int MaxStars = 5;
    public const int WishPrice = 10; // gems per wish

    // Bad luck protection: at the latest the 8th wish is Epic or better, the 40th Legendary
    public const int EpicWishPity = 8;
    public const int LegendaryWishPity = 40;

    // Chance (in percent) of each rarity for one wish. Index = rarity number (Common, Great, Rare, Epic, Legendary).
    public static readonly int[] WishChance = { 0, 0, 60, 30, 10 };

    // Shards for wishing an artifact you already have. Rarer artifacts give fewer.
    private static readonly int[] DuplicateShards = { 10, 10, 10, 6, 4 };

    public static ArtifactType Find(string id)
    {
        foreach (ArtifactType type in All)
        {
            if (type.id == id) return type;
        }
        return null;
    }

    // 0 stars = x1, 1 star = x1.5, 2 stars = x2 ... 5 stars = x3.5
    public static float StarMultiplier(int stars)
    {
        return 1f + stars * 0.5f;
    }

    // Shards needed for the next star: 10 for the 1st star, 20 for the 2nd... 50 for the 5th
    public static int StarCost(int stars)
    {
        return (stars + 1) * 10;
    }

    public static int ShardsForDuplicate(Rarity rarity)
    {
        return DuplicateShards[(int)rarity];
    }

    // Rolls a rarity with the wish chances (same idea as GearCatalog.RandomRarity), then a random artifact of it.
    // Anything below 'minimum' becomes 'minimum' (used for the bad luck protection).
    public static ArtifactType RandomForWish(Rarity minimum)
    {
        int roll = Random.Range(0, 100);
        Rarity rarity = Rarity.Rare;
        for (int i = 0; i < WishChance.Length; i++)
        {
            if (roll < WishChance[i])
            {
                rarity = (Rarity)i;
                break;
            }
            roll -= WishChance[i];
        }
        if (rarity < minimum) rarity = minimum;

        // Collect every artifact of that rarity, then pick one of them
        List<ArtifactType> possible = new List<ArtifactType>();
        foreach (ArtifactType type in All)
        {
            if (type.rarity == rarity) possible.Add(type);
        }
        if (possible.Count == 0) return All[Random.Range(0, All.Length)]; // no artifact of that rarity: any one

        return possible[Random.Range(0, possible.Count)];
    }

    public static bool IsPercent(ArtifactStat stat)
    {
        return stat != ArtifactStat.Attack && stat != ArtifactStat.MaxHealth;
    }

    public static string StatName(ArtifactStat stat)
    {
        switch (stat)
        {
            case ArtifactStat.Attack: return "ATK";
            case ArtifactStat.MaxHealth: return "Max HP";
            case ArtifactStat.AttackPercent: return "ATK";
            case ArtifactStat.HealthPercent: return "Max HP";
            case ArtifactStat.AttackSpeedPercent: return "ATK SPD";
            case ArtifactStat.MoveSpeedPercent: return "MOV SPD";
            case ArtifactStat.CritChance: return "Crit Chance";
            case ArtifactStat.CritDamage: return "Crit DMG";
            case ArtifactStat.Dodge: return "Dodge";
            case ArtifactStat.BossDamage: return "Boss DMG";
            default: return "Coins";
        }
    }

    // Just the number part: "+12" or "+4.5%"
    public static string FormatValue(ArtifactStat stat, float value)
    {
        if (IsPercent(stat)) return "+" + value.ToString("0.#") + "%"; // "0.#" = a decimal only when needed
        return "+" + Mathf.RoundToInt(value);
    }

    // The whole line: "ATK +12" or "Boss DMG +10%"
    public static string Format(ArtifactStat stat, float value)
    {
        return StatName(stat) + " " + FormatValue(stat, value);
    }
}
