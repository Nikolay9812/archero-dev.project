using UnityEngine;

// Ranged enemy: keeps its distance from the player and fires slow bullets at her.
// Before each shot it swells up for a moment, so the player can see it coming and dodge.
[RequireComponent(typeof(Enemy))]
public class EnemyShooter : MonoBehaviour
{
    // Optional: drag a bullet prefab here (it needs the EnemyBullet script).
    // If left empty, a simple glowing ball is created by code.
    public EnemyBullet bulletPrefab;

    public float speed = 2f;
    public float turnSpeed = 360f;
    public float detectRange = 18f;
    public float preferredDistance = 7f; // tries to stay about this far away
    public float fireInterval = 2f;      // seconds between shots
    public float windUpTime = 0.5f;
    public int damage = 30;
    public float bulletSpeed = 7f;
    public Color bulletColor = new Color(1f, 0.3f, 0.9f);
    public float shootHeight = 1f;

    private Enemy enemy;
    private float cooldown;
    private float windUpTimer = -1f; // < 0 = not winding up
    private Vector3 normalScale;

    void Awake()
    {
        enemy = GetComponent<Enemy>();
        normalScale = transform.localScale;
        cooldown = Random.Range(0.5f, fireInterval); // so a group doesn't fire all at once
    }

    // Runs once when the component is added in the editor: give shooters a purple tint
    void Reset()
    {
        GetComponent<Enemy>().bodyColor = new Color(0.7f, 0.45f, 1f);
    }

    void Update()
    {
        PlayerHealth player = PlayerHealth.Instance;
        if (player == null || player.IsDead)
        {
            CancelWindUp();
            return;
        }

        Vector3 toPlayer = player.transform.position - transform.position;
        toPlayer.y = 0f;
        float distance = toPlayer.magnitude;
        if (distance > detectRange) return;

        enemy.TurnTowards(player.transform.position, turnSpeed);

        if (windUpTimer >= 0f)
        {
            WindUp(player);
            return;
        }

        // Back off when too close, step in when too far, otherwise hold still
        Vector3 direction = toPlayer / distance;
        if (distance < preferredDistance - 1f) enemy.Move(-direction, speed);
        else if (distance > preferredDistance + 1f) enemy.Move(direction, speed);

        cooldown -= Time.deltaTime;
        if (cooldown <= 0f) windUpTimer = windUpTime;
    }

    void WindUp(PlayerHealth player)
    {
        windUpTimer -= Time.deltaTime;

        float progress = 1f - windUpTimer / windUpTime;
        transform.localScale = normalScale * (1f + 0.2f * progress);

        if (windUpTimer > 0f) return;

        Shoot(player);
        CancelWindUp();
        cooldown = fireInterval;
    }

    void CancelWindUp()
    {
        windUpTimer = -1f;
        transform.localScale = normalScale;
    }

    void Shoot(PlayerHealth player)
    {
        Vector3 direction = player.transform.position - transform.position;
        direction.y = 0f;
        direction.Normalize();

        Vector3 spawnPosition = new Vector3(transform.position.x, shootHeight, transform.position.z) + direction * 0.7f;
        Quaternion spawnRotation = Quaternion.LookRotation(direction);

        EnemyBullet bullet = bulletPrefab != null
            ? Instantiate(bulletPrefab, spawnPosition, spawnRotation)
            : CreateSimpleBullet(spawnPosition, spawnRotation);

        bullet.speed = bulletSpeed;
        bullet.damage = damage;
    }

    // Placeholder bullet until we have a real effect: a small colored sphere
    EnemyBullet CreateSimpleBullet(Vector3 position, Quaternion rotation)
    {
        GameObject ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        ball.name = "EnemyBullet";
        ball.transform.SetPositionAndRotation(position, rotation);
        ball.transform.localScale = Vector3.one * 0.35f;
        ball.GetComponent<Renderer>().material.color = bulletColor;

        ball.AddComponent<Rigidbody>();
        return ball.AddComponent<EnemyBullet>();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.7f, 0.45f, 1f);
        Gizmos.DrawWireSphere(transform.position, preferredDistance);
    }
}
