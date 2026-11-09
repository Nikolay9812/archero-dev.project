using UnityEngine;

// An arrow that flies straight forward, damages the first enemy it touches and then disappears.
// PlayerShooter creates arrows, so you normally never place this in the scene yourself.
[RequireComponent(typeof(Rigidbody))]
public class Arrow : MonoBehaviour
{
    public float speed = 20f;
    public int damage = 25;
    public float lifetime = 3f; // seconds before a missed arrow is removed
    public int bounces = 0;          // Ricochet skill: jumps to another enemy this many times
    public float bounceRange = 7f;   // only jumps to enemies this close

    // Set by PlayerShooter from the artifacts
    public float critChance = 0f;
    public float critMultiplier = 2f;
    public float bossMultiplier = 1f;

    private Enemy lastHit; // the enemy we just bounced off, so we don't hit it again right away

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
        // Walls and obstacles stop the arrow
        if (other.GetComponentInParent<Wall>() != null)
        {
            Destroy(gameObject);
            return;
        }

        // Ignore everything else that isn't an enemy (the player, the floor, other arrows)
        Enemy enemy = other.GetComponentInParent<Enemy>();
        if (enemy == null || enemy.IsDead || enemy == lastHit) return;

        // Each hit works out its own damage, so one arrow can crit on one enemy and not on the next
        float hit = damage;
        if (enemy.GetComponent<EnemyBoss>() != null) hit *= bossMultiplier;

        bool critical = Random.value < critChance; // critChance 0 = never
        if (critical) hit *= critMultiplier;

        enemy.TakeDamage(Mathf.RoundToInt(hit), critical);
        enemy.Knockback(transform.forward); // push it back the way the arrow was flying

        // Ricochet: fly on to the next enemy instead of disappearing
        if (bounces > 0 && BounceAwayFrom(enemy)) return;

        Destroy(gameObject);
    }

    // Turns the arrow towards the nearest other enemy. Returns false if there is none in range.
    bool BounceAwayFrom(Enemy hitEnemy)
    {
        Enemy next = null;
        float nearestDistance = bounceRange * bounceRange; // squared, like in PlayerShooter

        foreach (Enemy other in Enemy.All)
        {
            if (other == hitEnemy || other.IsDead) continue;

            float distance = (other.transform.position - transform.position).sqrMagnitude;
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                next = other;
            }
        }

        if (next == null) return false;

        bounces--;
        lastHit = hitEnemy;

        Vector3 toNext = next.transform.position - transform.position;
        toNext.y = 0f; // stay at the same height
        transform.rotation = Quaternion.LookRotation(toNext);
        return true;
    }
}
