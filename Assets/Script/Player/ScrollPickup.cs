using UnityEngine;

// A rolled-up scroll for one gear slot (weapon scroll, head scroll...). Scrolls level up gear.
// All the hopping and flying comes from Pickup, like Coin. Enemies drop them rarely, the wheel can give some.
public class ScrollPickup : Pickup
{
    public static readonly Color ScrollColor = new Color(0.95f, 0.85f, 0.6f); // old paper

    public GearSlot slot;
    public float spinSpeed = 180f;

    // Creates one scroll for a random slot
    public static void SpawnRandom(Vector3 position, float scatterRadius)
    {
        GearSlot randomSlot = (GearSlot)Random.Range(0, GearCatalog.SlotCount);
        Spawn(position, randomSlot, scatterRadius);
    }

    public static void Spawn(Vector3 position, GearSlot slot, float scatterRadius)
    {
        GameObject roll = CreateShape(PrimitiveType.Cylinder, "Scroll", position, new Vector3(0.18f, 0.25f, 0.18f), ScrollColor);

        ScrollPickup scroll = roll.AddComponent<ScrollPickup>();
        scroll.slot = slot;
        scroll.ScatterWithin(scatterRadius);
    }

    protected override void Update()
    {
        base.Update();

        // Lie on its side (90 degrees around Z) and turn slowly around the up axis
        transform.rotation = Quaternion.Euler(0f, Time.time * spinSpeed, 90f);
    }

    protected override void GiveReward(PlayerHealth player)
    {
        Inventory.AddScrolls(slot, 1);
        SoundManager.Play(Sound.Gem, true);
    }
}
