using System;
using System.Collections.Generic;
using UnityEngine;

// The rules of the shop: buying offers, purchase limits that come back every day/week/month,
// paying with real money (mocked for now), and opening gear chests.
// What is for sale is in ShopCatalog. What is bought is kept in the save file.
public static class Shop
{
    // TRUE = every real money price is free, so everything can be tested.
    // Before the Play Store release this becomes false and PayMoney talks to Google Play instead.
    public static readonly bool PaymentsAreMocked = true;

    // The shop page listens to this and redraws itself
    public static event Action Changed;

    // ---------- Periods ----------

    // 1 January 2024 was a Monday, so counting weeks from it makes every week start on a Monday
    private static readonly DateTime FirstDay = new DateTime(2024, 1, 1);

    // A number for "now" in this period: today's day number, this week's number, or this month's.
    // A purchase only counts while this number is still the same.
    public static int PeriodKey(ShopPeriod period)
    {
        int day = (int)(DateTime.Now.Date - FirstDay).TotalDays;

        switch (period)
        {
            case ShopPeriod.Daily: return day;
            case ShopPeriod.Weekly: return day / 7;
            case ShopPeriod.Monthly: return DateTime.Now.Year * 12 + DateTime.Now.Month;
            default: return 0; // OneTime and Unlimited never start over
        }
    }

    // How long until the period starts over
    public static TimeSpan TimeUntilRefresh(ShopPeriod period)
    {
        DateTime now = DateTime.Now;
        DateTime next;

        switch (period)
        {
            case ShopPeriod.Daily: next = now.Date.AddDays(1); break;
            case ShopPeriod.Weekly: next = FirstDay.AddDays((PeriodKey(ShopPeriod.Weekly) + 1) * 7); break;
            case ShopPeriod.Monthly: next = new DateTime(now.Year, now.Month, 1).AddMonths(1); break;
            default: return TimeSpan.Zero;
        }
        return next - now;
    }

    // "05:19:26" when it's less than a day, otherwise "06d 05h"
    public static string FormatTime(TimeSpan time)
    {
        if (time.TotalDays >= 1) return time.Days + "d " + time.Hours.ToString("00") + "h";
        return time.Hours.ToString("00") + ":" + time.Minutes.ToString("00") + ":" + time.Seconds.ToString("00");
    }

    // ---------- Buying offers ----------

    // How often it was bought in the current period
    public static int BoughtCount(ShopOffer offer)
    {
        ShopPurchase record = FindPurchase(offer, false);
        return record != null ? record.count : 0;
    }

    public static bool HasLimit(ShopOffer offer) => offer.period != ShopPeriod.Unlimited;

    public static int PurchasesLeft(ShopOffer offer) => offer.limit - BoughtCount(offer);

    public static bool IsSoldOut(ShopOffer offer) => HasLimit(offer) && PurchasesLeft(offer) <= 0;

    // Gem packs give the gems twice the first time
    public static bool HasFirstTimeBonus(ShopOffer offer) => offer.doubleFirstTime && BoughtCount(offer) == 0;

    // Pays, gives the rewards and counts the purchase. 'lines' gets one line per reward, for the "You got" window.
    // Returns false and gives nothing if it is sold out or too expensive; 'problem' then says why.
    public static bool TryBuy(ShopOffer offer, List<string> lines, out string problem)
    {
        problem = null;
        if (IsSoldOut(offer))
        {
            problem = "Sold out";
            return false;
        }

        bool bonus = HasFirstTimeBonus(offer); // check BEFORE counting this purchase
        if (!Pay(offer, out problem)) return false;

        foreach (Reward reward in offer.rewards)
        {
            lines.Add(reward.Give());
            if (bonus) lines.Add(reward.Give() + " (first time bonus)");
        }

        FindPurchase(offer, true).count++;
        SaveAndTell();
        return true;
    }

    static bool Pay(ShopOffer offer, out string problem)
    {
        problem = null;
        switch (offer.priceType)
        {
            case PriceType.Free:
                return true;

            case PriceType.Gems:
                if (Wallet.SpendGems(offer.price)) return true;
                problem = "Not enough gems";
                return false;

            case PriceType.Coins:
                if (Wallet.Spend(offer.price)) return true;
                problem = "Not enough coins";
                return false;

            default:
                return PayMoney(offer, out problem);
        }
    }

    // Crystals pay first, if there are enough. Crystal packs themselves can't be paid with crystals.
    static bool PayMoney(ShopOffer offer, out string problem)
    {
        problem = null;
        bool sellsCrystals = offer.rewards[0].type == ShopRewardType.Crystals;
        if (!sellsCrystals && Wallet.SpendCrystals(CrystalPrice(offer.price))) return true;

        if (PaymentsAreMocked)
        {
            Debug.Log("MOCK payment of " + FormatEuros(offer.price) + " for " + offer.id + ": free while testing.");
            return true;
        }

        problem = "Payments are not available yet";
        return false;
    }

    // A price in crystals for every euro price: 0,89 € = 99 crystals ... 94,99 € = 9999 crystals
    private static readonly int[] EuroPrices = { 89, 479, 949, 1699, 1899, 2899, 4999, 9499 };
    private static readonly int[] CrystalPrices = { 99, 499, 999, 1799, 1999, 2999, 4999, 9999 };

    public static int CrystalPrice(int cents)
    {
        int index = Array.IndexOf(EuroPrices, cents); // -1 = not in the list
        return index >= 0 ? CrystalPrices[index] : cents;
    }

    // 479 -> "4,79 €"
    public static string FormatEuros(int cents)
    {
        return cents / 100 + "," + (cents % 100).ToString("00") + " €";
    }

    // The save record of an offer for the current period. 'create' = make one if there is none yet.
    static ShopPurchase FindPurchase(ShopOffer offer, bool create)
    {
        int period = PeriodKey(offer.period);
        List<ShopPurchase> all = SaveSystem.Data.shopPurchases;

        foreach (ShopPurchase record in all)
        {
            if (record.id == offer.id && record.period == period) return record;
        }
        if (!create) return null;

        // Old records of this offer (yesterday's...) are no use anymore: remove them, so the save stays small.
        // RemoveAll takes a lambda that says which ones to remove.
        all.RemoveAll(record => record.id == offer.id);

        ShopPurchase newRecord = new ShopPurchase { id = offer.id, period = period };
        all.Add(newRecord);
        return newRecord;
    }

    // ---------- Gear chests ----------

    // The saved state of a chest, made the first time it is needed
    public static ChestState GetState(ChestType chest)
    {
        foreach (ChestState state in SaveSystem.Data.chests)
        {
            if (state.id == chest.id) return state;
        }

        ChestState newState = new ChestState { id = chest.id };
        SaveSystem.Data.chests.Add(newState);
        return newState;
    }

    public static int Keys(ChestType chest) => GetState(chest).keys;

    public static void AddKeys(string chestId, int amount)
    {
        GetState(ShopCatalog.FindChest(chestId)).keys += amount;
        SaveAndTell();
    }

    // How long until the next free open (zero or less = free now)
    public static TimeSpan FreeOpenIn(ChestType chest)
    {
        if (chest.freeHours <= 0) return TimeSpan.MaxValue;

        DateTime last = new DateTime(GetState(chest).lastFreeOpen, DateTimeKind.Utc);
        return last.AddHours(chest.freeHours) - DateTime.UtcNow;
    }

    public static bool IsFree(ChestType chest) => chest.freeHours > 0 && FreeOpenIn(chest) <= TimeSpan.Zero;

    // How many more opens until Epic / Legendary is guaranteed
    public static int OpensUntilEpic(ChestType chest) => chest.epicWithin - GetState(chest).opensWithoutEpic;
    public static int OpensUntilLegendary(ChestType chest) => chest.legendaryWithin - GetState(chest).opensWithoutLegendary;

    // Opens a chest 'count' times. Pays in this order: free open (only for 1), keys, gems.
    // 'focus' = the Hero Chest's focus tab (-1 = none). The new items are added to 'items'.
    public static bool TryOpenChest(ChestType chest, int count, int focus, List<OwnedGear> items, out string problem)
    {
        problem = null;
        ChestState state = GetState(chest);

        if (count == 1 && IsFree(chest)) state.lastFreeOpen = DateTime.UtcNow.Ticks;
        else if (state.keys >= count) state.keys -= count;
        else if (!Wallet.SpendGems(chest.price * count))
        {
            problem = "Not enough gems";
            return false;
        }

        for (int i = 0; i < count; i++)
        {
            Rarity rarity = RollRarity(chest, state);
            items.Add(Inventory.AddGear(RollType(focus), rarity));
        }

        SaveAndTell();
        return true;
    }

    // The chest's chances, but after too many unlucky opens the rarity is pushed up
    static Rarity RollRarity(ChestType chest, ChestState state)
    {
        int roll = UnityEngine.Random.Range(0, 100); // "UnityEngine." because System has a Random too
        Rarity rarity = Rarity.Common;
        for (int i = 0; i < chest.chance.Length; i++)
        {
            if (roll < chest.chance[i])
            {
                rarity = (Rarity)i;
                break;
            }
            roll -= chest.chance[i];
        }

        if (chest.epicWithin > 0 && state.opensWithoutEpic + 1 >= chest.epicWithin && rarity < Rarity.Epic) rarity = Rarity.Epic;
        if (chest.legendaryWithin > 0 && state.opensWithoutLegendary + 1 >= chest.legendaryWithin) rarity = Rarity.Legendary;

        state.opensWithoutEpic = rarity >= Rarity.Epic ? 0 : state.opensWithoutEpic + 1;
        state.opensWithoutLegendary = rarity >= Rarity.Legendary ? 0 : state.opensWithoutLegendary + 1;
        return rarity;
    }

    // With a focus tab, half of the items come from its slots. Otherwise any item.
    static GearType RollType(int focus)
    {
        if (focus < 0 || UnityEngine.Random.value >= 0.5f) return GearCatalog.RandomType();

        List<GearType> possible = new List<GearType>();
        foreach (GearType type in GearCatalog.All)
        {
            if (Array.IndexOf(ShopCatalog.FocusSlots[focus], type.slot) >= 0) possible.Add(type);
        }
        return possible[UnityEngine.Random.Range(0, possible.Count)];
    }

    // ---------- Testing ----------

    // Every limit and free chest comes back (menu "Archero > Reset Shop")
    public static void ResetForTesting()
    {
        SaveSystem.Data.shopPurchases.Clear();
        foreach (ChestState state in SaveSystem.Data.chests) state.lastFreeOpen = 0;
        SaveAndTell();
    }

    static void SaveAndTell()
    {
        SaveSystem.Save();
        Changed?.Invoke();
    }
}
