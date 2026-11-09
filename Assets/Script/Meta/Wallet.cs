using System;

// The player's coins and gems. They are stored in the save file, so they stay after dying or closing the game.
public static class Wallet
{
    // CoinCounter listens to this
    public static event Action Changed;

    public static int Coins => SaveSystem.Data.coins;
    public static int Gems => SaveSystem.Data.gems;

    // Picked up since the chapter started (GameOverScreen shows these at the end)
    public static int EarnedThisRun { get; private set; }
    public static int GemsEarnedThisRun { get; private set; }

    // ChapterManager calls this when a chapter starts
    public static void StartRun()
    {
        EarnedThisRun = 0;
        GemsEarnedThisRun = 0;
        Changed?.Invoke(); // so CoinCounter goes back to 0
    }

    public static void Add(int amount)
    {
        SaveSystem.Data.coins += amount;
        EarnedThisRun += amount;

        // Saving right away means nothing is lost if the phone closes the game.
        // The file is tiny, so writing it for every coin is no problem.
        SaveSystem.Save();

        Changed?.Invoke();
    }

    // Returns false, and takes nothing, if the player doesn't have enough coins
    public static bool Spend(int amount)
    {
        if (Coins < amount) return false;

        SaveSystem.Data.coins -= amount;
        SaveSystem.Save();

        Changed?.Invoke();
        return true;
    }

    public static void AddGems(int amount)
    {
        SaveSystem.Data.gems += amount;
        GemsEarnedThisRun += amount;

        SaveSystem.Save();

        Changed?.Invoke();
    }

    // Returns false, and takes nothing, if the player doesn't have enough gems
    public static bool SpendGems(int amount)
    {
        if (Gems < amount) return false;

        SaveSystem.Data.gems -= amount;
        SaveSystem.Save();

        Changed?.Invoke();
        return true;
    }

    // Crystals are only sold in the shop, for real money. They can pay for any paid offer instead of money.
    public static int Crystals => SaveSystem.Data.crystals;

    public static void AddCrystals(int amount)
    {
        SaveSystem.Data.crystals += amount;
        SaveSystem.Save();
        Changed?.Invoke();
    }

    public static bool SpendCrystals(int amount)
    {
        if (Crystals < amount) return false;

        SaveSystem.Data.crystals -= amount;
        SaveSystem.Save();

        Changed?.Invoke();
        return true;
    }
}
