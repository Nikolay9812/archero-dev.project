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
    public int bulletsPerShot = 1;       // more than 1 = a fan of bullets ("spread shooter")
    public float spreadAngle = 50f;      // width of the fan in degrees, from the leftmost to the rightmost bullet
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
            enemy.Stop();
            CancelWindUp();
            return;
        }

        Vector3 toPlayer = player.transform.position - transform.position;
        toPlayer.y = 0f;
        float distance = toPlayer.magnitude;
        if (distance > detectRange)
        {
            enemy.Stop();
            return;
        }

        enemy.TurnTowards(player.transform.position, turnSpeed);

        if (windUpTimer >= 0f)
        {
            enemy.Stop();
            WindUp(player);
            return;
        }

        // Back off when too close, step in when too far, otherwise hold still.
        // To back off we aim for a spot 2 m behind us; the NavMesh stops us at walls.
        Vector3 direction = toPlayer / distance;
        if (distance < preferredDistance - 1f) enemy.MoveTo(transform.position - direction * 2f, speed);
        else if (distance > preferredDistance + 1f) enemy.MoveTo(player.transform.position, speed);
        else enemy.Stop();

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

        for (int i = 0; i < bulletsPerShot; i++)
        {
            // Spread the bullets evenly across the fan. With 3 bullets and 50°: -25°, 0°, +25°.
            // (i / (bulletsPerShot - 1f)) goes from 0 to 1, Lerp turns that into an angle.
            float angle = 0f;
            if (bulletsPerShot > 1) angle = Mathf.Lerp(-spreadAngle / 2f, spreadAngle / 2f, i / (bulletsPerShot - 1f));

            SpawnBullet(Quaternion.Euler(0f, angle, 0f) * direction);
        }
    }

    void SpawnBullet(Vector3 direction)
    {
        Vector3 spawnPosition = new Vector3(transform.position.x, shootHeight, transform.position.z) + direction * 0.7f;
        EnemyBullet.Spawn(bulletPrefab, spawnPosition, direction, bulletSpeed, damage, bulletColor);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.7f, 0.45f, 1f);
        Gizmos.DrawWireSphere(transform.position, preferredDistance);
    }
}
