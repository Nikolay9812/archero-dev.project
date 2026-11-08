using System;
using System.Collections.Generic;
using UnityEngine;

// The player's gear and scrolls: what she owns, what she wears, leveling up and merging.
// Everything is kept in the save file (SaveSystem.Data), like the coins in Wallet.
public static class Inventory
{
    // The gear page listens to this, so it can redraw after a merge or a level up
    public static event Action Changed;

    // Found since the chapter started (GameOverScreen shows these at the end)
    public static int GearFoundThisRun { get; private set; }
    public static int ScrollsFoundThisRun { get; private set; }

    public static List<OwnedGear> All => SaveSystem.Data.gear;

    // ChapterManager calls this when a chapter starts
    public static void StartRun()
    {
        GearFoundThisRun = 0;
        ScrollsFoundThisRun = 0;
    }

    // ---------- Getting new things ----------

    public static OwnedGear AddGear(GearType type, Rarity rarity)
    {
        OwnedGear item = new OwnedGear();
        item.id = SaveSystem.Data.nextGearId;
        item.type = type.id;
        item.rarity = rarity;
        SaveSystem.Data.nextGearId++;

        All.Add(item);
        GearFoundThisRun++;
        SaveAndTell();
        return item;
    }

    // A random item. 'minimum' is the worst rarity it can be (the boss and the shop chest give at least Great).
    public static OwnedGear AddRandomGear(Rarity minimum)
    {
        return AddGear(GearCatalog.RandomType(), GearCatalog.RandomRarity(minimum));
    }

    public static void AddScrolls(GearSlot slot, int amount)
    {
        Scrolls[(int)slot] += amount;
        ScrollsFoundThisRun += amount;
        SaveAndTell();
    }

    public static int GetScrolls(GearSlot slot)
    {
        return Scrolls[(int)slot];
    }

    // ---------- Wearing things ----------

    public static GearType TypeOf(OwnedGear item)
    {
        return GearCatalog.Find(item.type);
    }

    // The item worn in a slot, or null if the slot is empty
    public static OwnedGear GetEquipped(GearSlot slot)
    {
        int id = Equipped[(int)slot];
        if (id == 0) return null;

        foreach (OwnedGear item in All)
        {
            if (item.id == id) return item;
        }
        return null; // the item is gone (should not happen)
    }

    public static bool IsEquipped(OwnedGear item)
    {
        return Equipped[(int)TypeOf(item).slot] == item.id;
    }

    // Wears the item. Whatever was in that slot before goes back to the inventory.
    public static void Equip(OwnedGear item)
    {
        Equipped[(int)TypeOf(item).slot] = item.id;
        SaveAndTell();
    }

    public static void Unequip(GearSlot slot)
    {
        Equipped[(int)slot] = 0;
        SaveAndTell();
    }

    // ---------- Stats ----------

    // The bonus of one item, with its rarity and level
    public static float ValueOf(OwnedGear item)
    {
        return GearCatalog.Value(TypeOf(item), item.rarity, item.level);
    }

    // All worn items that give this stat, added up. The player scripts read this when a run starts.
    public static float Bonus(GearStat stat)
    {
        float total = 0f;
        for (int slot = 0; slot < GearCatalog.SlotCount; slot++)
        {
            OwnedGear item = GetEquipped((GearSlot)slot);
            if (item == null || TypeOf(item) == null) continue; // empty slot (or an item removed from the game)

            if (TypeOf(item).stat == stat) total += ValueOf(item);
        }
        return total;
    }

    // The same, rounded to a whole number (for damage, HP and coin percent)
    public static int BonusInt(GearStat stat)
    {
        return Mathf.RoundToInt(Bonus(stat));
    }

    // ---------- Leveling up ----------

    public static bool IsMaxLevel(OwnedGear item)
    {
        return item.level >= GearCatalog.MaxLevel(item.rarity);
    }

    // Returns false, and takes nothing, if the item is maxed or the player lacks scrolls or coins
    public static bool TryLevelUp(OwnedGear item)
    {
        if (IsMaxLevel(item)) return false;

        GearSlot slot = TypeOf(item).slot;
        int scrollCost = GearCatalog.ScrollCost(item.level);
        if (GetScrolls(slot) < scrollCost) return false;
        if (!Wallet.Spend(GearCatalog.CoinCost(item.level))) return false; // this already saves

        Scrolls[(int)slot] -= scrollCost;
        item.level++;
        SaveAndTell();
        return true;
    }

    // ---------- Merging ----------

    public const int MergeCount = 3;

    // Can 'item' go into a merge together with 'first'? Same item, same rarity, and not already Legendary.
    public static bool CanMergeWith(OwnedGear first, OwnedGear item)
    {
        return item.type == first.type && item.rarity == first.rarity && item.rarity < Rarity.Legendary;
    }

    // Turns 3 matching items into 1 of the next rarity. It keeps the highest level of the three,
    // and if one of them was worn, the new item is worn instead. Returns null if the items don't match.
    public static OwnedGear Merge(List<OwnedGear> items)
    {
        if (items.Count != MergeCount) return null;
        foreach (OwnedGear item in items)
        {
            if (!CanMergeWith(items[0], item)) return null;
        }

        int bestLevel = 1;
        bool wasWorn = false;
        foreach (OwnedGear item in items)
        {
            bestLevel = Mathf.Max(bestLevel, item.level);
            if (IsEquipped(item)) wasWorn = true;
            All.Remove(item);
        }

        GearType type = TypeOf(items[0]);
        OwnedGear result = AddGear(type, items[0].rarity + 1); // enum + 1 = the next rarity
        result.level = bestLevel;

        if (wasWorn) Equip(result); // the slot pointed at a removed item: point it at the new one

        SaveAndTell();
        return result;
    }

    // ---------- Helpers ----------

    // The arrays in the save, made the right size first (an older save file may have shorter ones)
    private static int[] Equipped => FitToSlots(ref SaveSystem.Data.equipped);
    private static int[] Scrolls => FitToSlots(ref SaveSystem.Data.scrolls);

    // "ref" lets this method replace the array inside SaveData, not just a copy of the reference
    private static int[] FitToSlots(ref int[] array)
    {
        if (array == null) array = new int[GearCatalog.SlotCount];
        if (array.Length < GearCatalog.SlotCount) Array.Resize(ref array, GearCatalog.SlotCount);
        return array;
    }

    private static void SaveAndTell()
    {
        SaveSystem.Save();
        Changed?.Invoke();
    }
}
