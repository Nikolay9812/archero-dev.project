using System.Collections;
using UnityEngine;
using UnityEngine.AI;

// The chapter boss: a big, slow enemy that takes turns between three attacks.
// Between attacks it walks towards the player for a moment.
//   1. Bullet Ring - bullets in every direction at once (hide behind a pillar, or stand in a gap)
//   2. Volley      - a few quick fans of bullets aimed at the player
//   3. Jump Slam   - a red circle shows where it will land, then it jumps there and hurts everyone inside
// Below half health it gets angry: shorter pauses and more bullets.
//
// Each attack is a coroutine, so it reads from top to bottom like a little script:
// "swell up, wait, shoot, wait, shoot...". Compare with EnemyDasher, which uses a state machine instead.
[RequireComponent(typeof(Enemy))]
public class EnemyBoss : MonoBehaviour
{
    public string bossName = "Stone Warden"; // shown above the boss health bar

    [Header("Walking")]
    public float speed = 1.5f;
    public float turnSpeed = 180f;
    public float walkTime = 1.5f;          // walks this long between attacks

    [Header("Bullets")]
    public EnemyBullet bulletPrefab;       // optional: empty = simple glowing balls
    public Color bulletColor = new Color(1f, 0.5f, 0.1f);
    public float bulletSpeed = 6f;
    public int bulletDamage = 40;
    public float shootHeight = 1.2f;
    public float windUpTime = 0.6f;        // swells up this long before every attack, so the player can react

    [Header("Bullet Ring")]
    public int ringBullets = 12;

    [Header("Volley")]
    public int volleyShots = 3;
    public int bulletsPerVolley = 5;
    public float volleySpread = 60f;
    public float timeBetweenVolleys = 0.35f;

    [Header("Jump Slam")]
    public float jumpTime = 0.8f;
    public float jumpHeight = 3f;
    public float maxJumpDistance = 10f;
    public float slamRadius = 2.5f;
    public int slamDamage = 80;
    public float restAfterSlam = 0.8f;     // stands still after landing: a good moment to shoot it
    public Color warningColor = new Color(1f, 0.2f, 0.2f);

    // The boss health bar listens to these.
    // (We write System.Action in full, because "using System;" would make Random ambiguous.)
    public static event System.Action<EnemyBoss> Appeared;
    public static event System.Action Disappeared;

    public Enemy Enemy { get; private set; }

    // "=>" makes a property that is worked out again every time you read it
    private bool IsAngry => Enemy.CurrentHealth <= Enemy.maxHealth / 2;
    private float Pace => IsAngry ? 0.6f : 1f; // multiplies the waits: angry = 40% shorter

    private Vector3 normalScale;
    private Transform warningCircle;
    private int lastAttack = -1;

    void Awake()
    {
        Enemy = GetComponent<Enemy>();
        normalScale = transform.localScale;

        // Game feel for a boss: arrows don't push it around, and its death is a big moment
        Enemy.knockback = 0f;
        Enemy.deathPieces = 30;
        Enemy.deathShake = 0.6f;

        // A boss always drops a piece of gear (at least Great) and a scroll
        Enemy.gearDropChance = 1f;
        Enemy.scrollDropChance = 1f;
        Enemy.minimumGearRarity = Rarity.Great;

        warningCircle = CreateWarningCircle();
    }

    // Runs once when the component is added in the editor: big, tough and dark red
    void Reset()
    {
        Enemy enemy = GetComponent<Enemy>();
        enemy.maxHealth = 1500;
        enemy.bodyColor = new Color(0.8f, 0.3f, 0.3f);
        enemy.xpOrbs = 10;
        enemy.xpPerOrb = 10;
        enemy.coinDrops = 10;
        enemy.coinValue = 10;
    }

    // Start can be a coroutine too! It begins when the room wakes the boss up and runs the whole fight.
    IEnumerator Start()
    {
        Appeared?.Invoke(this);

        while (true)
        {
            PlayerHealth player = PlayerHealth.Instance;
            if (player == null || player.IsDead)
            {
                Enemy.Stop();
                yield return null; // wait one frame and check again
                continue;
            }

            yield return Walk();

            // Pick a random attack, but never the same one twice in a row
            int attack = Random.Range(0, 3);
            if (attack == lastAttack) attack = (attack + 1) % 3;
            lastAttack = attack;

            // "yield return" another coroutine = wait until that attack is completely finished
            if (attack == 0) yield return BulletRing();
            else if (attack == 1) yield return Volley();
            else yield return JumpSlam();
        }
    }

    void OnDestroy()
    {
        Disappeared?.Invoke();

        // The circle is not our child, so it must be cleaned up by hand
        if (warningCircle != null) Destroy(warningCircle.gameObject);
    }

    // ---------- Walking and winding up ----------

    IEnumerator Walk()
    {
        float timer = walkTime * Pace;
        while (timer > 0f)
        {
            PlayerHealth player = PlayerHealth.Instance;
            if (player == null || player.IsDead) break;

            Enemy.MoveTo(player.transform.position, speed);
            Enemy.TurnTowards(player.transform.position, turnSpeed);

            timer -= Time.deltaTime;
            yield return null; // continue here next frame
        }
        Enemy.Stop();
    }

    // Swells up while turning to the player: "an attack is coming!"
    IEnumerator WindUp()
    {
        float time = windUpTime * Pace;
        float elapsed = 0f;
        while (elapsed < time)
        {
            elapsed += Time.deltaTime;
            transform.localScale = normalScale * (1f + 0.15f * (elapsed / time));

            PlayerHealth player = PlayerHealth.Instance;
            if (player != null) Enemy.TurnTowards(player.transform.position, turnSpeed);

            yield return null;
        }
        transform.localScale = normalScale;
    }

    // ---------- Attack 1: Bullet Ring ----------

    IEnumerator BulletRing()
    {
        yield return WindUp();
        ShootRing(0f);

        // Angry: a second ring, turned by half a gap, so the safe spots from the first ring are gone
        if (IsAngry)
        {
            yield return new WaitForSeconds(0.4f);
            ShootRing(180f / ringBullets);
        }
    }

    void ShootRing(float startAngle)
    {
        float step = 360f / ringBullets; // degrees between two bullets
        for (int i = 0; i < ringBullets; i++)
        {
            Vector3 direction = Quaternion.Euler(0f, startAngle + i * step, 0f) * Vector3.forward;
            ShootBullet(direction);
        }
    }

    // ---------- Attack 2: Volley ----------

    IEnumerator Volley()
    {
        yield return WindUp();

        int shots = IsAngry ? volleyShots + 2 : volleyShots;
        for (int shot = 0; shot < shots; shot++)
        {
            PlayerHealth player = PlayerHealth.Instance;
            if (player == null) yield break; // "yield break" = stop this coroutine now

            ShootFan(player.transform.position);
            yield return new WaitForSeconds(timeBetweenVolleys);
        }
    }

    // The same fan math as EnemyShooter: spread the bullets evenly from -spread/2 to +spread/2
    void ShootFan(Vector3 target)
    {
        Vector3 direction = target - transform.position;
        direction.y = 0f;
        direction.Normalize();

        for (int i = 0; i < bulletsPerVolley; i++)
        {
            float angle = 0f;
            if (bulletsPerVolley > 1) angle = Mathf.Lerp(-volleySpread / 2f, volleySpread / 2f, i / (bulletsPerVolley - 1f));
            ShootBullet(Quaternion.Euler(0f, angle, 0f) * direction);
        }
    }

    // ---------- Attack 3: Jump Slam ----------

    IEnumerator JumpSlam()
    {
        PlayerHealth player = PlayerHealth.Instance;
        if (player == null) yield break;

        // Aim for the player, but not further than maxJumpDistance and not through a wall or pillar
        Vector3 start = Enemy.FloorPosition;
        Vector3 target = player.transform.position;
        target.y = start.y;
        Vector3 jump = Vector3.ClampMagnitude(target - start, maxJumpDistance);
        if (NavMesh.Raycast(start, start + jump, out NavMeshHit hit, NavMesh.AllAreas)) jump = hit.position - start;
        Vector3 landing = start + jump;

        // The landing spot is locked now: the red circle shows it, so the player can run away
        ShowWarning(landing);
        yield return WindUp();

        // Each frame: move this frame's piece of the jump, and lift the body along an arc.
        // Sin goes 0 -> 1 -> 0 while progress goes 0 -> 1, which is exactly the shape of a jump.
        float elapsed = 0f;
        Vector3 movedSoFar = Vector3.zero;
        while (elapsed < jumpTime)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / jumpTime);

            Vector3 shouldBeAt = jump * progress;
            Enemy.Push(shouldBeAt - movedSoFar);
            movedSoFar = shouldBeAt;

            Enemy.Lift(Mathf.Sin(progress * Mathf.PI) * jumpHeight);
            yield return null;
        }
        Enemy.Lift(0f);
        warningCircle.gameObject.SetActive(false);

        // Landed: hurt the player if she is still inside the circle
        if (!player.IsDead && FlatDistance(player.transform.position, landing) <= slamRadius)
        {
            player.TakeDamage(slamDamage);
        }

        // Angry: the landing also throws out a ring of bullets
        if (IsAngry) ShootRing(0f);

        yield return new WaitForSeconds(restAfterSlam * Pace);
    }

    void ShowWarning(Vector3 position)
    {
        warningCircle.position = position + Vector3.up * 0.03f;
        warningCircle.localScale = new Vector3(slamRadius * 2f, 0.01f, slamRadius * 2f); // a cylinder is 1 wide
        warningCircle.gameObject.SetActive(true);
    }

    // Placeholder effect: a very flat cylinder lying on the floor
    Transform CreateWarningCircle()
    {
        GameObject circle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        circle.name = "SlamWarningCircle";
        Destroy(circle.GetComponent<Collider>()); // just a picture, nothing should bump into it
        RuntimeMaterial.Paint(circle.GetComponent<Renderer>(), warningColor);
        circle.SetActive(false);
        return circle.transform;
    }

    // ---------- Helpers ----------

    void ShootBullet(Vector3 direction)
    {
        Vector3 position = new Vector3(transform.position.x, shootHeight, transform.position.z) + direction * 1.2f;
        EnemyBullet.Spawn(bulletPrefab, position, direction, bulletSpeed, bulletDamage, bulletColor);
    }

    static float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = warningColor;
        Gizmos.DrawWireSphere(transform.position, slamRadius);
    }
}
