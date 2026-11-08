using UnityEngine;

// A small glowing ball of XP. All the hopping and flying comes from Pickup;
// this class only adds what is special about XP.
// You never place one by hand: Enemy calls XpOrb.Spawn(...) when it dies.
public class XpOrb : Pickup
{
    public int value = 5; // XP given to the player

    // Creates one orb at 'position' that lands somewhere within 'scatterRadius' around it
    public static void Spawn(Vector3 position, int value, float scatterRadius)
    {
        GameObject ball = CreateBall("XpOrb", position, Vector3.one * 0.3f, new Color(0.3f, 0.9f, 1f));

        XpOrb orb = ball.AddComponent<XpOrb>();
        orb.value = value;
        orb.ScatterWithin(scatterRadius);
    }

    // "override" = this is our version of the abstract method promised in Pickup
    protected override void GiveReward(PlayerHealth player)
    {
        PlayerXp xp = player.GetComponent<PlayerXp>();
        if (xp != null) xp.AddXp(value);
        SoundManager.Play(Sound.Xp, true);
    }
}
