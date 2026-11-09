using UnityEngine;
using UnityEngine.UI;

// A number that pops up where something got hurt, floats upwards and fades away.
// You never place one by hand: call DamageNumber.Spawn(...) (Enemy and PlayerHealth do).
// It is a tiny world-space canvas, built the same way as the player's HP bar.
public class DamageNumber : MonoBehaviour
{
    public float lifetime = 0.7f;
    public float riseSpeed = 1.5f;  // meters per second upwards
    public float popTime = 0.15f;   // starts big and shrinks to normal size in this time
    public float popScale = 1.6f;

    private Text text;
    private Outline outline;
    private Color startColor;
    private float timer;

    // Creates one floating number at 'position' (a point in the world, e.g. above an enemy's head)
    public static void Spawn(Vector3 position, int amount, Color color)
    {
        Spawn(position, amount.ToString(), color);
    }

    // The same with any text instead of a number, e.g. "DODGE" (two methods may share a name
    // when their inputs are different: C# picks the one that fits)
    public static void Spawn(Vector3 position, string label, Color color)
    {
        // World-space canvas: 100 UI pixels = 1 meter
        const float pixelsPerMeter = 100f;

        GameObject canvasObject = new GameObject("DamageNumber", typeof(Canvas));
        // A little random sideways shift, so numbers from fast hits don't sit exactly on top of each other
        canvasObject.transform.position = position + new Vector3(Random.Range(-0.3f, 0.3f), 0f, 0f);
        canvasObject.transform.localScale = Vector3.one / pixelsPerMeter;

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 10; // in front of the HP bars

        GameObject textObject = new GameObject("Text", typeof(Text));
        textObject.transform.SetParent(canvasObject.transform, false);

        Text text = textObject.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 44;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.color = color;
        text.text = label;
        text.rectTransform.sizeDelta = new Vector2(200f, 60f);

        Outline outline = textObject.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(2f, -2f);

        DamageNumber number = canvasObject.AddComponent<DamageNumber>();
        number.text = text;
        number.outline = outline;
        number.startColor = color;
    }

    void Update()
    {
        timer += Time.deltaTime;

        transform.position += Vector3.up * riseSpeed * Time.deltaTime;

        // Always face the camera, like the HP bar
        if (Camera.main != null) transform.rotation = Camera.main.transform.rotation;

        // Pop: big at the start, then normal size (Lerp stops at 1 by itself once timer > popTime)
        float scale = Mathf.Lerp(popScale, 1f, timer / popTime);
        text.transform.localScale = Vector3.one * scale;

        // Fade out during the second half of its life: 1 = fully visible, 0 = gone
        float alpha = Mathf.InverseLerp(lifetime, lifetime * 0.5f, timer);
        text.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
        outline.effectColor = new Color(0f, 0f, 0f, alpha);

        if (timer >= lifetime) Destroy(gameObject);
    }
}
