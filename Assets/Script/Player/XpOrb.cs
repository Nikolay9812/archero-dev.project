using System.Collections.Generic;
using UnityEngine;

// A small glowing ball of XP that a dead enemy drops.
// It hops out a little and lies on the floor until the room is cleared,
// then flies to the player and gives her its XP.
// You never place one by hand: Enemy calls XpOrb.Spawn(...) when it dies.
public class XpOrb : MonoBehaviour
{
    public int value = 5;              // XP given to the player
    public float hopTime = 0.4f;       // how long the "pop out" hop takes
    public float hopHeight = 1f;
    public float startFlySpeed = 4f;   // flying to the player starts slow...
    public float flyAcceleration = 30f; // ...and speeds up every second
    public float collectDistance = 0.5f;

    // Every orb lying in the scene, so Room can call them all at once (same idea as Enemy.All)
    public static readonly List<XpOrb> All = new List<XpOrb>();

    private Vector3 startPosition;
    private Vector3 landPosition;
    private float timer;
    private float flySpeed;
    private bool isCollected; // true = the room is cleared, fly to the player

    // Room calls this when its last enemy dies: every orb on the floor flies to the player
    public static void CollectAll()
    {
        foreach (XpOrb orb in All)
        {
            orb.isCollected = true;
        }
    }

    // Creates one orb at 'position' that lands somewhere within 'scatterRadius' around it
    public static void Spawn(Vector3 position, int value, float scatterRadius)
    {
        GameObject orbObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        orbObject.name = "XpOrb";
        orbObject.transform.position = position;
        orbObject.transform.localScale = Vector3.one * 0.3f;
        orbObject.GetComponent<Renderer>().material.color = new Color(0.3f, 0.9f, 1f);

        // We check the distance to the player ourselves, so the orb needs no collider
        Destroy(orbObject.GetComponent<Collider>());

        XpOrb orb = orbObject.AddComponent<XpOrb>();
        orb.value = value;

        // Pick a random landing spot on the ground (Random.insideUnitCircle gives x and y, we use y as z)
        Vector2 offset = Random.insideUnitCircle * scatterRadius;
        orb.landPosition = position + new Vector3(offset.x, 0f, offset.y);
    }

    void Start()
    {
        startPosition = transform.position;
        flySpeed = startFlySpeed;
    }

    void OnEnable()
    {
        All.Add(this);
    }

    void OnDisable()
    {
        All.Remove(this);
    }

    void Update()
    {
        timer += Time.deltaTime;

        // Always finish the hop first, even if the room got cleared in the meantime
        if (timer < hopTime)
        {
            Hop();
        }
        else if (isCollected)
        {
            FlyToPlayer();
        }
        // otherwise: just lie on the floor and wait
    }

    void Hop()
    {
        float t = timer / hopTime; // goes from 0 to 1 during the hop

        // Slide from the start to the landing spot, plus an arc up and down (Sin goes 0 -> 1 -> 0)
        Vector3 position = Vector3.Lerp(startPosition, landPosition, t);
        position.y += Mathf.Sin(t * Mathf.PI) * hopHeight;
        transform.position = position;
    }

    void FlyToPlayer()
    {
        PlayerHealth player = PlayerHealth.Instance;
        if (player == null || player.IsDead) return; // nobody to collect us: just lie there

        Vector3 target = player.transform.position + Vector3.up; // aim at her chest, not her feet

        flySpeed += flyAcceleration * Time.deltaTime;
        transform.position = Vector3.MoveTowards(transform.position, target, flySpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, target) <= collectDistance)
        {
            PlayerXp xp = player.GetComponent<PlayerXp>();
            if (xp != null) xp.AddXp(value);
            Destroy(gameObject);
        }
    }
}
