using UnityEngine;

// Every sound in the game, the names the code uses for them.
// Append new ones at the END only: Unity saves enum values as numbers.
public enum Sound
{
    Shoot,
    EnemyHit,
    EnemyDeath,
    PlayerHurt,
    Heal,
    Coin,
    Gem,
    Xp,
    LevelUp,
    SkillChosen,
    Click,
    DoorOpen,
    RoomClear,
    WheelSpin,
    WheelPrize,
    BossAppear,
    GameOver,
    ChapterClear
}

// The list of all sound files. Make ONE of these (right click > Create > Archero > Sound Library),
// name it exactly "SoundLibrary" and put it in a folder called "Resources",
// so SoundManager can find it by itself with Resources.Load.
//
// Every sound is a list: give it 2 or 3 similar files and a random one is picked each time,
// which sounds much less repetitive than the same file over and over. Empty = silent.
[CreateAssetMenu(fileName = "SoundLibrary", menuName = "Archero/Sound Library")]
public class SoundLibrary : ScriptableObject
{
    [Header("Volume (0 = off, 1 = full)")]
    [Range(0f, 1f)] public float soundVolume = 0.8f;
    [Range(0f, 1f)] public float musicVolume = 0.4f;

    [Header("Music (loops)")]
    public AudioClip menuMusic;
    public AudioClip gameMusic;
    public AudioClip bossMusic; // empty = keep the game music during the boss

    [Header("Fighting")]
    public AudioClip[] shoot;
    public AudioClip[] enemyHit;
    public AudioClip[] enemyDeath;
    public AudioClip[] playerHurt;
    public AudioClip[] heal;

    [Header("Pickups")]
    public AudioClip[] coin;
    public AudioClip[] gem;
    public AudioClip[] xp;

    [Header("Progress")]
    public AudioClip[] levelUp;
    public AudioClip[] skillChosen;
    public AudioClip[] doorOpen;
    public AudioClip[] roomClear;
    public AudioClip[] wheelSpin;
    public AudioClip[] wheelPrize;
    public AudioClip[] bossAppear;
    public AudioClip[] gameOver;
    public AudioClip[] chapterClear;

    [Header("Menus")]
    public AudioClip[] click;

    // Finds the list of files for a sound
    public AudioClip[] Get(Sound sound)
    {
        switch (sound)
        {
            case Sound.Shoot: return shoot;
            case Sound.EnemyHit: return enemyHit;
            case Sound.EnemyDeath: return enemyDeath;
            case Sound.PlayerHurt: return playerHurt;
            case Sound.Heal: return heal;
            case Sound.Coin: return coin;
            case Sound.Gem: return gem;
            case Sound.Xp: return xp;
            case Sound.LevelUp: return levelUp;
            case Sound.SkillChosen: return skillChosen;
            case Sound.Click: return click;
            case Sound.DoorOpen: return doorOpen;
            case Sound.RoomClear: return roomClear;
            case Sound.WheelSpin: return wheelSpin;
            case Sound.WheelPrize: return wheelPrize;
            case Sound.BossAppear: return bossAppear;
            case Sound.GameOver: return gameOver;
            case Sound.ChapterClear: return chapterClear;
            default: return null;
        }
    }
}
