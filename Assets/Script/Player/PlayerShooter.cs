using UnityEngine;

// Archero auto-shoot: while the player stands still, turn to the nearest enemy and fire arrows.
// As soon as the player moves again, shooting stops.
[RequireComponent(typeof(PlayerMovement))]
public class PlayerShooter : MonoBehaviour
{
    // Optional: drag an arrow prefab here (it needs the Arrow script).
    // If left empty, a simple stick-shaped arrow is created by code.
    public Arrow arrowPrefab;

    public float fireRate = 2f;        // arrows per second
    public float range = 15f;          // enemies further away are ignored
    public float turnSpeed = 720f;     // degrees per second when turning to aim
    public float aimTolerance = 10f;   // only shoot when facing the enemy within this angle
    public Vector3 shootOffset = new Vector3(0f, 1f, 0.6f); // where arrows spawn (up, forward from the player)

    public Enemy CurrentTarget { get; private set; }

    private PlayerMovement movement;
    private float cooldown;

    void Awake()
    {
        movement = GetComponent<PlayerMovement>();
    }

    void Update()
    {
        cooldown -= Time.deltaTime;

        // Moving = no shooting. This is the core rule of the game.
        if (movement.IsMoving)
        {
            CurrentTarget = null;
            return;
        }

        CurrentTarget = FindNearestEnemy();
        if (CurrentTarget == null) return;

        // Turn on the ground plane only, so she doesn't tilt up or down
        Vector3 toEnemy = CurrentTarget.transform.position - transform.position;
        toEnemy.y = 0f;
        if (toEnemy.sqrMagnitude < 0.001f) return;

        Quaternion targetRotation = Quaternion.LookRotation(toEnemy);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);

        bool isAimed = Quaternion.Angle(transform.rotation, targetRotation) <= aimTolerance;
        if (isAimed && cooldown <= 0f)
        {
            Shoot(toEnemy.normalized);
            cooldown = 1f / fireRate;
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
        Vector3 spawnPosition = transform.position + Vector3.up * shootOffset.y + direction * shootOffset.z;
        Quaternion spawnRotation = Quaternion.LookRotation(direction);

        if (arrowPrefab != null) Instantiate(arrowPrefab, spawnPosition, spawnRotation);
        else CreateSimpleArrow(spawnPosition, spawnRotation);
    }

    // Placeholder arrow until we have a real model: a thin stretched cube
    void CreateSimpleArrow(Vector3 position, Quaternion rotation)
    {
        GameObject arrow = GameObject.CreatePrimitive(PrimitiveType.Cube);
        arrow.name = "Arrow";
        arrow.transform.SetPositionAndRotation(position, rotation);
        arrow.transform.localScale = new Vector3(0.08f, 0.08f, 0.7f);
        arrow.GetComponent<Renderer>().material.color = new Color(1f, 0.85f, 0.3f);

        arrow.AddComponent<Rigidbody>();
        arrow.AddComponent<Arrow>();
    }

    // Shows the shooting range in the Scene view when the player is selected
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, range);
    }
}
