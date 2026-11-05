using UnityEngine;

// A slow enemy bullet that flies straight, hurts the player on touch and then disappears.
// EnemyShooter and EnemyBoss create these with EnemyBullet.Spawn, so you never place one in the scene yourself.
[RequireComponent(typeof(Rigidbody))]
public class EnemyBullet : MonoBehaviour
{
    public float speed = 7f;
    public int damage = 30;
    public float lifetime = 4f;

    // Creates one bullet flying in 'direction'. Any enemy can call this.
    // 'prefab' may be null: then a simple glowing ball in 'color' is made by code.
    // "static" means it belongs to the class, not to one bullet, so we can call it before any bullet exists.
    public static EnemyBullet Spawn(EnemyBullet prefab, Vector3 position, Vector3 direction, float speed, int damage, Color color)
    {
        Quaternion rotation = Quaternion.LookRotation(direction);

        EnemyBullet bullet = prefab != null
            ? Instantiate(prefab, position, rotation)
            : CreateSimpleBullet(position, rotation, color);

        bullet.speed = speed;
        bullet.damage = damage;
        return bullet;
    }

    // Placeholder bullet until we have a real effect: a small colored sphere
    static EnemyBullet CreateSimpleBullet(Vector3 position, Quaternion rotation, Color color)
    {
        GameObject ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        ball.name = "EnemyBullet";
        ball.transform.SetPositionAndRotation(position, rotation);
        ball.transform.localScale = Vector3.one * 0.35f;
        ball.GetComponent<Renderer>().material.color = color;

        ball.AddComponent<Rigidbody>();
        return ball.AddComponent<EnemyBullet>();
    }

    void Awake()
    {
        // Same trick as the player's Arrow: kinematic trigger moved by script
        Rigidbody body = GetComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;

        foreach (Collider col in GetComponentsInChildren<Collider>()) col.isTrigger = true;

        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        transform.position += transform.forward * speed * Time.deltaTime;
    }

    void OnTriggerEnter(Collider other)
    {
        // Walls and obstacles stop the bullet
        if (other.GetComponentInParent<Wall>() != null)
        {
            Destroy(gameObject);
            return;
        }

        // Fly through enemies, arrows and the floor; only the player matters
        PlayerHealth player = other.GetComponentInParent<PlayerHealth>();
        if (player == null || player.IsDead) return;

        player.TakeDamage(damage);
        Destroy(gameObject);
    }
}
