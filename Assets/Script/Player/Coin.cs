using UnityEngine;

// A flat gold coin. All the hopping and flying comes from Pickup, exactly like XpOrb.
// The difference: XP is gone when the run ends, coins go into the Wallet and are kept forever.
// You never place one by hand: Enemy calls Coin.Spawn(...) when it dies.
public class Coin : Pickup
{
    public int value = 5; // coins added to the wallet
    public float spinSpeed = 360f;

    protected override void Update() {
        base.Update();

        transform.Rotate(spinSpeed * Time.deltaTime, 0f, 0f);
    }

    // Creates one coin at 'position' that lands somewhere within 'scatterRadius' around it
    public static void Spawn(Vector3 position, int value, float scatterRadius)
    {
        Vector3 flatScale = new Vector3(0.4f, 0.08f, 0.4f);
        GameObject ball = CreateBall("Coin", position, flatScale, new Color(1f, 0.8f, 0.15f));

        Coin coin = ball.AddComponent<Coin>();
        coin.value = value;
        coin.ScatterWithin(scatterRadius);
    }

    protected override void GiveReward(PlayerHealth player)
    {
        Wallet.Add(value);
    }
}
