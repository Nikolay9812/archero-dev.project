using UnityEngine;

// Archero auto-shoot: while the player stands still, turn to the nearest enemy and fire arrows.
// As soon as the player moves again, shooting stops.
[RequireComponent(typeof(PlayerMovement))]
public class PlayerShooter : MonoBehaviour
{
    // Optional: drag an arrow prefab here (it needs the Arrow script).
    // If left empty, a simple stick-shaped arrow is created by code.
    public Arrow arrowPrefab;

    public int damage = 25;            // given to every arrow we shoot (skills raise it)
    public float fireRate = 2f;        // arrows per second
    public float range = 15f;          // enemies further away are ignored
    public float turnSpeed = 720f;     // degrees per second when turning to aim
    public float aimTolerance = 10f;   // only shoot when facing the enemy within this angle
    public Vector3 shootOffset = new Vector3(0f, 1f, 0.6f); // where arrows spawn (up, forward from the player)

    // Arrow skills (PlayerSkills raises these)
    public int frontArrows = 1;          // arrows flying straight ahead, side by side
    public float frontArrowSpacing = 0.4f;
    public int diagonalPairs = 0;        // each pair = one arrow to the left and one to the right
    public float diagonalAngle = 30f;    // angle of the first pair; the second pair is twice as wide, and so on
    public int ricochetBounces = 0;      // how many times each arrow jumps to another enemy after a hit

    // Artifact stats (set in Awake, passed on to every arrow)
    public float critChance = 0f;        // 0.05 = 5% of all hits are critical
    public float critMultiplier = 2f;    // a critical hit does this many times the damage
    public float bossMultiplier = 1f;    // 1.1 = 10% more damage against bosses

    public Enemy CurrentTarget { get; private set; }

    // Fired when she STARTS drawing the bow (PlayerAnimation listens to it to play the bow animation).
    // The arrow itself only flies a moment later, when her hand lets go in the animation.
    public event System.Action StartedDrawing;

    private PlayerMovement movement;
    private PlayerAnimation playerAnimation;
    private float cooldown;
    private bool isDrawing;   // true = the bow is being drawn, the arrow is about to fly
    private float drawTimer;  // seconds left until the arrow is released

    void Awake()
    {
        movement = GetComponent<PlayerMovement>();
        playerAnimation = GetComponent<PlayerAnimation>();

        // Permanent talents from the Collectible page
        damage += Upgrades.BonusDamage;
        fireRate += Upgrades.BonusFireRate;

        // Worn gear from the GEAR page
        damage += Inventory.BonusInt(GearStat.Damage);
        fireRate += Inventory.Bonus(GearStat.FireRate);

        // Found artifacts from the Collectible page: first the flat bonus, then the percent bonuses on top
        damage += Artifacts.BonusInt(ArtifactStat.Attack);
        damage = Mathf.RoundToInt(damage * Artifacts.Multiplier(ArtifactStat.AttackPercent));
        fireRate *= Artifacts.Multiplier(ArtifactStat.AttackSpeedPercent);
        critChance = Artifacts.Bonus(ArtifactStat.CritChance) / 100f; // 5% -> 0.05
        critMultiplier = 2f + Artifacts.Bonus(ArtifactStat.CritDamage) / 100f;
        bossMultiplier = Artifacts.Multiplier(ArtifactStat.BossDamage);
    }

    void Update()
    {
        cooldown -= Time.deltaTime;

        // Moving = no shooting. This is the core rule of the game.
        // Moving in the middle of a draw cancels that arrow.
        if (movement.IsMoving)
        {
            CurrentTarget = null;
            isDrawing = false;
            return;
        }

        CurrentTarget = FindNearestEnemy();
        if (CurrentTarget == null)
        {
            isDrawing = false;
            return;
        }

        // Turn on the ground plane only, so she doesn't tilt up or down
        Vector3 toEnemy = CurrentTarget.transform.position - transform.position;
        toEnemy.y = 0f;
        if (toEnemy.sqrMagnitude < 0.001f) return;

        Quaternion targetRotation = Quaternion.LookRotation(toEnemy);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);

        // 2. The bow is drawn: count down to the release, then let the arrows fly.
        //    She keeps turning to the target meanwhile, so we aim at where it is NOW.
        if (isDrawing)
        {
            drawTimer -= Time.deltaTime;
            if (drawTimer <= 0f)
            {
                isDrawing = false;
                Shoot(toEnemy.normalized);
            }
            return;
        }

        // 1. Ready and aimed: start drawing the bow (this starts the animation)
        bool isAimed = Quaternion.Angle(transform.rotation, targetRotation) <= aimTolerance;
        if (isAimed && cooldown <= 0f)
        {
            cooldown = 1f / fireRate;
            isDrawing = true;
            // How long until her hand lets go in the animation (0 = no animation, shoot right away)
            drawTimer = playerAnimation != null ? playerAnimation.ReleaseDelay(fireRate) : 0f;
            StartedDrawing?.Invoke();
        }
    }

    Enemy FindNearestEnemy()
    {
        Enemy nearest = null;
        float nearestDistance = range * range; // compare squared distances (faster, same result)

        foreach (Enemy enemy in Enemy.All)
        {
            if (enemy.IsDead) continue;

            float distance = (enemy.transform.position - transform.position).sqrMagnitude;
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = enemy;
            }
        }
        return nearest;
    }

    void Shoot(Vector3 direction)
    {
        SoundManager.Play(Sound.Shoot, true); // one sound per volley, not per arrow

        // Front arrows: side by side, centered on the aim line.
        // 'right' points to the player's right; Cross gives the direction at 90 degrees to both vectors.
        Vector3 right = Vector3.Cross(Vector3.up, direction);
        for (int i = 0; i < frontArrows; i++)
        {
            // 1 arrow: offset 0. 2 arrows: -0.5 and +0.5 spacing. 3 arrows: -1, 0, +1 spacing...
            float offset = (i - (frontArrows - 1) / 2f) * frontArrowSpacing;
            SpawnArrow(direction, right * offset);
        }

        // Diagonal arrows: turn the aim direction left and right around the up axis
        for (int pair = 1; pair <= diagonalPairs; pair++)
        {
            float angle = pair * diagonalAngle;
            SpawnArrow(Quaternion.Euler(0f, angle, 0f) * direction, Vector3.zero);
            SpawnArrow(Quaternion.Euler(0f, -angle, 0f) * direction, Vector3.zero);
        }
    }

    void SpawnArrow(Vector3 direction, Vector3 sideOffset)
    {
        Vector3 spawnPosition = transform.position + Vector3.up * shootOffset.y + direction * shootOffset.z + sideOffset;
        Quaternion spawnRotation = Quaternion.LookRotation(direction);

        Arrow arrow;
        if (arrowPrefab != null) arrow = Instantiate(arrowPrefab, spawnPosition, spawnRotation);
        else arrow = CreateSimpleArrow(spawnPosition, spawnRotation);

        arrow.damage = damage;
        arrow.bounces = ricochetBounces;
        arrow.critChance = critChance;
        arrow.critMultiplier = critMultiplier;
        arrow.bossMultiplier = bossMultiplier;
    }

    // Placeholder arrow until we have a real model: a thin stretched cube
    Arrow CreateSimpleArrow(Vector3 position, Quaternion rotation)
    {
        GameObject arrow = GameObject.CreatePrimitive(PrimitiveType.Cube);
        arrow.name = "Arrow";
        arrow.transform.SetPositionAndRotation(position, rotation);
        arrow.transform.localScale = new Vector3(0.08f, 0.08f, 0.7f);
        arrow.GetComponent<Renderer>().material.color = new Color(1f, 0.85f, 0.3f);

        arrow.AddComponent<Rigidbody>();
        return arrow.AddComponent<Arrow>();
    }

    // Shows the shooting range in the Scene view when the player is selected
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, range);
    }
}
