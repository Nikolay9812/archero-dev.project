using System;

// The player's coins. They are stored in the save file, so they stay after dying or closing the game.
public static class Wallet
{
    // CoinCounter listens to this
    public static event Action Changed;

    public static int Coins => SaveSystem.Data.coins;

    // Coins picked up since the chapter started (GameOverScreen shows this at the end)
    public static int EarnedThisRun { get; private set; }

    // ChapterManager calls this when a chapter starts
    public static void StartRun()
    {
        EarnedThisRun = 0;
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
}
