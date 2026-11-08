using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object; // "Object" could mean System.Object too; we want Unity's

// An editor tool, not part of the game: click Archero > Build Chapter 1 in Unity's top menu.
// It makes the new room prefabs (each one starts as a copy of Room1) and fills in the Chapter1 room list.
// Scripts in a folder called "Editor" are left out of the phone build automatically.
//
// A room that already exists is skipped, so you can safely edit the rooms by hand afterwards.
// To rebuild one room from this code, delete its prefab and click the menu item again.
//
// Positions below are (x, z) in the room: x goes from -6 to 6 (left to right),
// z goes from -8 to 8 (bottom to top). The player starts at (0, -6), the exit door is at (0, 8).
public static class Chapter1Builder
{
    const string RoomFolder = "Assets/Prefabs/Rooms/";
    const string EnemyFolder = "Assets/Prefabs/Enemies/";
    const string MaterialFolder = "Assets/Material/";
    const string ChapterPath = "Assets/Chapters/Chapter1.asset";

    // The enemy prefab file names
    const string Chaser = "Chaser";
    const string Shooter = "Shooter";
    const string SpreadShooter = "SpreadShooter";
    const string Dasher = "Dasher";

    [MenuItem("Archero/Build Chapter 1")]
    static void BuildChapter1()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("Stop Play mode first, then click Build Chapter 1 again.");
            return;
        }

        Room rest = MakeRoom("Room_Rest", FillRest);

        // The chapter, from first room to last. Change the order here to change the chapter.
        List<Room> rooms = new List<Room>
        {
            LoadRoom("Room1"),
            MakeRoom("Room_Cover", FillCover),
            MakeRoom("Room_Pond", FillPond),
            LoadRoom("Room2"),
            rest,
            MakeRoom("Room_River", FillRiver),
            MakeRoom("Room_Pillars", FillPillars),
            MakeRoom("Room_Gauntlet", FillGauntlet),
            rest,
            LoadRoom("BossRoom"),
        };

        ChapterData chapter = AssetDatabase.LoadAssetAtPath<ChapterData>(ChapterPath);
        chapter.rooms = rooms;
        EditorUtility.SetDirty(chapter); // tells Unity this asset changed and needs saving
        AssetDatabase.SaveAssets();

        Debug.Log("Chapter 1 is ready with " + rooms.Count + " rooms.");
    }

    // ---------- The rooms ----------

    // Easy start with cover to hide behind
    static void FillCover(Transform room)
    {
        AddRock(room, -3f, -1f, 2f, 2f);
        AddRock(room, 3f, -1f, 2f, 2f);
        AddRock(room, 0f, 3f, 2f, 2f);

        AddEnemy(room, Chaser, -4f, 5f);
        AddEnemy(room, Chaser, 4f, 5f);
        AddEnemy(room, Shooter, 0f, 6.5f);
    }

    // First water: a pond in the middle. Chasers walk around it, the shooter fires across it.
    static void FillPond(Transform room)
    {
        AddWater(room, 0f, 0.5f, 6f, 5f);

        AddEnemy(room, Chaser, -4.5f, 2f);
        AddEnemy(room, Chaser, 4.5f, 2f);
        AddEnemy(room, Shooter, 0f, 5.5f);
    }

    // A river across the room with one bridge in the middle, so the bridge is the danger spot
    static void FillRiver(Transform room)
    {
        AddWater(room, -3.75f, 1f, 4.5f, 3f);
        AddWater(room, 3.75f, 1f, 4.5f, 3f);

        AddEnemy(room, SpreadShooter, -4f, 5.5f);
        AddEnemy(room, SpreadShooter, 4f, 5.5f);
        AddEnemy(room, Chaser, -1.5f, 5f);
        AddEnemy(room, Chaser, 1.5f, 5f);
        AddEnemy(room, Dasher, 0f, 6.5f);
    }

    // Pillars to dodge dashes around
    static void FillPillars(Transform room)
    {
        AddRock(room, -3f, -1.5f, 1.5f, 1.5f);
        AddRock(room, 3f, -1.5f, 1.5f, 1.5f);
        AddRock(room, -3f, 3.5f, 1.5f, 1.5f);
        AddRock(room, 3f, 3.5f, 1.5f, 1.5f);

        AddEnemy(room, Dasher, -4.5f, 1f);
        AddEnemy(room, Dasher, 4.5f, 1f);
        AddEnemy(room, SpreadShooter, 0f, 6f);
        AddEnemy(room, Chaser, 0f, 2f);
    }

    // The last fight before the boss: every normal enemy type, with water and rocks
    static void FillGauntlet(Transform room)
    {
        AddWater(room, -4f, 2f, 4f, 4f);
        AddRock(room, 3.5f, -1.5f, 2f, 2f);
        AddRock(room, 3.5f, 4f, 2f, 2f);

        AddEnemy(room, Chaser, -4f, 6f);
        AddEnemy(room, Chaser, 4f, 6.5f);
        AddEnemy(room, Shooter, 0f, 6.5f);
        AddEnemy(room, SpreadShooter, 0.5f, 1.5f);
        AddEnemy(room, Dasher, 5f, 1.5f);
    }

    // No enemies, so Room opens the exit door straight away. The player picks one of the two rewards.
    static void FillRest(Transform room)
    {
        AddReward(room, RewardType.Heal, -2.5f, 1f, "+40% HP", new Color(0.3f, 0.9f, 0.4f));
        AddReward(room, RewardType.Skill, 2.5f, 1f, "NEW SKILL", new Color(1f, 0.8f, 0.2f));
    }

    // ---------- Making prefabs ----------

    static Room LoadRoom(string roomName)
    {
        return AssetDatabase.LoadAssetAtPath<Room>(RoomFolder + roomName + ".prefab");
    }

    // Action<Transform> means "a method that takes a Transform", so each room passes in its own Fill method
    static Room MakeRoom(string roomName, Action<Transform> fill)
    {
        Room existing = LoadRoom(roomName);
        if (existing != null) return existing; // never overwrite a room you may have edited

        string path = RoomFolder + roomName + ".prefab";
        AssetDatabase.CopyAsset(RoomFolder + "Room1.prefab", path);

        // Open the copy without showing it, change it, save it, close it
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        DestroyChildren(root.transform.Find("Enemies"));
        Transform oldRock = root.transform.Find("Walls/Rock_A");
        if (oldRock != null) Object.DestroyImmediate(oldRock.gameObject);
        fill(root.transform);
        PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);

        return LoadRoom(roomName);
    }

    static void DestroyChildren(Transform parent)
    {
        // Go backwards, because removing a child moves the later ones forward
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Object.DestroyImmediate(parent.GetChild(i).gameObject);
        }
    }

    // ---------- Things to put in a room ----------

    // A rock goes under "Walls", so it blocks walking AND breaks arrows and bullets
    static void AddRock(Transform room, float x, float z, float width, float depth)
    {
        Material rockMaterial = AssetDatabase.LoadAssetAtPath<Material>(MaterialFolder + "RockMaterial.mat");

        GameObject rock = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rock.name = "Rock";
        rock.transform.SetParent(room.Find("Walls"), false);
        rock.transform.position = new Vector3(x, 1f, z);
        rock.transform.localScale = new Vector3(width, 2f, depth);
        rock.GetComponent<Renderer>().sharedMaterial = rockMaterial;
    }

    // Water is NOT under "Walls": its collider stops anyone walking in,
    // but arrows and bullets only break on a Wall, so they fly right over it.
    static void AddWater(Transform room, float x, float z, float width, float depth)
    {
        GameObject water = new GameObject("Water");
        water.transform.SetParent(room, false);
        water.transform.position = new Vector3(x, 0f, z);

        // The invisible part that blocks walking
        BoxCollider blocker = water.AddComponent<BoxCollider>();
        blocker.size = new Vector3(width, 2f, depth);
        blocker.center = new Vector3(0f, 1f, 0f);

        // The part you see: a thin blue slab lying on the floor
        Material material = GetMaterial("WaterMaterial", new Color(0.15f, 0.5f, 0.85f));
        material.SetFloat("_Smoothness", 0.9f); // shiny, like a water surface
        AddShape(PrimitiveType.Cube, water.transform, new Vector3(0f, 0.02f, 0f), new Vector3(width, 0.04f, depth), material);
    }

    static void AddEnemy(Transform room, string enemyName, float x, float z)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyFolder + enemyName + ".prefab");

        // InstantiatePrefab keeps the link to the prefab, so changing the Dasher prefab changes every dasher
        GameObject enemy = (GameObject)PrefabUtility.InstantiatePrefab(prefab, room.Find("Enemies"));
        enemy.transform.position = new Vector3(x, 0f, z);
    }

    static void AddReward(Transform room, RewardType type, float x, float z, string label, Color color)
    {
        GameObject shrine = new GameObject(type + "Reward");
        shrine.transform.SetParent(room, false);
        shrine.transform.position = new Vector3(x, 0f, z);

        // AddComponent also adds the Box Collider, because RoomReward requires one
        RoomReward reward = shrine.AddComponent<RoomReward>();
        reward.type = type;

        BoxCollider trigger = shrine.GetComponent<BoxCollider>();
        trigger.size = new Vector3(2f, 2f, 2f);
        trigger.center = new Vector3(0f, 1f, 0f);

        // A flat stone with a colored ball above it
        Material material = GetMaterial(type + "RewardMaterial", color);
        AddShape(PrimitiveType.Cylinder, shrine.transform, new Vector3(0f, 0.1f, 0f), new Vector3(1.6f, 0.1f, 1.6f), material);
        AddShape(PrimitiveType.Sphere, shrine.transform, new Vector3(0f, 1f, 0f), new Vector3(0.7f, 0.7f, 0.7f), material);

        AddLabel(shrine.transform, label, color);
    }

    // A shape only for looks: its collider is removed so it can't block anything
    static void AddShape(PrimitiveType shapeType, Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        GameObject shape = GameObject.CreatePrimitive(shapeType);
        shape.transform.SetParent(parent, false);
        shape.transform.localPosition = position;
        shape.transform.localScale = scale;
        shape.GetComponent<Renderer>().sharedMaterial = material;
        Object.DestroyImmediate(shape.GetComponent<Collider>());
    }

    // 3D text floating above a reward, tilted so it faces the game camera
    static void AddLabel(Transform parent, string text, Color color)
    {
        GameObject labelObject = new GameObject("Label", typeof(TextMesh));
        labelObject.transform.SetParent(parent, false);
        labelObject.transform.localPosition = new Vector3(0f, 2f, 0f);
        labelObject.transform.localRotation = Quaternion.Euler(64f, 0f, 0f);

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        TextMesh textMesh = labelObject.GetComponent<TextMesh>();
        textMesh.text = text;
        textMesh.font = font;
        textMesh.fontSize = 48;
        textMesh.characterSize = 0.08f;
        textMesh.fontStyle = FontStyle.Bold;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.color = color;
        labelObject.GetComponent<MeshRenderer>().sharedMaterial = font.material;
    }

    // Loads a material from Assets/Material, or creates it the first time
    static Material GetMaterial(string materialName, Color color)
    {
        string path = MaterialFolder + materialName + ".mat";

        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.color = color;
            AssetDatabase.CreateAsset(material, path);
        }
        return material;
    }
}
