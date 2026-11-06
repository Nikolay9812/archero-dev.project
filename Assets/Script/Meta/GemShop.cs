// The items sold for gems in the shop. Each one is bought once and then works in every run.
public enum GemItem
{
    ExtraArrow,
    Revive
}

public static class GemShop
{
    public static readonly GemItem[] All = { GemItem.ExtraArrow, GemItem.Revive };

    public static bool Owns(GemItem item)
    {
        switch (item)
        {
            case GemItem.ExtraArrow: return SaveSystem.Data.ownsExtraArrow;
            default: return SaveSystem.Data.ownsRevive;
        }
    }

    public static int GetPrice(GemItem item)
    {
        switch (item)
        {
            case GemItem.ExtraArrow: return 30;
            default: return 20;
        }
    }

    public static string GetTitle(GemItem item)
    {
        switch (item)
        {
            case GemItem.ExtraArrow: return "EXTRA ARROW";
            default: return "REVIVE";
        }
    }

    public static string GetDescription(GemItem item)
    {
        switch (item)
        {
            case GemItem.ExtraArrow: return "One more arrow in every shot";
            default: return "Come back once per run with half your HP";
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
        }

        SaveSystem.Save();
        return true;
    }
}
