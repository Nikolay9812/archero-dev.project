using System.Collections.Generic;
using UnityEngine;

// A chapter is just a list of room prefabs, played from top to bottom.
// The same room prefab can be used more than once.
// Create one with right-click in the Project window > Create > Archero > Chapter.
[CreateAssetMenu(fileName = "NewChapter", menuName = "Archero/Chapter")]
public class ChapterData : ScriptableObject
{
    public string chapterName = "Chapter 1";
    public List<Room> rooms = new List<Room>();
}
