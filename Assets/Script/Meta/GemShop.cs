// The items sold for gems in the shop. ExtraArrow and Revive are bought once and then work in every run.
// The Gear Chest can be bought again and again.
public enum GemItem
{
    ExtraArrow,
    Revive,
    GearChest
}

public static class GemShop
{
    public static readonly GemItem[] All = { GemItem.ExtraArrow, GemItem.Revive, GemItem.GearChest };

    // The item that came out of the last chest (ShopScreen shows it)
    public static OwnedGear LastChestItem { get; private set; }

    public static bool Owns(GemItem item)
    {
        switch (item)
        {
            case GemItem.ExtraArrow: return SaveSystem.Data.ownsExtraArrow;
            case GemItem.Revive: return SaveSystem.Data.ownsRevive;
            default: return false; // a chest is never "owned", you can always buy another
        }
    }

    public static int GetPrice(GemItem item)
    {
        switch (item)
        {
            case GemItem.ExtraArrow: return 30;
            case GemItem.Revive: return 20;
            default: return 15;
        }
    }

    public static string GetTitle(GemItem item)
    {
        switch (item)
        {
            case GemItem.ExtraArrow: return "EXTRA ARROW";
            case GemItem.Revive: return "REVIVE";
            default: return "GEAR CHEST";
        }
    }

    public static string GetDescription(GemItem item)
    {
        switch (item)
        {
            case GemItem.ExtraArrow: return "One more arrow in every shot";
            case GemItem.Revive: return "Come back once per run with half your HP";
            default: return "A random piece of gear, Great or better";
        }
    }

    // Returns true if the item was bought, false if it is already owned or the player has too few gems
    public static bool TryBuy(GemItem item)
    {
        if (Owns(item)) return false;
        if (!Wallet.SpendGems(GetPrice(item))) return false;

        switch (item)
        {
            case GemItem.ExtraArrow: SaveSystem.Data.ownsExtraArrow = true; break;
            case GemItem.Revive: SaveSystem.Data.ownsRevive = true; break;
            case GemItem.GearChest: LastChestItem = Inventory.AddRandomGear(Rarity.Great); break;
        }

        SaveSystem.Save();
        return true;
    }
}
