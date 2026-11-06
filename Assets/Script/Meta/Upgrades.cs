using System.Collections.Generic;
using UnityEngine;

// The permanent talents on the Collectible page. The player doesn't choose one: a draw upgrades a random talent.
// Only the level of each talent is saved. The bonuses and the price are worked out from those levels.
public enum UpgradeType
{
    Attack,
    MaxHealth,
    AttackSpeed,
    MoveSpeed,
    RoomHeal,
    CoinBonus
}

public static class Upgrades
{
    public const int MaxLevel = 5; // the number of stars on a card

    // Every 5th profile level gives 5 more draws
    public const int LevelsPerBatch = 5;
    public const int DrawsPerBatch = 5;

    // Change these numbers to balance the game. "const" = a value that never changes while the game runs.
    private const int DamagePerLevel = 4;
    private const int HealthPerLevel = 60;
    private const float FireRatePerLevel = 0.2f;
    private const float MoveSpeedPerLevel = 0.2f;
    private const int RoomHealPerLevel = 10;
    private const int CoinPercentPerLevel = 10;
    private const int StartPrice = 100;
    private const int PriceRisePerDraw = 50;

    // Every talent. To add one: add it to the enum and to this list, then to each switch below.
    public static readonly UpgradeType[] All =
    {
        UpgradeType.Attack, UpgradeType.MaxHealth, UpgradeType.AttackSpeed,
        UpgradeType.MoveSpeed, UpgradeType.RoomHeal, UpgradeType.CoinBonus
    };

    // The player scripts add these to their stats when the game starts
    public static int BonusDamage => SaveSystem.Data.attackUpgrade * DamagePerLevel;
    public static int BonusHealth => SaveSystem.Data.healthUpgrade * HealthPerLevel;
    public static float BonusFireRate => SaveSystem.Data.speedUpgrade * FireRatePerLevel;
    public static float BonusMoveSpeed => SaveSystem.Data.moveUpgrade * MoveSpeedPerLevel;

    // HP healed when the player enters a new room (ChapterManager uses this)
    public static int RoomHeal => SaveSystem.Data.roomHealUpgrade * RoomHealPerLevel;

    // Extra coins from enemies, in percent (Enemy uses this)
    public static int CoinBonusPercent => SaveSystem.Data.coinUpgrade * CoinPercentPerLevel;

    // Dividing two ints drops the decimals: level 7 / 5 = 1, so level 7 has unlocked one batch
    public static int DrawsUnlocked => PlayerProfile.Level / LevelsPerBatch * DrawsPerBatch;

    // Each draw adds exactly one level, so the levels together are the number of draws used
    public static int DrawsUsed
    {
        get
        {
            int total = 0;
            foreach (UpgradeType type in All) total += GetLevel(type);
            return total;
        }
    }

    public static int DrawsLeft => Mathf.Max(DrawsUnlocked - DrawsUsed, 0);
    public static int NextUnlockLevel => (PlayerProfile.Level / LevelsPerBatch + 1) * LevelsPerBatch;
    public static bool AllMaxed => DrawsUsed >= All.Length * MaxLevel;

    // 1st draw: 100 coins, 2nd: 150, 3rd: 200...
    public static int Price => StartPrice + DrawsUsed * PriceRisePerDraw;

    public static int GetLevel(UpgradeType type)
    {
        switch (type)
        {
            case UpgradeType.Attack: return SaveSystem.Data.attackUpgrade;
            case UpgradeType.MaxHealth: return SaveSystem.Data.healthUpgrade;
            case UpgradeType.AttackSpeed: return SaveSystem.Data.speedUpgrade;
            case UpgradeType.MoveSpeed: return SaveSystem.Data.moveUpgrade;
            case UpgradeType.RoomHeal: return SaveSystem.Data.roomHealUpgrade;
            default: return SaveSystem.Data.coinUpgrade;
        }
    }

    public static string GetTitle(UpgradeType type)
    {
        switch (type)
        {
            case UpgradeType.Attack: return "ATTACK";
            case UpgradeType.MaxHealth: return "MAX HP";
            case UpgradeType.AttackSpeed: return "ATTACK SPEED";
            case UpgradeType.MoveSpeed: return "MOVE SPEED";
            case UpgradeType.RoomHeal: return "ROOM HEAL";
            default: return "COIN BONUS";
        }
    }

    // What the talent gives right now, for example "+8 damage"
    public static string GetBonusText(UpgradeType type)
    {
        switch (type)
        {
            case UpgradeType.Attack: return "+" + BonusDamage + " damage";
            case UpgradeType.MaxHealth: return "+" + BonusHealth + " HP";
            case UpgradeType.AttackSpeed: return "+" + BonusFireRate.ToString("0.0") + " arrows/sec"; // "0.0" = always one decimal
            case UpgradeType.MoveSpeed: return "+" + BonusMoveSpeed.ToString("0.0") + " speed";
            case UpgradeType.RoomHeal: return "+" + RoomHeal + " HP per room";
            default: return "+" + CoinBonusPercent + "% coins";
        }
    }

    // Pays and upgrades a random talent that isn't maxed yet. Returns false if that isn't possible right now.
    // "out" gives the caller a second result: which talent was upgraded.
    public static bool TryDraw(out UpgradeType won)
    {
        won = UpgradeType.Attack; // an "out" value must always be set, even when the draw fails

        if (DrawsLeft <= 0 || AllMaxed) return false;
        if (!Wallet.Spend(Price)) return false;

        List<UpgradeType> possible = new List<UpgradeType>();
        foreach (UpgradeType type in All)
        {
            if (GetLevel(type) < MaxLevel) possible.Add(type);
        }

        won = possible[Random.Range(0, possible.Count)]; // for ints the second number is never picked itself

        switch (won)
        {
            case UpgradeType.Attack: SaveSystem.Data.attackUpgrade++; break;
            case UpgradeType.MaxHealth: SaveSystem.Data.healthUpgrade++; break;
            case UpgradeType.AttackSpeed: SaveSystem.Data.speedUpgrade++; break;
            case UpgradeType.MoveSpeed: SaveSystem.Data.moveUpgrade++; break;
            case UpgradeType.RoomHeal: SaveSystem.Data.roomHealUpgrade++; break;
            case UpgradeType.CoinBonus: SaveSystem.Data.coinUpgrade++; break;
        }

        SaveSystem.Save();
        return true;
    }
}
