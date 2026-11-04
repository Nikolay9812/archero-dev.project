using UnityEngine;
using UnityEngine.AI;

// Charging enemy: walks closer, stops and aims (a red warning line shows where it will go),
// then dashes in a straight line at high speed. Touching the player during the dash hurts.
// After the dash it rests for a moment - the best time to shoot it!
[RequireComponent(typeof(Enemy))]
public class EnemyDasher : MonoBehaviour
{
    public float speed = 2.5f;         // normal walking speed
    public float turnSpeed = 720f;
    public float detectRange = 20f;
    public float dashRange = 7f;       // starts aiming when the player is this close
    public float aimTime = 0.8f;       // how long the warning line shows before the dash
    public float lockTime = 0.25f;     // the last part of the aim stops following the player, so she can dodge
    public float dashDistance = 9f;
    public float dashSpeed = 20f;
    public float restTime = 1.2f;      // stands still after a dash
    public float recoverTime = 0.5f;
    public int damage = 50;
    public float hitRadius = 1f;       // how close the player must be to get hit during the dash
    public Color warningColor = new Color(1f, 0.2f, 0.2f);


    // An enum is a list of named options. The dasher is always in exactly one of these states,
    // and Update does something different for each one. This is called a "state machine".
    private enum State { Walk, Aim, Dash, Rest, Recover }
    private State state = State.Walk;
    private float stateTimer;          // counts down in Aim and Rest

    private Enemy enemy;
    private Vector3 normalScale;
    private Transform warningLine;

    private Vector3 dashDirection;
    private float dashLength;          // how far this dash goes (shorter if a wall is in the way)
    private float dashedSoFar;
    private bool hitThisDash;          // only one hit per dash

    void Awake()
    {
        enemy = GetComponent<Enemy>();
        normalScale = transform.localScale;
        warningLine = CreateWarningLine();
    }

    // Runs once when the component is added in the editor: give dashers an orange tint
    void Reset()
    {
        GetComponent<Enemy>().bodyColor = new Color(1f, 0.65f, 0.2f);
    }

    void OnDestroy()
    {
        // The line is not our child, so it must be cleaned up by hand
        if (warningLine != null) Destroy(warningLine.gameObject);
    }

    void Update()
    {
        PlayerHealth player = PlayerHealth.Instance;
        if (player == null || player.IsDead)
        {
            enemy.Stop();
            ChangeState(State.Walk, 0f);
            return;
        }

        switch (state)
        {
            case State.Walk: Walk(player); break;
            case State.Aim:  Aim(player);  break;
            case State.Dash: Dash(player); break;
            case State.Recover: Recover(); break;
            case State.Rest: Rest();       break;
        }
    }

    void Walk(PlayerHealth player)
    {
        float distance = FlatDistance(player.transform.position);
        if (distance > detectRange)
        {
            enemy.Stop();
            return;
        }

        enemy.TurnTowards(player.transform.position, turnSpeed);

        // Keep walking until the player is close AND nothing blocks the straight line to her
        if (distance > dashRange || IsBlocked(player.transform.position))
        {
            enemy.MoveTo(player.transform.position, speed);
            return;
        }

        enemy.Stop();
        ChangeState(State.Aim, aimTime);
    }

    void Aim(PlayerHealth player)
    {
        stateTimer -= Time.deltaTime;

        // Follow the player at first, then lock the direction for the last moment
        if (stateTimer > lockTime) enemy.TurnTowards(player.transform.position, turnSpeed);

        dashDirection = transform.forward;
        dashLength = DashLengthBeforeWall(dashDirection);
        PlaceWarningLine();

        // Swell up a little, like the other enemies before they attack
        float progress = 1f - stateTimer / aimTime;
        transform.localScale = normalScale * (1f + 0.2f * progress);

        if (stateTimer > 0f) return;

        dashedSoFar = 0f;
        hitThisDash = false;
        ChangeState(State.Dash, 0f);
    }

    void Dash(PlayerHealth player)
    {
        // Move this frame's piece of the dash, but never further than the planned length
        float step = Mathf.Min(dashSpeed * Time.deltaTime, dashLength - dashedSoFar);
        enemy.Push(dashDirection * step);
        dashedSoFar += step;

        if (!hitThisDash && FlatDistance(player.transform.position) <= hitRadius)
        {
            player.TakeDamage(damage);
            hitThisDash = true;
        }

        if (dashedSoFar >= dashLength - 0.01f) ChangeState(State.Rest, restTime);
    }

    void Rest()
    {
        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0f) ChangeState(State.Recover, recoverTime);
    }

    // Spins around once after resting, then goes back to chasing
    void Recover()
    {
        stateTimer -= Time.deltaTime;

        float degreesPerSecond = 360f / recoverTime;           // one full turn in recoverTime seconds
        transform.Rotate(0f, degreesPerSecond * Time.deltaTime, 0f); // this frame's piece of the turn

        if (stateTimer <= 0f) ChangeState(State.Walk, 0f);
    }

    // One place that switches states, so the warning line and the size are always reset correctly
    void ChangeState(State newState, float time)
    {
        state = newState;
        stateTimer = time;
        warningLine.gameObject.SetActive(newState == State.Aim);
        if (newState != State.Aim) transform.localScale = normalScale;
    }

    // Distance on the ground only (ignores height)
    float FlatDistance(Vector3 point)
    {
        Vector3 offset = point - transform.position;
        offset.y = 0f;
        return offset.magnitude;
    }

    // NavMesh.Raycast walks along the floor and returns true when it hits an edge (a wall, a rock...)
    bool IsBlocked(Vector3 target)
    {
        Vector3 start = enemy.FloorPosition;
        target.y = start.y;
        return NavMesh.Raycast(start, target, out NavMeshHit hit, NavMesh.AllAreas);
    }

    float DashLengthBeforeWall(Vector3 direction)
    {
        Vector3 start = enemy.FloorPosition;
        Vector3 end = start + direction * dashDistance;
        if (NavMesh.Raycast(start, end, out NavMeshHit hit, NavMesh.AllAreas)) return hit.distance;
        return dashDistance;
    }

    // Stretch the thin red bar from our feet to where the dash will end
    void PlaceWarningLine()
    {
        Vector3 start = enemy.FloorPosition + Vector3.up * 0.05f;
        warningLine.position = start + dashDirection * (dashLength / 2f);
        warningLine.rotation = Quaternion.LookRotation(dashDirection);
        warningLine.localScale = new Vector3(0.2f, 0.02f, dashLength);
    }

    // Placeholder effect: a very flat cube lying on the floor
    Transform CreateWarningLine()
    {
        GameObject line = GameObject.CreatePrimitive(PrimitiveType.Cube);
        line.name = "DashWarningLine";
        Destroy(line.GetComponent<Collider>()); // just a picture, arrows should fly over it
        line.GetComponent<Renderer>().material.color = warningColor;
        line.SetActive(false);
        return line.transform;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.65f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, dashRange);
    }
}
