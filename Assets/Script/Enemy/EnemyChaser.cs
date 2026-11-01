using UnityEngine;

// Melee enemy: runs at the player, stops when close, winds up for a moment and then hits.
// The wind-up gives the player a fair chance to step away.
[RequireComponent(typeof(Enemy))]
public class EnemyChaser : MonoBehaviour
{
    public float speed = 3f;
    public float turnSpeed = 540f;
    public float detectRange = 20f;   // starts chasing when the player is this close
    public float attackRange = 1.3f;  // distance at which it stops and attacks
    public int damage = 40;
    public float windUpTime = 0.35f;  // pause before the hit lands
    public float attackCooldown = 1f;

    private Enemy enemy;
    private float cooldown;
    private float windUpTimer = -1f; // < 0 = not winding up
    private Vector3 normalScale;

    void Awake()
    {
        enemy = GetComponent<Enemy>();
        normalScale = transform.localScale;
    }

    // Runs once when the component is added in the editor: give chasers a red tint
    void Reset()
    {
        GetComponent<Enemy>().bodyColor = new Color(1f, 0.45f, 0.4f);
    }

    void Update()
    {
        cooldown -= Time.deltaTime;

        PlayerHealth player = PlayerHealth.Instance;
        if (player == null || player.IsDead)
        {
            enemy.Stop();
            CancelWindUp();
            return;
        }

        Vector3 toPlayer = player.transform.position - transform.position;
        toPlayer.y = 0f;
        float distance = toPlayer.magnitude;

        if (windUpTimer >= 0f)
        {
            enemy.Stop();
            WindUp(player, distance);
            return;
        }

        if (distance > detectRange)
        {
            enemy.Stop();
            return;
        }

        enemy.TurnTowards(player.transform.position, turnSpeed);

        if (distance > attackRange)
        {
            // The NavMesh finds the way, even if a wall is in between
            enemy.MoveTo(player.transform.position, speed);
            return;
        }

        enemy.Stop();
        if (cooldown <= 0f) windUpTimer = windUpTime;
    }

    void WindUp(PlayerHealth player, float distance)
    {
        windUpTimer -= Time.deltaTime;

        // Swell up a little so the player can see the hit coming
        float progress = 1f - windUpTimer / windUpTime;
        transform.localScale = normalScale * (1f + 0.25f * progress);

        if (windUpTimer > 0f) return;

        // The hit only lands if the player is still close (a bit of extra reach feels fair)
        if (distance <= attackRange + 0.4f) player.TakeDamage(damage);

        CancelWindUp();
        cooldown = attackCooldown;
    }

    void CancelWindUp()
    {
        windUpTimer = -1f;
        transform.localScale = normalScale;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
