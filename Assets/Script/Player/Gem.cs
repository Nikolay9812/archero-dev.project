using UnityEngine;

// A purple gem, the rare currency. All the hopping and flying comes from Pickup, like Coin and XpOrb.
// You never place one by hand: SpinWheel calls Gem.Spawn(...).
public class Gem : Pickup
{
    public int value = 1; // gems added to the wallet

    // Creates one gem at 'position' that lands somewhere within 'scatterRadius' around it
    public static void Spawn(Vector3 position, int value, float scatterRadius)
    {
        GameObject ball = CreateBall("Gem", position, Vector3.one * 0.35f, new Color(0.9f, 0.3f, 1f));

        Gem gem = ball.AddComponent<Gem>();
        gem.value = value;
        gem.ScatterWithin(scatterRadius);
    }

    protected override void GiveReward(PlayerHealth player)
    {
        Wallet.AddGems(value);
        SoundManager.Play(Sound.Gem, true);
    }
}
