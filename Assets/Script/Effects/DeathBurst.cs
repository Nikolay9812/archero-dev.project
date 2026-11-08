using UnityEngine;

// One little cube that flies out of a dying enemy, bounces on the floor and shrinks away.
// Call DeathBurst.Spawn(...) to throw a whole handful of them at once (Enemy does it in Die).
// There is no physics here: we move the cube ourselves with a simple "gravity" number.
public class DeathBurst : MonoBehaviour
{
    const float gravity = 15f;   // how fast pieces fall (meters per second, every second)
    const float bounce = 0.4f;   // keeps 40% of the speed when hitting the floor

    private Vector3 velocity;
    private Vector3 spin;        // degrees per second around each axis
    private Vector3 startScale;
    private float floorY;
    private float lifetime;
    private float timer;

    // Throws 'pieces' cubes of 'color' out from 'floorPosition' (the point on the floor under the enemy).
    // 'size' is the size of one cube in meters.
    public static void Spawn(Vector3 floorPosition, Color color, int pieces, float size)
    {
        for (int i = 0; i < pieces; i++)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "DeathPiece";
            Destroy(cube.GetComponent<Collider>()); // only for looks: arrows and the player go through
            cube.GetComponent<Renderer>().material.color = color;

            float pieceSize = size * Random.Range(0.6f, 1.2f);
            cube.transform.localScale = Vector3.one * pieceSize;
            cube.transform.position = floorPosition + Vector3.up * 0.8f;
            cube.transform.rotation = Random.rotation;

            DeathBurst piece = cube.AddComponent<DeathBurst>();

            // A random direction on the ground, plus a push upwards
            Vector2 side = Random.insideUnitCircle.normalized * Random.Range(2f, 5f);
            piece.velocity = new Vector3(side.x, Random.Range(3f, 6f), side.y);
            piece.spin = Random.insideUnitSphere * 720f;
            piece.startScale = cube.transform.localScale;
            piece.floorY = floorPosition.y + pieceSize / 2f; // so the cube rests ON the floor, not half in it
            piece.lifetime = Random.Range(0.6f, 0.9f);
        }
    }

    void Update()
    {
        timer += Time.deltaTime;

        // Fall a little faster every frame, then move
        velocity.y -= gravity * Time.deltaTime;
        transform.position += velocity * Time.deltaTime;
        transform.Rotate(spin * Time.deltaTime);

        // Hit the floor: bounce up a little and slow down
        if (transform.position.y < floorY)
        {
            Vector3 position = transform.position;
            position.y = floorY;
            transform.position = position;

            velocity.y = -velocity.y * bounce;
            velocity.x *= 0.6f;
            velocity.z *= 0.6f;
            spin *= 0.5f;
        }

        // Shrink to nothing during the last 40% of its life
        float shrink = Mathf.InverseLerp(lifetime, lifetime * 0.6f, timer);
        transform.localScale = startScale * shrink;

        if (timer >= lifetime) Destroy(gameObject);
    }
}
