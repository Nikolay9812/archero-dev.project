using UnityEngine;

// A slow enemy bullet that flies straight, hurts the player on touch and then disappears.
// EnemyShooter creates these, so you normally never place this in the scene yourself.
[RequireComponent(typeof(Rigidbody))]
public class EnemyBullet : MonoBehaviour
{
    public float speed = 7f;
    public int damage = 30;
    public float lifetime = 4f;

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
