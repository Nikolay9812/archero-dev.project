// The player's profile (account) level. Don't mix it up with PlayerXp:
// PlayerXp is for the current run and starts over every time. This one is stored in the save file and is kept forever.
public static class PlayerProfile
{
    public static int Level => SaveSystem.Data.profileLevel;
    public static int Xp => SaveSystem.Data.profileXp;
    public static int XpToNextLevel => 100 + (Level - 1) * 50; // level 1: 100, level 2: 150, level 3: 200...

    // Earned since the chapter started (GameOverScreen shows this at the end)
    public static int XpEarnedThisRun { get; private set; }

    // ChapterManager calls this when a chapter starts
    public static void StartRun()
    {
        XpEarnedThisRun = 0;
    }

    public static void AddXp(int amount)
    {
        SaveData data = SaveSystem.Data;
        data.profileXp += amount;
        XpEarnedThisRun += amount;

        // "while", not "if": one big amount of XP could be enough for two levels at once
        while (data.profileXp >= XpToNextLevel)
        {
            data.profileXp -= XpToNextLevel; // leftover XP carries over to the next level
            data.profileLevel++;
        }

        SaveSystem.Save();
    }
}
