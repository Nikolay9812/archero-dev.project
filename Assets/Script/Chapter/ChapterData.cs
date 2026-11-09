using System.Collections.Generic;
using UnityEngine;

// A chapter is just a list of room prefabs, played from top to bottom.
// The same room prefab can be used more than once.
// Create one with right-click in the Project window > Create > Archero > Chapter.
[CreateAssetMenu(fileName = "NewChapter", menuName = "Archero/Chapter")]
public class ChapterData : ScriptableObject
{
    public string chapterName = "Chapter 1";
    // Difficulty: every enemy in this chapter gets this much HP and damage (1 = as set in the prefab, 1.5 = 50% more).
    // Later chapters only need bigger numbers here, no new enemy prefabs.
    // Changes in Play mode stay (it's an asset), so you can tune it while playing.
    public float enemyHealthMultiplier = 1f;
    public float enemyDamageMultiplier = 1f;
    public float coinMultiplier = 1f;   // coins dropped by enemies

    public int profileXpReward = 150; // permanent XP for finishing this chapter
    public int gemReward = 5;         // gems for finishing this chapter
    public int gearReward = 1;        // random pieces of gear for finishing this chapter
    public int wildShardReward = 5;   // artifact shards that count for any artifact
    public bool firstRoomIsStartRoom = true; // true = the room counter doesn't count the first room
    public List<Room> rooms = new List<Room>();
}
