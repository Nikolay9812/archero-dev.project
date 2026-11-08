using System;
using UnityEngine;

// The player's experience and level. XP orbs call AddXp; when the bar is full she levels up.
// Other scripts (the XP bar now, the skill choice screen later) listen to the events below,
// so this script doesn't need to know anything about UI.
public class PlayerXp : MonoBehaviour
{
    public int xpForLevel2 = 20;   // XP needed to go from level 1 to 2
    public int xpIncrease = 10;    // every next level needs this much more XP than the one before

    // "Something about XP changed, redraw yourself". XpBar listens to this.
    public static event Action XpChanged;

    // "She just reached a new level" (the number is the new level). Later: open the skill choice.
    public static event Action<int> LeveledUp;

    // The one player in the scene, so UI can read the numbers without a reference
    public static PlayerXp Instance { get; private set; }

    public int Level { get; private set; } = 1;
    public int CurrentXp { get; private set; }  // XP collected towards the next level
    public int XpToNextLevel => xpForLevel2 + (Level - 1) * xpIncrease; // level 1: 20, level 2: 30, level 3: 40...

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void AddXp(int amount)
    {
        CurrentXp += amount;

        // "while", not "if": one big amount of XP could be enough for two levels at once
        while (CurrentXp >= XpToNextLevel)
        {
            CurrentXp -= XpToNextLevel; // leftover XP carries over to the next level
            Level++;
            SoundManager.Play(Sound.LevelUp);
            LeveledUp?.Invoke(Level);
        }

        XpChanged?.Invoke();
    }
}
