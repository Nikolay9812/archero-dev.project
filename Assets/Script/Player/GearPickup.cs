using UnityEngine;

// A piece of gear lying on the floor: a spinning cube in the color of its rarity.
// Which item it is gets decided when it drops, so the color already tells the player how good it is.
// It goes into the Inventory (and the save file) when it reaches the player.
public class GearPickup : Pickup
{
    public string typeId;
    public Rarity rarity;
    public float spinSpeed = 120f;

    // Creates a random item. 'minimum' = the worst rarity it can be.
    public static void SpawnRandom(Vector3 position, Rarity minimum, float scatterRadius)
    {
        Spawn(position, GearCatalog.RandomType(), GearCatalog.RandomRarity(minimum), scatterRadius);
    }

    public static void Spawn(Vector3 position, GearType type, Rarity rarity, float scatterRadius)
    {
        GameObject box = CreateShape(PrimitiveType.Cube, "Gear", position, Vector3.one * 0.45f, GearCatalog.RarityColor(rarity));

        GearPickup gear = box.AddComponent<GearPickup>();
        gear.typeId = type.id;
        gear.rarity = rarity;
        gear.ScatterWithin(scatterRadius);
    }

    protected override void Update()
    {
        base.Update();

        transform.rotation = Quaternion.Euler(20f, Time.time * spinSpeed, 20f);
    }

    protected override void GiveReward(PlayerHealth player)
    {
        Inventory.AddGear(GearCatalog.Find(typeId), rarity);
        SoundManager.Play(Sound.WheelPrize);
    }
}
