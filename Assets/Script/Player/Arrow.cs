using UnityEngine;

// An arrow that flies straight forward, damages the first enemy it touches and then disappears.
// PlayerShooter creates arrows, so you normally never place this in the scene yourself.
[RequireComponent(typeof(Rigidbody))]
public class Arrow : MonoBehaviour
{
    public float speed = 20f;
    public int damage = 25;
    public float lifetime = 3f; // seconds before a missed arrow is removed

    void Awake()
    {
        // Kinematic + trigger: the arrow moves by script, and Unity still tells us when it touches something
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
        // Ignore everything that isn't an enemy (the player, the floor, other arrows)
        Enemy enemy = other.GetComponentInParent<Enemy>();
        if (enemy == null || enemy.IsDead) return;

        enemy.TakeDamage(damage);
        Destroy(gameObject);
    }
}
