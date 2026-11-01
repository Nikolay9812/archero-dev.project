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

    // Every living enemy in the scene. The player's auto-shoot looks here for the nearest target,
    // so we don't need tags or FindObjectsByType every frame.
    public static readonly List<Enemy> All = new List<Enemy>();

    public int CurrentHealth { get; private set; }
    public bool IsDead => CurrentHealth <= 0;

    private NavMeshAgent agent;
    private Renderer[] renderers;
    private Color[] originalColors;
    private float flashTimer;

    void Awake()
    {
        CurrentHealth = maxHealth;

        // The agent finds the path and moves us; turning is done by our own TurnTowards
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false;
        agent.acceleration = 40f; // start and stop quickly instead of sliding

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

        if (IsDead) Die();
    }

    void Die()
    {
        // Later: death animation, coins, XP...
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
