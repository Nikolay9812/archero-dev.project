using System.Collections.Generic;
using UnityEngine;

// Something a dead enemy drops: it hops out a little and lies on the floor until the room is cleared,
// then flies to the player and gives her its reward.
//
// "abstract" means this class is only a shared base: you can't use a plain Pickup, only a class
// built on top of it (XpOrb, Coin). Each of those inherits all the movement below,
// and only has to say what it looks like and what the player gets (GiveReward).
public abstract class Pickup : MonoBehaviour
{
    public float hopTime = 0.4f;        // how long the "pop out" hop takes
    public float hopHeight = 1f;
    public float startFlySpeed = 4f;    // flying to the player starts slow...
    public float flyAcceleration = 30f; // ...and speeds up every second
    public float collectDistance = 0.5f;

    // Every pickup lying in the scene, so Room can call them all at once (same idea as Enemy.All)
    public static readonly List<Pickup> All = new List<Pickup>();

    private Vector3 startPosition;
    private Vector3 landPosition;
    private float timer;
    private float flySpeed;
    private bool isCollected; // true = the room is cleared, fly to the player

    // Every class built on Pickup must write this method ("abstract" = no code here, only the promise)
    protected abstract void GiveReward(PlayerHealth player);

    // Room calls this when its last enemy dies: every pickup on the floor flies to the player
    public static void CollectAll()
    {
        foreach (Pickup pickup in All)
        {
            pickup.isCollected = true;
        }
    }

    // Makes the ball-shaped object for a pickup. "protected" = only classes built on Pickup can call it.
    protected static GameObject CreateBall(string name, Vector3 position, Vector3 scale, Color color)
    {
        return CreateShape(PrimitiveType.Sphere, name, position, scale, color);
    }

    // The same with any shape (scrolls are cylinders, gear is a cube)
    protected static GameObject CreateShape(PrimitiveType shapeType, string name, Vector3 position, Vector3 scale, Color color)
    {
        GameObject shape = GameObject.CreatePrimitive(shapeType);
        shape.name = name;
        shape.transform.position = position;
        shape.transform.localScale = scale;
        shape.GetComponent<Renderer>().material.color = color;

        // We check the distance to the player ourselves, so the pickup needs no collider
        Destroy(shape.GetComponent<Collider>());
        return shape;
    }

    // Picks a random landing spot on the ground within 'radius' of where we are
    protected void ScatterWithin(float radius)
    {
        // Random.insideUnitCircle gives x and y, we use y as z
        Vector2 offset = Random.insideUnitCircle * radius;
        landPosition = transform.position + new Vector3(offset.x, 0f, offset.y);
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

    protected virtual void Update()
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
            GiveReward(player); // runs the version written in XpOrb or Coin
            Destroy(gameObject);
        }
    }
}
