using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// A simple enemy: it has health, flashes when hit and disappears when health reaches 0.
// Put it on any object that has a Collider (e.g. a Capsule), so arrows can hit it.
// It walks with a NavMeshAgent, so it finds its way around walls (the room needs a baked NavMesh).
[RequireComponent(typeof(NavMeshAgent))]
public class Enemy : MonoBehaviour
{
    public int maxHealth = 100;
    public Color bodyColor = Color.white; // tints the whole enemy, so different types are easy to tell apart
    public Color hitFlashColor = Color.white;
    public float hitFlashTime = 0.1f;

    // XP dropped on death: this many orbs, each worth xpPerOrb
    public int xpOrbs = 3;
    public int xpPerOrb = 5;

    // Coins dropped on death: this many coins, each worth coinValue
    public int coinDrops = 2;
    public int coinValue = 5;

    [Header("Game feel")]
    public Color damageNumberColor = Color.white;
    public float knockback = 3f;       // how hard arrows push it back (0 = not at all, the boss uses 0)
    public int deathPieces = 8;        // little cubes that fly out when it dies
    public float deathShake = 0.15f;   // how much the camera shakes when it dies

    // Every living enemy in the scene. The player's auto-shoot looks here for the nearest target,
    // so we don't need tags or FindObjectsByType every frame.
    public static readonly List<Enemy> All = new List<Enemy>();

    // Fired every time this enemy takes damage (the boss health bar listens to it)
    public event Action HealthChanged;

    public int CurrentHealth { get; private set; }
    public bool IsDead => CurrentHealth <= 0;

    private NavMeshAgent agent;
    private float normalBaseOffset;
    private Renderer[] renderers;
    private Color[] originalColors;
    private float flashTimer;
    private Vector3 knockbackVelocity;

    void Awake()
    {
        CurrentHealth = maxHealth;

        // The agent finds the path and moves us; turning is done by our own TurnTowards
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false;
        agent.acceleration = 40f; // start and stop quickly instead of sliding
        normalBaseOffset = agent.baseOffset;

        // Remember the normal colors so we can flash and then go back
        renderers = GetComponentsInChildren<Renderer>();
        originalColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].material.color *= bodyColor;
            originalColors[i] = renderers[i].material.color;
        }
    }

    // Walks towards a point, going around walls on the way. Used by EnemyChaser and EnemyShooter.
    // The agent also steers around other enemies, so a group doesn't melt into one blob.
    public void MoveTo(Vector3 destination, float speed)
    {
        if (!agent.isOnNavMesh) return; // e.g. placed off the floor, or the NavMesh isn't baked yet

        agent.speed = speed;
        agent.isStopped = false;
        agent.SetDestination(destination);
    }

    // Stands still (for example while winding up an attack)
    public void Stop()
    {
        if (agent.isOnNavMesh) agent.isStopped = true;
    }

    // Moves straight by 'offset' right now, without path finding (used for dashes).
    // agent.Move keeps us on the NavMesh, so we slide along walls instead of going through them.
    public void Push(Vector3 offset)
    {
        if (agent.isOnNavMesh) agent.Move(offset);
    }

    // Lifts the body 'height' meters above its normal height (used for the boss jump).
    // The agent keeps walking on the NavMesh underneath; only the body goes up. Lift(0) = back down.
    public void Lift(float height)
    {
        agent.baseOffset = normalBaseOffset + height;
    }

    // The agent floats 'baseOffset' above the NavMesh, so this is the point on the floor under us
    public Vector3 FloorPosition => transform.position - Vector3.up * agent.baseOffset;

    // Turns to face a point on the ground (no tilting)
    public void TurnTowards(Vector3 point, float turnSpeed)
    {
        Vector3 toPoint = point - transform.position;
        toPoint.y = 0f;
        if (toPoint.sqrMagnitude < 0.001f) return;

        Quaternion targetRotation = Quaternion.LookRotation(toPoint);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
    }

    void OnEnable()
    {
        All.Add(this);
    }

    void OnDisable()
    {
        All.Remove(this);
    }

    void Update()
    {
        // Knockback: slide back and slow down quickly (Lerp moves the speed a bit closer to zero every frame)
        if (knockbackVelocity.sqrMagnitude > 0.01f)
        {
            Push(knockbackVelocity * Time.deltaTime);
            knockbackVelocity = Vector3.Lerp(knockbackVelocity, Vector3.zero, 10f * Time.deltaTime);
        }

        if (flashTimer <= 0f) return;

        flashTimer -= Time.deltaTime;
        if (flashTimer <= 0f) SetColor(null);
    }

    public void TakeDamage(int amount)
    {
        if (IsDead) return;

        CurrentHealth -= amount;
        flashTimer = hitFlashTime;
        SetColor(hitFlashColor);
        HealthChanged?.Invoke();
        DamageNumber.Spawn(TopOfBody() + Vector3.up * 0.3f, amount, damageNumberColor);

        if (IsDead) Die();
    }

    // Pushes the enemy back a little, in 'direction' (Arrow calls this with its flying direction)
    public void Knockback(Vector3 direction)
    {
        direction.y = 0f; // only along the ground
        knockbackVelocity = direction.normalized * knockback;
    }

    // The highest point of the model, so damage numbers appear above the head of small AND big enemies
    Vector3 TopOfBody()
    {
        float top = transform.position.y;
        foreach (Renderer r in renderers)
        {
            if (r != null) top = Mathf.Max(top, r.bounds.max.y);
        }
        return new Vector3(transform.position.x, top, transform.position.z);
    }

    void Die()
    {
        // Orbs only fly to the player once the room is cleared. In the last room the chapter is over
        // by then, so a level up (and its skill cards) would be pointless: drop no XP there.
        if (!ChapterManager.IsLastRoom)
        {
            for (int i = 0; i < xpOrbs; i++)
            {
                XpOrb.Spawn(FloorPosition + Vector3.up * 0.3f, xpPerOrb, 1.2f);
            }
        }

        // The Coin Bonus talent makes each coin worth more. 100 + 20 = 120% of the normal value.
        int valueWithBonus = Mathf.RoundToInt(coinValue * (100 + Upgrades.CoinBonusPercent) / 100f);

        for (int i = 0; i < coinDrops; i++)
        {
            Coin.Spawn(FloorPosition + Vector3.up * 0.3f, valueWithBonus, 1.2f);
        }

        // Burst into little cubes of our own color. Bigger enemies (the boss) throw bigger pieces.
        Color pieceColor = originalColors.Length > 0 ? originalColors[0] : bodyColor;
        DeathBurst.Spawn(FloorPosition, pieceColor, deathPieces, 0.25f * transform.localScale.x);
        CameraFollow.Shake(deathShake, 0.2f);

        Destroy(gameObject);
    }

    // null = back to the original colors
    void SetColor(Color? color)
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null) renderers[i].material.color = color ?? originalColors[i];
        }
    }
}
