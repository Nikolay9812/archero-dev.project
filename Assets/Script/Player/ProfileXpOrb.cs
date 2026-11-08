using UnityEngine;

// A green orb of profile XP, which is kept forever (the blue XpOrb only counts for the current run).
// All the hopping and flying comes from Pickup. You never place one by hand: SpinWheel calls ProfileXpOrb.Spawn(...).
public class ProfileXpOrb : Pickup
{
    public int value = 5; // profile XP given to the player

    // Creates one orb at 'position' that lands somewhere within 'scatterRadius' around it
    public static void Spawn(Vector3 position, int value, float scatterRadius)
    {
        GameObject ball = CreateBall("ProfileXpOrb", position, Vector3.one * 0.3f, new Color(0.4f, 1f, 0.4f));

        ProfileXpOrb orb = ball.AddComponent<ProfileXpOrb>();
        orb.value = value;
        orb.ScatterWithin(scatterRadius);
    }

    protected override void GiveReward(PlayerHealth player)
    {
        PlayerProfile.AddXp(value);
        SoundManager.Play(Sound.Xp, true);
    }
}
