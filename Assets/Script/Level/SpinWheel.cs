using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum WheelPrizeType
{
    Coins,
    ProfileXp, // the permanent XP of the player's profile, not the XP of the current run
    Gems
}

// One slice of the wheel. [Serializable] makes it show up in the Inspector.
[System.Serializable]
public class WheelPrize
{
    public WheelPrizeType type;
    public int amount;
}

// A prize wheel for the start room. Walk onto it and it spins once: the game pauses,
// the wheel slows down, and the prize it stops on flies to the player.
// Every prize is permanent: it is kept in the save file, even if the player dies in the first room.
// It builds its own looks when the game starts, so you only need to add this component to an empty GameObject.
// Needs a Box Collider (added and set up automatically).
[RequireComponent(typeof(BoxCollider))]
public class SpinWheel : MonoBehaviour
{
    // Every slice has the same chance. Use the same prize twice to make it more likely.
    public List<WheelPrize> prizes = new List<WheelPrize>
    {
        new WheelPrize { type = WheelPrizeType.Coins, amount = 25 },
        new WheelPrize { type = WheelPrizeType.ProfileXp, amount = 20 },
        new WheelPrize { type = WheelPrizeType.Coins, amount = 50 },
        new WheelPrize { type = WheelPrizeType.ProfileXp, amount = 40 },
        new WheelPrize { type = WheelPrizeType.Coins, amount = 100 },
        new WheelPrize { type = WheelPrizeType.Gems, amount = 3 }
    };

    public float spinTime = 3f;     // seconds until the wheel stops
    public int fullTurns = 5;       // full circles before it stops
    public float resultTime = 1.2f; // how long the prize is shown before the game goes on
    public float radius = 1.6f;
    public int piecesPerPrize = 5;  // a prize flies to the player as this many coins, orbs or gems

    private Transform spinner; // the part that turns (the pointer stays still)
    private TextMesh label;
    private bool used;

    void Awake()
    {
        BoxCollider trigger = GetComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.size = new Vector3(radius * 2f, 2f, radius * 2f);
        trigger.center = new Vector3(0f, 1f, 0f);

        BuildWheel();
    }

    void OnTriggerEnter(Collider other)
    {
        if (used || prizes.Count == 0) return;

        PlayerHealth player = other.GetComponentInParent<PlayerHealth>();
        if (player == null || player.IsDead) return;

        used = true;
        StartCoroutine(Spin());
    }

    IEnumerator Spin()
    {
        Time.timeScale = 0f; // pause, so the player can't walk away during the spin
        label.text = "";

        int prizeIndex = Random.Range(0, prizes.Count);
        WheelPrize prize = prizes[prizeIndex];

        // Slice i sits i * sliceAngle around the wheel. Turning back by that angle puts it under the pointer.
        float sliceAngle = 360f / prizes.Count;
        float endAngle = fullTurns * 360f - prizeIndex * sliceAngle;

        // The game is paused, so Time.deltaTime is 0 here. unscaledDeltaTime keeps counting anyway.
        float t = 0f; // goes from 0 to 1 during the spin
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / spinTime;

            // Starts fast and slows down at the end
            float remaining = 1f - Mathf.Clamp01(t);
            float eased = 1f - remaining * remaining * remaining;

            spinner.localRotation = Quaternion.Euler(0f, endAngle * eased, 0f);
            yield return null; // wait one frame
        }

        label.text = "+" + prize.amount + " " + PrizeName(prize.type);
        label.color = PrizeColor(prize.type);
        yield return new WaitForSecondsRealtime(resultTime); // the normal WaitForSeconds would wait forever while paused

        Time.timeScale = 1f;
        SpawnPrize(prize);
    }

    // Splits the prize into pieces and sends them to the player
    void SpawnPrize(WheelPrize prize)
    {
        int pieces = Mathf.Clamp(piecesPerPrize, 1, prize.amount);
        int pieceValue = prize.amount / pieces;
        int leftover = prize.amount % pieces; // 52 in 5 pieces = 10 each, with 2 left over

        for (int i = 0; i < pieces; i++)
        {
            int value = pieceValue;
            if (i == 0) value += leftover; // the first piece takes the leftover, so nothing is lost

            Vector3 position = transform.position + Vector3.up * 0.5f;
            switch (prize.type)
            {
                case WheelPrizeType.Coins: Coin.Spawn(position, value, radius); break;
                case WheelPrizeType.ProfileXp: ProfileXpOrb.Spawn(position, value, radius); break;
                case WheelPrizeType.Gems: Gem.Spawn(position, value, radius); break;
            }
        }

        Pickup.CollectAll(); // the room has no enemies to clear, so they can fly right away
    }

    static string PrizeName(WheelPrizeType type)
    {
        switch (type)
        {
            case WheelPrizeType.Coins: return "COINS";
            case WheelPrizeType.ProfileXp: return "PROFILE XP";
            default: return "GEMS";
        }
    }

    static Color PrizeColor(WheelPrizeType type)
    {
        switch (type)
        {
            case WheelPrizeType.Coins: return new Color(1f, 0.8f, 0.15f);
            case WheelPrizeType.ProfileXp: return new Color(0.4f, 1f, 0.4f);
            default: return new Color(0.9f, 0.3f, 1f);
        }
    }

    void BuildWheel()
    {
        spinner = new GameObject("Spinner").transform;
        spinner.SetParent(transform, false);

        AddShape(PrimitiveType.Cylinder, spinner, new Vector3(0f, 0.05f, 0f), new Vector3(radius * 2f, 0.05f, radius * 2f), new Color(0.2f, 0.22f, 0.28f));

        // One colored ball per slice, in a circle. Same colors as the coins, profile XP orbs and gems.
        float sliceAngle = 360f / Mathf.Max(prizes.Count, 1);
        for (int i = 0; i < prizes.Count; i++)
        {
            // Rotating "forward" by an angle gives a point on the circle
            Vector3 position = Quaternion.Euler(0f, i * sliceAngle, 0f) * Vector3.forward * (radius * 0.7f);
            position.y = 0.25f;
            AddShape(PrimitiveType.Sphere, spinner, position, Vector3.one * 0.55f, PrizeColor(prizes[i].type));
        }

        // The pointer is a child of the wheel, not of the spinner, so it stays still at the top
        AddShape(PrimitiveType.Cube, transform, new Vector3(0f, 0.3f, radius + 0.15f), new Vector3(0.3f, 0.3f, 0.6f), Color.red);

        BuildLabel();
    }

    // A shape only for looks: its collider is removed so it can't block anything
    void AddShape(PrimitiveType shapeType, Transform parent, Vector3 position, Vector3 scale, Color color)
    {
        GameObject shape = GameObject.CreatePrimitive(shapeType);
        shape.transform.SetParent(parent, false);
        shape.transform.localPosition = position;
        shape.transform.localScale = scale;
        shape.GetComponent<Renderer>().material.color = color;
        Destroy(shape.GetComponent<Collider>());
    }

    // 3D text floating above the wheel, tilted so it faces the game camera
    void BuildLabel()
    {
        GameObject labelObject = new GameObject("Label", typeof(TextMesh));
        labelObject.transform.SetParent(transform, false);
        labelObject.transform.localPosition = new Vector3(0f, 2f, 0f);
        labelObject.transform.localRotation = Quaternion.Euler(64f, 0f, 0f);

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label = labelObject.GetComponent<TextMesh>();
        label.text = "SPIN!";
        label.font = font;
        label.fontSize = 48;
        label.characterSize = 0.08f;
        label.fontStyle = FontStyle.Bold;
        label.anchor = TextAnchor.MiddleCenter;
        labelObject.GetComponent<MeshRenderer>().sharedMaterial = font.material;
    }
}
