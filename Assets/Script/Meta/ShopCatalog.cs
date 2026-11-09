using System.Collections.Generic;
using UnityEngine;

// What an offer costs. "Money" = real money (euros), which is FREE while Shop.PaymentsAreMocked is true.
public enum PriceType
{
    Free,
    Gems,
    Coins,
    Money
}

// How often the "Purchases left" of an offer come back
public enum ShopPeriod
{
    Unlimited, // no limit at all
    OneTime,   // never comes back
    Daily,     // every day at midnight
    Weekly,    // every Monday at midnight
    Monthly    // on the 1st of every month
}

// One thing for sale in the shop: what it gives, what it costs and how often it can be bought
public class ShopOffer
{
    public string id;             // must be unique: the save file counts purchases by it
    public string title = "";
    public Reward[] rewards;
    public PriceType priceType;
    public int price;             // gems, coins, or for Money: cents (479 = 4,79 €)
    public int limit;             // purchases per period (ignored when Unlimited)
    public ShopPeriod period;
    public int valuePercent;      // the "500% Value" badge, 0 = no badge
    public int discountPercent;   // the "20% Off" badge, already taken off 'price'
    public bool doubleFirstTime;  // gem packs: the first purchase gives the gems twice ("Bonus")
}

// One gear chest. The chances are percent per rarity: index = rarity number (Common, Great, Rare, Epic, Legendary).
public class ChestType
{
    public string id;
    public string title;
    public string description;
    public int[] chance;
    public int price;          // gems for one open
    public int freeHours;      // can be opened for free every so many hours, 0 = never
    public int epicWithin;     // bad luck protection: Epic or better at the latest on this open, 0 = none
    public int legendaryWithin;
    public bool hasFocus;      // shows the focus tabs (WEAPONS / ARMOR / JEWELRY)
    public Color color;
}

// Everything the shop sells. To add an offer: add one line to a list. To change a price: change a number.
public static class ShopCatalog
{
    // ---------- Gear chests ----------

    public static readonly ChestType HeroChest = new ChestType
    {
        id = "hero", title = "Hero Chest", description = "1 Great, Rare, Epic or Legendary item.\nYour FOCUS slots come up more often.",
        chance = new[] { 0, 60, 30, 9, 1 }, price = 15, epicWithin = 10, legendaryWithin = 30, hasFocus = true,
        color = new Color(0.55f, 0.3f, 0.8f)
    };

    public static readonly ChestType PrismChest = new ChestType
    {
        id = "prism", title = "Prism Chest", description = "1 Rare, Epic or Legendary item.\nHigher chance of Legendary.",
        chance = new[] { 0, 0, 70, 25, 5 }, price = 30, epicWithin = 9, legendaryWithin = 25,
        color = new Color(0.9f, 0.45f, 0.35f)
    };

    public static readonly ChestType BronzeChest = new ChestType
    {
        id = "bronze", title = "Bronze Chest", description = "1 Common or Great item",
        chance = new[] { 70, 30, 0, 0, 0 }, price = 2, freeHours = 24,
        color = new Color(0.3f, 0.5f, 0.8f)
    };

    public static readonly ChestType RoyalChest = new ChestType
    {
        id = "royal", title = "Royal Chest", description = "1 Great, Rare or Epic item",
        chance = new[] { 0, 60, 30, 10, 0 }, price = 8, freeHours = 72, epicWithin = 9,
        color = new Color(0.6f, 0.3f, 0.75f)
    };

    public static readonly ChestType[] Chests = { HeroChest, PrismChest, BronzeChest, RoyalChest };

    // The Hero Chest's focus tabs: which slots each tab prefers
    public static readonly string[] FocusNames = { "WEAPONS", "ARMOR", "JEWELRY" };
    public static readonly GearSlot[][] FocusSlots =
    {
        new[] { GearSlot.Weapon, GearSlot.Hands },
        new[] { GearSlot.Head, GearSlot.Armor, GearSlot.Boots },
        new[] { GearSlot.Ring, GearSlot.Amulet }
    };

    public static ChestType FindChest(string id)
    {
        foreach (ChestType chest in Chests)
        {
            if (chest.id == id) return chest;
        }
        return null;
    }

    // ---------- Chapter packs (the slider at the top) ----------

    public static readonly ShopOffer[] ChapterPacks =
    {
        Pack("chapter_1", 479, 1, ShopPeriod.OneTime, 600, Reward.Gems(300), Reward.Gear(Rarity.Epic, 1), Reward.WildShards(10), Reward.Key("hero", 2)),
        Pack("chapter_2", 949, 1, ShopPeriod.OneTime, 600, Reward.Gems(600), Reward.Gear(Rarity.Epic, 1), Reward.WildShards(20), Reward.Key("hero", 4)),
        Pack("chapter_3", 1899, 1, ShopPeriod.OneTime, 600, Reward.Gems(1200), Reward.Gear(Rarity.Epic, 2), Reward.WildShards(30), Reward.Key("hero", 6)),
        Pack("chapter_4", 4999, 1, ShopPeriod.OneTime, 600, Reward.Gems(3000), Reward.Gear(Rarity.Legendary, 1), Reward.WildShards(50), Reward.Key("hero", 8)),
        Pack("chapter_5", 9499, 1, ShopPeriod.OneTime, 600, Reward.Gems(6000), Reward.Gear(Rarity.Legendary, 2), Reward.WildShards(80), Reward.Key("hero", 10))
    };

    // ---------- Limited offers ----------

    public static readonly ShopOffer[] DailySpecials =
    {
        Pack("daily_0", 0, 1, ShopPeriod.Daily, 0, Reward.Gems(2), Reward.Coins(100), Reward.Scrolls(2)),
        Pack("daily_1", 479, 1, ShopPeriod.Daily, 450, Reward.Gems(50), Reward.Key("hero", 1), Reward.Key("royal", 3), Reward.Scrolls(30)),
        Pack("daily_2", 1699, 1, ShopPeriod.Daily, 450, Reward.Gems(180), Reward.WildShards(10), Reward.Coins(6000), Reward.Scrolls(15)),
        Pack("daily_3", 1899, 1, ShopPeriod.Daily, 450, Reward.Gems(250), Reward.Gear(Rarity.Rare, 1), Reward.Coins(8000), Reward.Scrolls(20)),
        Pack("daily_4", 2899, 1, ShopPeriod.Daily, 450, Reward.Gems(390), Reward.WildShards(15), Reward.Coins(12000), Reward.Key("hero", 2)),
        Pack("daily_5", 4999, 1, ShopPeriod.Daily, 450, Reward.Gems(650), Reward.Gear(Rarity.Epic, 1), Reward.Coins(20000), Reward.Scrolls(50))
    };

    public static readonly ShopOffer[] WeeklySpecials =
    {
        Pack("weekly_0", 0, 1, ShopPeriod.Weekly, 0, Reward.Gems(2), Reward.Coins(200), Reward.Scrolls(2)),
        Pack("weekly_1", 1699, 1, ShopPeriod.Weekly, 500, Reward.Gems(180), Reward.Key("hero", 3), Reward.Coins(6000), Reward.Scrolls(15)),
        Pack("weekly_2", 4999, 1, ShopPeriod.Weekly, 500, Reward.Gems(650), Reward.Key("hero", 10), Reward.Coins(20000), Reward.Scrolls(50)),
        Pack("weekly_3", 4999, 1, ShopPeriod.Weekly, 500, Reward.Gems(650), Reward.Coins(20000), Reward.Scrolls(50)),
        Pack("weekly_4", 9499, 1, ShopPeriod.Weekly, 500, Reward.Gems(1400), Reward.Coins(40000), Reward.Scrolls(100)),
        Pack("weekly_5", 9499, 1, ShopPeriod.Weekly, 500, Reward.Gems(1400), Reward.Gear(Rarity.Legendary, 1), Reward.Key("hero", 3), Reward.Coins(40000))
    };

    public static readonly ShopOffer[] MonthlySpecials =
    {
        Pack("monthly_0", 0, 1, ShopPeriod.Monthly, 0, Reward.Gems(3), Reward.Coins(300), Reward.Scrolls(3)),
        Pack("monthly_1", 2899, 1, ShopPeriod.Monthly, 550, Reward.Gems(390), Reward.Key("hero", 8), Reward.Coins(12000), Reward.Scrolls(30)),
        Pack("monthly_2", 9499, 1, ShopPeriod.Monthly, 550, Reward.Gems(1400)),
        Pack("monthly_3", 9499, 3, ShopPeriod.Monthly, 550, Reward.Gems(1400), Reward.Coins(40000)),
        Pack("monthly_4", 9499, 5, ShopPeriod.Monthly, 550, Reward.Gems(1400), Reward.WildShards(40)),
        Pack("monthly_5", 9499, 2, ShopPeriod.Monthly, 550, Reward.Gems(1400), Reward.Key("royal", 10), Reward.Coins(40000), Reward.Scrolls(100))
    };

    // ---------- Top up ----------

    public static readonly ShopOffer[] GemPacks =
    {
        GemPack("gems_0", "Handful of Gems", 10, 89),
        GemPack("gems_1", "Pouch of Gems", 50, 479),
        GemPack("gems_2", "Sack of Gems", 120, 949),
        GemPack("gems_3", "Bag of Gems", 250, 1899),
        GemPack("gems_4", "Barrel of Gems", 650, 4999),
        GemPack("gems_5", "Crate of Gems", 1400, 9499)
    };

    public static readonly ShopOffer[] CoinPacks =
    {
        CoinPack("coins_0", "Coin Pouch", 250, 2),
        CoinPack("coins_1", "Coin Sack", 2500, 20),
        CoinPack("coins_2", "Coin Vault", 25000, 200)
    };

    public static readonly ShopOffer[] CrystalPacks =
    {
        CrystalPack("crystals_0", 99, 89, 30),
        CrystalPack("crystals_1", 499, 479, 30),
        CrystalPack("crystals_2", 999, 949, 0),
        CrystalPack("crystals_3", 1999, 1899, 0),
        CrystalPack("crystals_4", 4999, 4999, 0),
        CrystalPack("crystals_5", 9999, 9499, 0)
    };

    // ---------- Daily shop ----------

    public const int DailyShopSize = 6;
    private static readonly int[] Discounts = { 0, 0, 10, 20, 30, 40 };

    // Six random offers for one day. The same day number always gives the same six offers,
    // because System.Random with the same "seed" makes the same numbers every time.
    // So nothing has to be saved, and at midnight the shop is new by itself.
    public static List<ShopOffer> DailyShop(int day)
    {
        System.Random random = new System.Random(day);
        List<ShopOffer> offers = new List<ShopOffer>();

        for (int i = 0; i < DailyShopSize; i++)
        {
            ShopOffer offer = RandomDailyOffer(random);
            offer.id = "shop_" + i;
            offer.limit = 1;
            offer.period = ShopPeriod.Daily;

            // Take the discount off the price, but never below 1
            offer.discountPercent = Discounts[random.Next(Discounts.Length)];
            offer.price = Mathf.Max(1, offer.price * (100 - offer.discountPercent) / 100);

            offers.Add(offer);
        }
        return offers;
    }

    static ShopOffer RandomDailyOffer(System.Random random)
    {
        GearSlot slot = (GearSlot)random.Next(GearCatalog.SlotCount);
        GearType gear = GearCatalog.All[random.Next(GearCatalog.All.Length)];

        switch (random.Next(8)) // random.Next(8) = 0..7
        {
            case 0: return Sale("Royal Chest Key", PriceType.Gems, 6, Reward.Key("royal", 1));
            case 1: return Sale("Hero Chest Key", PriceType.Gems, 12, Reward.Key("hero", 1));
            case 2: return Sale("Artifact Shards", PriceType.Gems, 8, Reward.WildShards(5));
            case 3: return Sale(SlotTitle(slot) + " Scroll", PriceType.Gems, 3, Reward.Scrolls(slot, 10));
            case 4: return Sale(SlotTitle(slot) + " Scroll", PriceType.Coins, 300, Reward.Scrolls(slot, 3));
            case 5: return Sale(SlotTitle(slot) + " Scroll", PriceType.Coins, 100, Reward.Scrolls(slot, 1));
            case 6: return Sale(gear.title, PriceType.Gems, 5, Reward.Gear(gear.id, Rarity.Great));
            default: return Sale(gear.title, PriceType.Coins, 400, Reward.Gear(gear.id, Rarity.Common));
        }
    }

    // ---------- Helpers that make offers (so each line above stays short) ----------

    // "params" lets the caller list any number of rewards, separated by commas
    static ShopOffer Pack(string id, int cents, int limit, ShopPeriod period, int value, params Reward[] rewards)
    {
        return new ShopOffer
        {
            id = id, rewards = rewards, limit = limit, period = period, valuePercent = value,
            priceType = cents == 0 ? PriceType.Free : PriceType.Money, price = cents,
            title = id.StartsWith("chapter_") ? "Ch. " + id.Substring(8) + " Gift Pack" : ""
        };
    }

    static ShopOffer GemPack(string id, string title, int gems, int cents)
    {
        return new ShopOffer
        {
            id = id, title = title, rewards = new[] { Reward.Gems(gems) },
            priceType = PriceType.Money, price = cents, period = ShopPeriod.Unlimited, doubleFirstTime = true
        };
    }

    static ShopOffer CoinPack(string id, string title, int coins, int gems)
    {
        return new ShopOffer
        {
            id = id, title = title, rewards = new[] { Reward.Coins(coins) },
            priceType = PriceType.Gems, price = gems, period = ShopPeriod.Unlimited
        };
    }

    // limit 0 = as many as you like
    static ShopOffer CrystalPack(string id, int crystals, int cents, int limit)
    {
        return new ShopOffer
        {
            id = id, rewards = new[] { Reward.Crystals(crystals) }, priceType = PriceType.Money, price = cents,
            limit = limit, period = limit > 0 ? ShopPeriod.Monthly : ShopPeriod.Unlimited
        };
    }

    static ShopOffer Sale(string title, PriceType priceType, int price, Reward reward)
    {
        return new ShopOffer { title = title, priceType = priceType, price = price, rewards = new[] { reward } };
    }

    // ---------- Text helpers ----------

    // GearSlot.Boots -> "Boots"
    public static string SlotTitle(GearSlot slot)
    {
        return slot.ToString();
    }

    // Rich text: "<color=#FFAA00>text</color>" shows the text in that color
    public static string Colored(string text, Color color)
    {
        return "<color=#" + ColorUtility.ToHtmlStringRGB(color) + ">" + text + "</color>";
    }
}
